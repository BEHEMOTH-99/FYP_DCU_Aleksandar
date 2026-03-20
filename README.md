# Board-Mount Sensor-Based Exoskeleton: Final Year Project

This repository contains the codebase and technical documentation for a wireless, 3-finger (9-sensor) hand tracking and calibration system developed for a DCU Final Year Project (FYP).

## System Overview

The system captures hand movement using board-mounted potentiometer sensors and transmits high-precision degree data wirelessly to a Unity-based hand model.

### Key Features
- **On-Board Calibration:** 2-point linear spline calibration (0° and 90°) implemented directly on the ESP32 transmitter.
- **ESP-NOW Communication:** Ultra-low latency wireless protocol between the Glove (Sender) and the USB Dongle (Receiver).
- **Unity Real-time Visualization:** Smooth, anatomical bone mapping for the Middle, Index, and Thumb fingers.
- **Data Export:** CSV-compatible output for sensor response analysis and thesis visualization.

## Hardware Components
- **Microcontrollers:** ESP32 Feather (Sender) and ESP32 S3 (Receiver).
- **Sensors:** 9x Rotary Potentiometers (3 per finger: Horizontal MCP, Vertical MCP, Vertical PIP).
- **Communication:** ~50 Hz data throughput via ESP-NOW.

## Project Structure
- `Arduino_Code/`: 
  - `Main_Calibration/`: The core transmitter firmware.
  - `Receiver_9_Sensors/`: The serial dongle firmware for Unity.
  - `Angle_Test_9_Sensors/` & `Angle_Calibration_9_Sensors/`: Diagnostic and standalone tools.
- `Unity Project/`: The complete hand visualization project.
- `Python_Scripts/`: Optional data plotting and analysis tools.

## Calibration Guide
To calibrate the system, connect the Glove via USB and use the following Serial Monitor commands:
- **Index:** `I0` (Straight), `I90` (Bent)
- **Middle:** `M0` (Straight), `M90` (Bent)
- **Thumb:** `T0` (Straight), `T90` (Bent) - *Note: Thumb MCP is software-mapped to a realistic 60° limit.*
- **Horizontal:** `Ih0`, `Ih30` etc.
- **Finalize:** Type `save` to commit to ESP32 Flash memory.

---
**Developer:** Aleksandar (BEHEMOTH-99)  
**Institution:** Dublin City University (DCU)
