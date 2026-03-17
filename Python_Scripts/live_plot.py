import serial
import matplotlib.pyplot as plt
import matplotlib.animation as animation
from collections import deque
import sys

# =======================================================
# CONFIGURATION
# Set this to the COM port your Receiver ESP32 is using 
# (e.g., 'COM3', 'COM4', etc. Check Arduino IDE)
# =======================================================
COM_PORT = 'COM3' 
BAUD_RATE = 115200

# Number of points to display simultaneously
MAX_POINTS = 100

# Queues to hold the rolling data
data_pot1 = deque(maxlen=MAX_POINTS)
data_pot2 = deque(maxlen=MAX_POINTS)
data_pot3 = deque(maxlen=MAX_POINTS)

try:
    # Open the serial port
    ser = serial.Serial(COM_PORT, BAUD_RATE, timeout=0.1)
    print(f"Successfully connected to {COM_PORT}.")
    print("Waiting for data... Close the plot window to exit.")
except Exception as e:
    print(f"Error opening serial port {COM_PORT}: {e}")
    print("Ensure the port is correct and NOT open in Arduino IDE Serial Monitor/Plotter or Unity!")
    sys.exit(1)

# Initialize the Matplotlib plot
fig, ax = plt.subplots(figsize=(10, 5))
line1, = ax.plot([], [], label='Index (Pot 1)', color='#1f77b4', linewidth=2)
line2, = ax.plot([], [], label='Middle (Pot 2)', color='#ff7f0e', linewidth=2)
line3, = ax.plot([], [], label='Ring (Pot 3)', color='#2ca02c', linewidth=2)

ax.set_xlim(0, MAX_POINTS)
ax.set_ylim(0, 4095) # ESP32 ADC is 12-bit (0 to 4095)
ax.set_title("Live ESP32 Hand Controller Data")
ax.set_xlabel("Time (Samples)")
ax.set_ylabel("ADC Value (0-4095)")
ax.legend(loc='upper right')
ax.grid(True, linestyle='--', alpha=0.7)

def update(frame):
    """ Animation function called periodically by Matplotlib """
    try:
        while ser.in_waiting: # Read all available lines in buffer
            # Read a line from serial, decode to string, and strip whitespace/newlines
            line = ser.readline().decode('utf-8', errors='ignore').strip()
            if line:
                # The Receiver.ino outputs csv data like: "val1,val2,val3"
                values = line.split(',')
                if len(values) == 3:
                    # Append new data to our rolling lists
                    data_pot1.append(int(values[0]))
                    data_pot2.append(int(values[1]))
                    data_pot3.append(int(values[2]))
                    
        # Update the line data
        line1.set_data(range(len(data_pot1)), data_pot1)
        line2.set_data(range(len(data_pot2)), data_pot2)
        line3.set_data(range(len(data_pot3)), data_pot3)
        
    except ValueError:
        pass # Ignore incomplete/corrupted serial lines
    except Exception as e:
        print(f"Error reading data: {e}")
        
    return line1, line2, line3

# Run the live animation at ~50 fps (20ms interval)
ani = animation.FuncAnimation(fig, update, interval=20, blit=False, cache_frame_data=False)

plt.tight_layout()
plt.show()

# Safely close the serial port when the window is closed
ser.close()
print("Serial port closed.")
