using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace ML_mnist
{
    public partial class Form1 : Form
    {
        private bool isMouseDown = false;
        private Point lastPoint;
        private Pen pen = new Pen(Color.White, 5);

        private Bitmap canvasBitmap;
        private Bitmap overlayBitmap;

        private class DigitResult
        {
            public int x { get; set; }
            public int y { get; set; }
            public int w { get; set; }
            public int h { get; set; }
            public float[] pred { get; set; }
            public string image { get; set; }
        }

        private Panel ClonePanel(Panel originalPanel)
        {
            Panel clonedPanel = new Panel()
            {
                Size = originalPanel.Size,
                BackColor = originalPanel.BackColor,
                Margin = originalPanel.Margin,
                Name = originalPanel.Name
            };

            foreach (Control ctrl in originalPanel.Controls)
            {
                Control newCtrl = CloneControl(ctrl);
                clonedPanel.Controls.Add(newCtrl);
            }

            return clonedPanel;
        }
        private Control CloneControl(Control original)
        {
            Control clone = (Control)Activator.CreateInstance(original.GetType());

            // Common prop
            clone.Size = original.Size;
            clone.Location = original.Location;
            clone.BackColor = original.BackColor;
            clone.ForeColor = original.ForeColor;
            clone.Font = original.Font;
            clone.Text = original.Text;
            clone.Enabled = original.Enabled;
            clone.Dock = original.Dock;
            clone.Margin = original.Margin;
            clone.Padding = original.Padding;
            clone.Name = original.Name;

            if (clone.GetType() == typeof(PictureBox))
            {
                PictureBox pictureBox = clone as PictureBox;
                pictureBox.Image = (original as PictureBox).Image;
                pictureBox.SizeMode = (original as PictureBox).SizeMode;
            }

            if (clone.GetType() == typeof(Label))
            {
                Label label = clone as Label;
                label.TextAlign = (original as Label).TextAlign;
            }

            // Recursive
            foreach (Control child in original.Controls)
            {
                clone.Controls.Add(CloneControl(child));
            }

            return clone;
        }

        private class ProgressInfo
        {
            public Panel Panel { get; set; }
            public float Probability { get; set; }
        }

        int currentTick = 0;
        int targetTick = 60;
        List<ProgressInfo> probBarList = new List<ProgressInfo>();

        private float getProgress(float x)
        {
            return x * x * (3f - 2f * x);
        }

        private void timerTick(object sender, EventArgs e)
        {
            if (currentTick < targetTick)
            {
                currentTick += 1;
                float mult = getProgress((float)currentTick / targetTick);

                foreach (var anim in probBarList)
                {
                    anim.Panel.Width = (int)(mult * anim.Probability * predBarWidth);
                }
            }
            else
            {
                currentTick = 0;
                timer.Stop();
            }
        }
        private Bitmap MergeBitmaps(Bitmap bottom, Bitmap top)
        {
            Bitmap result = new Bitmap(bottom.Width, bottom.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bottom, Point.Empty);
                g.DrawImage(top, Point.Empty);
            }
            return result;
        }

        int baseSizeX;
        int infoSizeX;
        int infoMargin;
        int predBarWidth;

        public Form1()
        {
            InitializeComponent();

            canvasBitmap = new Bitmap(canvasBox.Width, canvasBox.Height);
            overlayBitmap = new Bitmap(canvasBox.Width, canvasBox.Height);

            canvasBox.Image = MergeBitmaps(canvasBitmap, overlayBitmap);

            baseSizeX = this.Width;
            infoSizeX = digitSample.Width;
            infoMargin = digitSample.Margin.Right;
            predBarWidth = pred_1.Width;
        }

        private void canvasBox_MouseDown(object sender, MouseEventArgs e)
        {
            lastPoint = e.Location;
            isMouseDown = true;
        }

        private void canvasBox_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
        }

        bool hasCleared = false;

        private void canvasBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                using (Graphics g = Graphics.FromImage(canvasBitmap))
                {
                    g.DrawLine(pen, lastPoint, e.Location);
                }
                lastPoint = e.Location;
                if (!hasCleared)
                {
                    overlayBitmap = new Bitmap(canvasBox.Width, canvasBox.Height);
                    hasCleared = true;
                }
                canvasBox.Image = MergeBitmaps(canvasBitmap, overlayBitmap);
            }
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            canvasBitmap = new Bitmap(canvasBox.Width, canvasBox.Height);
            overlayBitmap = new Bitmap(canvasBox.Width, canvasBox.Height);
            canvasBox.Image = MergeBitmaps(canvasBitmap, overlayBitmap);
            // canvasBox.Invalidate();
        }

        private async void predictButton_Click(object sender, EventArgs e)
        {
            string savePath = "input.png";
            canvasBitmap.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            using (Graphics g = Graphics.FromImage(overlayBitmap))
            {
                var imgBytes = File.ReadAllBytes(savePath);
                var fileContent = new ByteArrayContent(imgBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                content.Add(fileContent, "image", "input.png");

                var response = await client.PostAsync("http://127.0.0.1:5000/predict", content);
                var resultJson = await response.Content.ReadAsStringAsync();
                var resultList = JsonConvert.DeserializeObject<List<DigitResult>>(resultJson);

                digitHolder.Controls.Clear();

                int count = resultList.Count;
                int mainWidth = baseSizeX + (count - 1) * (infoSizeX + infoMargin);
                if (mainWidth < baseSizeX)
                {
                    mainWidth = baseSizeX;
                }
                this.Width = mainWidth;
                digitHolder.Width = count * (infoSizeX + infoMargin);

                string predicted = "";
                probBarList = new List<ProgressInfo>();

                foreach (var digit in resultList)
                {
                    Panel panel = ClonePanel(digitSample);
                    digitHolder.Controls.Add(panel);

                    byte[] decodedImage = Convert.FromBase64String(digit.image);
                    var pictureBox = panel.Controls
                        .OfType<PictureBox>()
                        .FirstOrDefault();
                    using (var ms = new MemoryStream(decodedImage))
                    {
                        pictureBox.Image = Image.FromStream(ms);
                    }

                    var topPreds = digit.pred
                        .Select((value, index) => new { Index = index, Value = value })
                        .OrderByDescending(p => p.Value)
                        .ToList();
                    var probListPanel = panel.Controls.OfType<Panel>().FirstOrDefault();

                    for (int i = 1; i <= 4; i++)
                    {
                        var predInfo = topPreds[i - 1];
                        var predPanel = probListPanel.Controls
                            .OfType<Panel>()
                            .FirstOrDefault(p => p.Name == $"pred_{i}");

                        var digitLabel = predPanel.Controls
                            .OfType<Label>()
                            .FirstOrDefault(l => l.Name == $"pred_{i}_digit");

                        var probLabel = predPanel.Controls
                            .OfType<Label>()
                            .FirstOrDefault(l => l.Name == $"pred_{i}_prob");

                        digitLabel.Text = predInfo.Index.ToString();
                        probLabel.Text = (predInfo.Value * 100).ToString("0.00") + "%";

                        var probBar = predPanel.Controls
                            .OfType<Panel>()
                            .FirstOrDefault();

                        var probBarDigit = probBar.Controls
                            .OfType<Label>()
                            .FirstOrDefault(l => l.Name == $"pred_{i}_probBar_digit");

                        var probBarProb = probBar.Controls
                            .OfType<Label>()
                            .FirstOrDefault(l => l.Name == $"pred_{i}_probBar_prob");

                        probBarDigit.Text = predInfo.Index.ToString();
                        probBarProb.Text = (predInfo.Value * 100).ToString("0.00") + "%";

                        probBarList.Add(new ProgressInfo {
                            Panel = probBar,
                            Probability = predInfo.Value
                        });
                    }

                    Color boxColor = topPreds[0].Value > 0.7 ? Color.Lime : Color.Red;
                    using (Pen boxPen = new Pen(boxColor, 1))
                    {
                        g.DrawRectangle(boxPen, digit.x, digit.y, digit.w, digit.h);
                    }

                    using (Font font = new Font("Arial", 14, FontStyle.Regular))
                    using (Brush brush = new SolidBrush(boxColor))
                    {
                        g.DrawString(topPreds[0].Index.ToString(), font, brush, digit.x, digit.y - 27);
                    }

                    predicted = predicted + topPreds[0].Index.ToString();
                    timer.Start();
                }

                predictButton.Text = "Predicted number: " + predicted;
                canvasBox.Image = MergeBitmaps(canvasBitmap, overlayBitmap);
                hasCleared = false;
            }
        }
    }
}