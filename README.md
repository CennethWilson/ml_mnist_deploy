# Multi-Digit Reader

Multi-Digit Reader is a data science-focused passion project designed to read and predict multiple user-inputted, hand-drawn digits using a neural network model trained on the MNIST dataset.

---

## ℹ️ About the Project

The **Multi-Digit Reader** is an software that features:

- Recognizes multiple digits using openCV
- Predicts digits using a trained MNIST model with probability breakdown
- GUI built with C# (Winforms)
- Local based backend with flask API

---

## 🛠️ Built With

- [C#](https://learn.microsoft.com/en-us/dotnet/csharp/) — programming language used for gui
- [WinForms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/) — UI development
- [Microsoft Visual Studio](https://visualstudio.microsoft.com/) — gui development environment
- [Python](https://www.python.org/) — programming language used for model training and backend (Flask)
- [PyCharm](https://www.jetbrains.com/pycharm/) — model training and backend development environment
- [Keras](https://keras.io/) — CNN model training and mnist dataset
- [OpenCV](https://opencv.org/) — Digit recognition
- [Flask](https://flask.palletsprojects.com/en/stable/) — Web server for backend
- [PyInstaller](https://pyinstaller.org/en/stable/) — backend packaging to .exe

---

### Installation & Setup

1. **Clone the repository:**

   ```bash
   git clone https://github.com/CennethWilson/ml_mnist.git
   cd ml-mnist

2. **Create the backend exe file**

   - Open `backend/backend.py`
   
   - Type in terminal:
   ```bash
   pyinstaller --onefile --noconsole backend.py
   ```
   
   - Move the resulting exe file to backend folder

3. **Run the application**

   `Run start_app.bat`

---

## 📊 Results
https://github.com/user-attachments/assets/0fcb4b0b-7c85-4218-95bd-de79a3cba710


## 📃 License

This project is licensed under the MIT License. See the `LICENSE.txt` file for more information.
