import matplotlib.pyplot as plt
import numpy as np
import sys

try:
    # Generate mock timeline
    t = np.linspace(0, 10, 300)
    
    # Generate 3 potentiometer signals with some noise, scaled 0-4095 (12-bit ADC)
    # Adding slight phase shifts to differentiate them
    p1 = 2048 + 1800 * np.sin(t) + np.random.normal(0, 50, len(t))
    p2 = 2048 + 1800 * np.sin(t - 1) + np.random.normal(0, 50, len(t))
    p3 = 2048 + 1800 * np.sin(t - 2) + np.random.normal(0, 50, len(t))
    
    # Clip to valid 12-bit ADC range
    p1 = np.clip(p1, 0, 4095)
    p2 = np.clip(p2, 0, 4095)
    p3 = np.clip(p3, 0, 4095)

    plt.figure(figsize=(10, 5))
    plt.plot(t, p1, label='A0 (Sensor 1 / Index)', color='#1f77b4', linewidth=2)
    plt.plot(t, p2, label='A1 (Sensor 2 / Middle)', color='#ff7f0e', linewidth=2)
    plt.plot(t, p3, label='A2 (Sensor 3 / Ring)', color='#2ca02c', linewidth=2)
    
    plt.title('Simulated ESP32 Potentiometer ADC Readings (12-bit)', fontsize=14, pad=10)
    plt.xlabel('Time (s)', fontsize=12)
    plt.ylabel('ADC Value (0-4095)', fontsize=12)
    plt.legend(loc='upper right')
    plt.grid(True, linestyle='--', alpha=0.7)
    
    plt.tight_layout()
    plt.savefig(r'C:\Users\aleks\.gemini\antigravity\brain\14f81a21-ecd4-4892-bb9f-366022505866\pot_graph.png', dpi=150)
    print("Graph generated successfully.")
except Exception as e:
    print(f"Failed to generate graph: {e}")
    sys.exit(1)
