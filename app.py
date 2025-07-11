from flask import Flask, jsonify, request
import numpy as np
from keras import models
from PIL import Image
import io
import cv2
import base64

model = models.load_model("mnist_model.keras")
app = Flask(__name__)

def clean_image(digit_img):
    kernel = np.ones((2, 2), np.uint8)
    thick = cv2.dilate(digit_img, kernel, iterations=3)

    h, w = thick.shape
    scale = 20.0 / max(h, w)
    resized = cv2.resize(thick, (int(w * scale), int(h * scale)), interpolation=cv2.INTER_AREA)

    new_img = np.zeros((28, 28), dtype=np.uint8)
    x_offset = (28 - resized.shape[1]) // 2
    y_offset = (28 - resized.shape[0]) // 2
    new_img[y_offset:y_offset + resized.shape[0], x_offset:x_offset + resized.shape[1]] = resized

    return new_img

def get_digits(image_bytes):
    img_pil = Image.open(io.BytesIO(image_bytes)).convert('L')
    img_array = np.array(img_pil)

    _, thresh = cv2.threshold(img_array, 10, 255, cv2.THRESH_BINARY)
    contours, _ = cv2.findContours(thresh, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    boxes = []

    for c in contours:
        x, y, w, h = cv2.boundingRect(c)
        raw = thresh[y:y+h, x:x+w]
        clean = clean_image(raw)

        boxes.append((clean, x, y, w, h))
    sorted_boxes = [x for x in sorted(boxes, key = lambda s: s[1])]
    return sorted_boxes

@app.route("/predict", methods=["POST"])
def predict():
    image_bytes = request.files['image'].read()
    digit_images = get_digits(image_bytes)

    result = []

    for info in digit_images:
        digit_img = info[0]
        x, y, w, h = info[1:]

        input_array = digit_img.reshape(1, 28, 28, 1).astype("float32") / 255.0
        pred = model.predict(input_array).tolist()

        pil_img = Image.fromarray(digit_img.astype(np.uint8))
        buf = io.BytesIO()
        pil_img.save(buf, format="PNG")
        img_base64 = base64.b64encode(buf.getvalue()).decode("utf-8")

        result.append({
            "x": x,
            "y": y,
            "w": w,
            "h": h,
            "pred": pred[0],
            "image": img_base64,
        })

    # print(result)
    return jsonify(result)

@app.route("/ping", methods=["GET"])
def ping():
    return "pong", 200

if __name__ == "__main__":
    port = int(os.environ.get("PORT", 8000))
    app.run(host="0.0.0.0", port=port)
