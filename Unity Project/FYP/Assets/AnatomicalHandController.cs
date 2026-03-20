using UnityEngine;

public enum RotationAxis
{
    X,
    Y,
    Z,
    NegativeX,
    NegativeY,
    NegativeZ
}

[System.Serializable]
public class FingerJoints
{
    public string fingerName = "Finger";
    
    [Header("Joint Transforms")]
    public Transform mcp; // Proximal
    public Transform pip; // Middle
    public Transform dip; // Distal

    [HideInInspector] public Quaternion mcp_initial;
    [HideInInspector] public Quaternion pip_initial;
    [HideInInspector] public Quaternion dip_initial;

    [Header("Current Smoothed Targets")]
    public float currentPIP = 0f;
    public float currentMCP_Main = 0f;
    public float currentMCP_Secondary = 0f;
    
    [HideInInspector] public float targetPIP = 0f;
    [HideInInspector] public float targetMCP_Main = 0f;
    [HideInInspector] public float targetMCP_Secondary = 0f;
}

public class AnatomicalHandController : MonoBehaviour
{
    [Header("Finger Setup (Index, Middle, Ring, Pinky, Thumb)")]
    // Create exactly 5 fingers in the inspector by default
    public FingerJoints[] fingers = new FingerJoints[5];

    public float smoothingSpeed = 15f;

    [Header("Anatomical Settings")]
    [Tooltip("The main curling axis (e.g., Z). Maps to 2nd Pot in cluster.")]
    public RotationAxis anatomicalAxisMain = RotationAxis.Z;
    [Tooltip("The secondary splay/abduction axis (e.g., X). Maps to 3rd Pot in cluster.")]
    public RotationAxis anatomicalAxisSecondary = RotationAxis.X;

    // Max rotation angles for realistic human degrees
    private const float MCP_MAX_CURL = 90f;
    private const float MCP_MAX_SPLAY = 30f; // Updated for 30-degree calibration
    private const float PIP_MAX_ANGLE = 90f; // Updated for 90-degree calibration
    
    // DIP is biologically tied to the PIP joint. Usually ~ 2/3rds of PIP rotation.
    private const float DIP_RATIO = 0.66f;

    void Start()
    {
        // Name them neatly for the inspector if they are empty
        string[] names = { "Index", "Middle", "Ring", "Pinky", "Thumb" };
        for (int i = 0; i < fingers.Length; i++)
        {
            if (fingers[i] == null) fingers[i] = new FingerJoints();
            if (string.IsNullOrEmpty(fingers[i].fingerName) || fingers[i].fingerName == "Finger") 
                fingers[i].fingerName = names[i];

            // Store initial rest pose local rotations
            if (fingers[i].mcp) fingers[i].mcp_initial = fingers[i].mcp.localRotation;
            if (fingers[i].pip) fingers[i].pip_initial = fingers[i].pip.localRotation;
            if (fingers[i].dip) fingers[i].dip_initial = fingers[i].dip.localRotation;
        }
    }

    public void SetTargetInputs(float[] sensorArray)
    {
        // NEW MAPPING: 
        // sensorArray[0,1,2] = Middle  (H, V, V)
        // sensorArray[3,4,5] = Index   (H, V, V)
        // sensorArray[6,7,8] = Thumb   (H, V, V)
        
        // 1. MIDDLE FINGER (fingers[1])
        if (fingers.Length > 1 && 2 < sensorArray.Length) {
            fingers[1].targetMCP_Secondary = (sensorArray[0] / MCP_MAX_SPLAY) * 2f - 1f;
            fingers[1].targetMCP_Main = sensorArray[1] / MCP_MAX_CURL;
            fingers[1].targetPIP = sensorArray[2] / PIP_MAX_ANGLE;
        }

        // 2. INDEX FINGER (fingers[0])
        if (fingers.Length > 0 && 5 < sensorArray.Length) {
            fingers[0].targetMCP_Secondary = (sensorArray[3] / MCP_MAX_SPLAY) * 2f - 1f;
            fingers[0].targetMCP_Main = sensorArray[4] / MCP_MAX_CURL;
            fingers[0].targetPIP = sensorArray[5] / PIP_MAX_ANGLE;
        }

        // 3. THUMB FINGER (fingers[4])
        if (fingers.Length > 4 && 8 < sensorArray.Length) {
            fingers[4].targetMCP_Secondary = (sensorArray[6] / MCP_MAX_SPLAY) * 2f - 1f;
            fingers[4].targetMCP_Main = sensorArray[7] / MCP_MAX_CURL;
            fingers[4].targetPIP = sensorArray[8] / PIP_MAX_ANGLE;
        }
    }

    void Update()
    {
        Vector3 axisMain = GetEulerAxis(anatomicalAxisMain);
        Vector3 axisSecondary = GetEulerAxis(anatomicalAxisSecondary);

        foreach (var finger in fingers)
        {
            // Smooth the inputs before converting to degrees
            finger.currentPIP = Mathf.Lerp(finger.currentPIP, finger.targetPIP, Time.deltaTime * smoothingSpeed);
            finger.currentMCP_Main = Mathf.Lerp(finger.currentMCP_Main, finger.targetMCP_Main, Time.deltaTime * smoothingSpeed);
            finger.currentMCP_Secondary = Mathf.Lerp(finger.currentMCP_Secondary, finger.targetMCP_Secondary, Time.deltaTime * smoothingSpeed);

            // Calculate specific rotations for the current smoothed target
            Quaternion mcpRotMain = Quaternion.AngleAxis(finger.currentMCP_Main * MCP_MAX_CURL, axisMain);
            Quaternion mcpRotSecondary = Quaternion.AngleAxis(finger.currentMCP_Secondary * MCP_MAX_SPLAY, axisSecondary);
            Quaternion mcpFinalRot = mcpRotSecondary * mcpRotMain; // Combine 2DOF

            // PIP receives raw Pot1 input (smoothed)
            float pipAngle = finger.currentPIP * PIP_MAX_ANGLE;
            Quaternion pipRot = Quaternion.AngleAxis(pipAngle, axisMain);

            // DIP auto-calculates based on PIP angle
            Quaternion dipRot = Quaternion.AngleAxis(pipAngle * DIP_RATIO, axisMain);

            // Apply starting + new rotation
            if (finger.mcp) finger.mcp.localRotation = finger.mcp_initial * mcpFinalRot;
            if (finger.pip) finger.pip.localRotation = finger.pip_initial * pipRot;
            if (finger.dip) finger.dip.localRotation = finger.dip_initial * dipRot;
        }
    }

    private Vector3 GetEulerAxis(RotationAxis rotAxis)
    {
        switch (rotAxis)
        {
            case RotationAxis.X: return Vector3.right;
            case RotationAxis.Y: return Vector3.up;
            case RotationAxis.Z: return Vector3.forward;
            case RotationAxis.NegativeX: return Vector3.left;
            case RotationAxis.NegativeY: return Vector3.down;
            case RotationAxis.NegativeZ: return Vector3.back;
            default: return Vector3.right;
        }
    }
}
