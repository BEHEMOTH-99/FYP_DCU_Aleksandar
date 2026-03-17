using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class SerialBridge : MonoBehaviour
{
    [Header("Serial Port Settings")]
    [Tooltip("Check your device manager for the correct COM port of the ESP32-S3 Receiver")]
    public string portName = "COM3";
    public int baudRate = 115200;

    [Header("Target Controller")]
    public AnatomicalHandController handController;

    private SerialPort serialPort;
    private Thread serialThread;
    private bool isRunning = false;
    
    // Shared variable for thread-safe transfer
    private float targetGripStrength = 0f;

    void Start()
    {
        // Auto-assign if attached to the same GameObject
        if (handController == null)
        {
            handController = GetComponent<AnatomicalHandController>();
        }

        OpenSerialPort();
    }

    void OpenSerialPort()
    {
        try
        {
            // Note: If 'SerialPort' is not found, ensure your Unity project API Compatibility Level is set to '.NET Framework' (Edit -> Project Settings -> Player -> Other Settings -> Api Compatibility Level).
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 50;
            serialPort.Open();
            
            isRunning = true;
            serialThread = new Thread(ReadSerialData);
            serialThread.Start();
            Debug.Log($"[SerialBridge] Port {portName} opened successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SerialBridge] Error opening serial port: {e.Message}");
        }
    }

    void ReadSerialData()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string dataLine = serialPort.ReadLine();
                ParseData(dataLine);
            }
            catch (System.TimeoutException) { /* Timeout is expected, keeps thread responsive */ }
            catch (System.Exception e) { Debug.LogWarning($"[SerialBridge] Read Error: {e.Message}"); }
        }
    }

    void ParseData(string dataLine)
    {
        // Expected CSV format: "pot1,pot2,pot3" (0-4095)
        string[] values = dataLine.Trim().Split(',');
        if (values.Length == 3)
        {
            if (int.TryParse(values[0], out int pot1) &&
                int.TryParse(values[1], out int pot2) &&
                int.TryParse(values[2], out int pot3))
            {
                // Map from 12-bit ADC (0-4095) to normalized values (0.0 - 1.0)
                if (handController != null)
                {
                    handController.SetTargetInputs(
                        Mathf.Clamp01(pot1 / 4095f), 
                        Mathf.Clamp01(pot2 / 4095f), 
                        Mathf.Clamp01(pot3 / 4095f)
                    );
                }
            }
        }
    }

    void Update()
    {
        // The smoothing has been moved directly into the AnatomicalHandController
        // to handle multiple joints cleanly.
    }

    void OnDestroy()
    {
        isRunning = false;
        
        if (serialThread != null && serialThread.IsAlive)
        {
            serialThread.Join(500);
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}
