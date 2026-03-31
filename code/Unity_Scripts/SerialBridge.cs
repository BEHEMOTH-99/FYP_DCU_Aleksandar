using UnityEngine;
using UnityEngine.InputSystem;
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

    // Latency tracking
    private bool baseTimeSet = false;
    private uint baseSenderTime = 0;
    private float baseUnityTime = 0f;
    private float latencySum = 0f;
    private int packetCount = 0;

    // Latency Stopwatch (Thread Safe)
    private System.Diagnostics.Stopwatch stopwatch;

    // Thread-safe data passing (Updated for 9-float array)
    private readonly object dataLock = new object();
    private float[] latestAngles = null;
    private bool hasNewData = false;

    void Start()
    {
        // Auto-assign if attached to the same GameObject
        if (handController == null)
        {
            handController = GetComponent<AnatomicalHandController>();
        }

        stopwatch = System.Diagnostics.Stopwatch.StartNew();

        OpenSerialPort();
    }

    void OpenSerialPort()
    {
        try
        {
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

    void Update()
    {
        float[] dataToProcess = null;
        
        lock (dataLock)
        {
            if (hasNewData && latestAngles != null)
            {
                dataToProcess = latestAngles;
                hasNewData = false;
            }
        }

        // We process unity objects (Animation) on the MAIN THREAD safely!
        // Note: GloveCalibration is bypassed as ESP32 sends degrees directly.
        if (dataToProcess != null && handController != null)
        {
            handController.SetTargetInputs(dataToProcess);
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
        string[] values = dataLine.Trim().Split(',');
        
        // Ensure we have at least some sensors and one timestamp
        if (values.Length >= 2)
        {
            int numSensors = values.Length - 1; // Last is always timestamp
            string timestampStr = values[values.Length - 1];
            
            // --- LATENCY MEASUREMENT ---
            if (uint.TryParse(timestampStr, out uint senderTime))
            {
                // Use Stopwatch instead of Time.realtimeSinceStartup to avoid Unity main thread errors
                float unityTime = (float)stopwatch.Elapsed.TotalMilliseconds;
                if (!baseTimeSet)
                {
                    baseSenderTime = senderTime;
                    baseUnityTime = unityTime;
                    baseTimeSet = true;
                    latencySum = 0f;
                    packetCount = 0;
                }
                else
                {
                    float expectedUnityTime = baseUnityTime + (senderTime - baseSenderTime);
                    float latencyDelta = unityTime - expectedUnityTime;
                    
                    latencySum += latencyDelta;
                    packetCount++;
                    if (packetCount >= 50)
                    {
                        Debug.Log($"[Latency Target Delta] +{(latencySum / 50f):F2} ms (Fluctuation). Parsed {numSensors} sensors.");
                        latencySum = 0;
                        packetCount = 0;
                    }
                }
            }

            // --- HAND ANIMATION WITH CALIBRATION ---
            int maxSensorsToRead = Mathf.Min(numSensors, 20); // Cap at 20 based on GloveCalibration struct
            int[] rawSensors = new int[20];
            
            bool validParse = true;
            // --- HAND ANIMATION ---
            int maxSensorsToRead = Mathf.Min(numSensors, 9); 
            float[] tempAngles = new float[9];
            
            bool validParse = true;
            for (int i = 0; i < maxSensorsToRead; i++)
            {
                if (!float.TryParse(values[i], out tempAngles[i]))
                {
                    validParse = false;
                    break;
                }
            }

            if (validParse)
            {
                // Save the data to be processed by the Main Thread in Update()
                lock (dataLock)
                {
                    latestAngles = tempAngles;
                    hasNewData = true;
                }
            }
        }
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
