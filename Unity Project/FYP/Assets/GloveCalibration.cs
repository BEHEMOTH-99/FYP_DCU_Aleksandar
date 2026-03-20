using System.Collections;
using System.IO;
using UnityEngine;

[System.Serializable]
public class GloveProfile
{
    public int[] minValues = new int[20];
    public int[] maxValues = new int[20];
}

public class GloveCalibration : MonoBehaviour
{
    public static GloveCalibration Instance;

    public enum CalibrationState { Calibrated, CalibratingOpen, CalibratingClosed }
    public CalibrationState currentState = CalibrationState.Calibrated;

    [Header("Profile Data")]
    public GloveProfile profile = new GloveProfile();

    private string SavePath => Path.Combine(Application.persistentDataPath, "glove_profile.json");

    // Sampling state
    private int _sampleCount = 0;
    private long[] _sampleSum = new long[20];
    private const int SAMPLES_NEEDED = 50;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadProfile();
    }

    void Update()
    {
        // Keyboard inputs for the user to trigger calibration
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCalibration(CalibrationState.CalibratingOpen);
        }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            StartCalibration(CalibrationState.CalibratingClosed);
        }
    }

    void StartCalibration(CalibrationState newState)
    {
        currentState = newState;
        _sampleCount = 0;
        for (int i = 0; i < 20; i++) _sampleSum[i] = 0;
        Debug.Log($"[Calibration] Started: {newState}");
    }

    // Called by SerialBridge with the latest raw ADC values
    public float[] ProcessSensors(int[] rawValues)
    {
        float[] normalized = new float[20];

        // If we are currently calibrating, accumulate the samples
        if (currentState == CalibrationState.CalibratingOpen || currentState == CalibrationState.CalibratingClosed)
        {
            for (int i = 0; i < 20 && i < rawValues.Length; i++)
            {
                _sampleSum[i] += rawValues[i];
            }
            _sampleCount++;

            if (_sampleCount >= SAMPLES_NEEDED)
            {
                CompleteCalibrationStep();
            }
        }

        // Always return normalized values based on current profile
        for (int i = 0; i < 20 && i < rawValues.Length; i++)
        {
            int min = profile.minValues[i];
            int max = profile.maxValues[i];

            // Prevent division by zero if uncalibrated
            if (max - min == 0)
            {
                normalized[i] = 0f;
                continue;
            }

            // Normal InverseLerp
            float percent = (float)(rawValues[i] - min) / (max - min);
            normalized[i] = Mathf.Clamp01(percent);
        }

        return normalized;
    }

    void CompleteCalibrationStep()
    {
        for (int i = 0; i < 20; i++)
        {
            int average = (int)(_sampleSum[i] / SAMPLES_NEEDED);

            if (currentState == CalibrationState.CalibratingOpen)
            {
                profile.minValues[i] = average;
            }
            else if (currentState == CalibrationState.CalibratingClosed)
            {
                profile.maxValues[i] = average;
            }
        }

        Debug.Log($"[Calibration] Finished {currentState}. Array 0 is now: min={profile.minValues[0]} max={profile.maxValues[0]}");

        currentState = CalibrationState.Calibrated;
        SaveProfile();
    }

    void SaveProfile()
    {
        string json = JsonUtility.ToJson(profile, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("[Calibration] Saved to: " + SavePath);
    }

    void LoadProfile()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            JsonUtility.FromJsonOverwrite(json, profile);
            Debug.Log("[Calibration] Loaded existing profile from: " + SavePath);
        }
        else
        {
            Debug.Log("[Calibration] No profile found. Please calibrate by pressing Space (Open) and Enter (Closed).");
        }
    }

    // Quick on-screen UI for prompts without needing a Unity Canvas
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 20;
        style.alignment = TextAnchor.UpperLeft;
        style.normal.textColor = Color.white;

        string msg = "Glove Calibration System\n";

        if (currentState == CalibrationState.Calibrated)
        {
            msg += "State: Ready/Calibrated\n";
            msg += "Press [Spacebar] to record OPEN HAND (Min Values)\n";
            msg += "Press [Enter] to record CLOSED FIST (Max Values)\n";
        }
        else if (currentState == CalibrationState.CalibratingOpen)
        {
            msg += "State: RECORDING OPEN HAND... Hold Still!\n";
        }
        else if (currentState == CalibrationState.CalibratingClosed)
        {
            msg += "State: RECORDING CLOSED FIST... Hold Still!\n";
        }

        GUI.Box(new Rect(10, 10, 450, 120), msg, style);
    }
}
