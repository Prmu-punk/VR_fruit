# Fruit Ninja VR Simulator

Unity VR-style Fruit Ninja project for the VR course final assignment.

## Run in Unity

- Recommended Unity version: `2021.3.16f1c1`.
- Open this folder as a Unity project.
- Open `Assets/Scenes/FruitNinja.unity`.
- Press Play.
- Without webcam tracking, the right XR saber follows the mouse. Hold left mouse button to slice.

## Webcam XR Tracking

The game listens for webcam tracking data on UDP port `7777`.

Run the tracking service from the companion simulator project:

```powershell
cd "path\to\VR_simulator\tools\tracking_service"
python webcam_tracking_service.py
```

Wait until the camera window shows `Calibrated: yes`, then return to Unity Play mode.

When calibrated, the left and right webcam-tracked hands drive the left and right XR controller sabers.

## Notes

- Unity cache folders such as `Library/`, `Logs/`, and `UserSettings/` are intentionally ignored.
- The original Fruit Ninja tutorial project is from `zigurous/unity-fruit-ninja-tutorial`.
