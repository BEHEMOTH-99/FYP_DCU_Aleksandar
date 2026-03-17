using UnityEngine;

public class AnatomicalHandController : MonoBehaviour
{
    [Header("Joint Targets (0-1 from Serial)")]
    public float targetPIP = 0f;
    public float targetMCP_Main = 0f;
    public float targetMCP_Secondary = 0f;

    [Header("Current Smoothed Joints")]
    public float currentPIP = 0f;
    public float currentMCP_Main = 0f;
    public float currentMCP_Secondary = 0f;

    public float smoothingSpeed = 15f;

    [Header("Anatomical Settings")]
    [Tooltip("The main curling axis (e.g., Z). Maps to Pot 2.")]
    public RotationAxis anatomicalAxisMain = RotationAxis.Z;
    [Tooltip("The secondary splay/abduction axis (e.g., X). Maps to Pot 3.")]
    public RotationAxis anatomicalAxisSecondary = RotationAxis.X;

    [Header("Index Finger")]
    public Transform index_prox_14; // MCP
    public Transform index_midd_13; // PIP
    public Transform index_dist_12; // DIP

    [Header("Middle Finger")]
    public Transform midd_prox_10; // MCP
    public Transform midd_midd_9;  // PIP
    public Transform midd_dist_8;  // DIP

    // Max rotation angles for realistic human degrees
    private const float MCP_MAX_CURL = 90f;
    private const float MCP_MAX_SPLAY = 25f; // Splay/Abduction limits
    
    private const float PIP_MAX_ANGLE = 100f;
    
    // DIP is biologically tied to the PIP joint. Usually ~ 2/3rds of PIP rotation.
    private const float DIP_RATIO = 0.66f;

    // Initial rest rotations
    private Quaternion i_prox_initial;
    private Quaternion i_midd_initial;
    private Quaternion i_dist_initial;

    private Quaternion m_prox_initial;
    private Quaternion m_midd_initial;
    private Quaternion m_dist_initial;

    void Start()
    {
        // Store initial rest pose local rotations
        if (index_prox_14) i_prox_initial = index_prox_14.localRotation;
        if (index_midd_13) i_midd_initial = index_midd_13.localRotation;
        if (index_dist_12) i_dist_initial = index_dist_12.localRotation;

        if (midd_prox_10) m_prox_initial = midd_prox_10.localRotation;
        if (midd_midd_9)  m_midd_initial = midd_midd_9.localRotation;
        if (midd_dist_8)  m_dist_initial = midd_dist_8.localRotation;
    }

    public void SetTargetInputs(float pot1Norm, float pot2Norm, float pot3Norm)
    {
        targetPIP = pot1Norm;
        targetMCP_Main = pot2Norm;
        
        // Map Pot 3 correctly for splay (-0.5 to 0.5 center point allows positive and negative rotation)
        // Center the pot3 input, so center potentiometer = 0 splay rotation
        targetMCP_Secondary = (pot3Norm - 0.5f) * 2f; 
    }

    void Update()
    {
        // Smooth the inputs before converting to degrees
        currentPIP = Mathf.Lerp(currentPIP, targetPIP, Time.deltaTime * smoothingSpeed);
        currentMCP_Main = Mathf.Lerp(currentMCP_Main, targetMCP_Main, Time.deltaTime * smoothingSpeed);
        currentMCP_Secondary = Mathf.Lerp(currentMCP_Secondary, targetMCP_Secondary, Time.deltaTime * smoothingSpeed);

        Vector3 axisMain = GetEulerAxis(anatomicalAxisMain);
        Vector3 axisSecondary = GetEulerAxis(anatomicalAxisSecondary);

        // Calculate specific rotations for the current smoothed target
        // MCP takes two inputs combining the main curl and the secondary splay
        Quaternion mcpRotMain = Quaternion.AngleAxis(currentMCP_Main * MCP_MAX_CURL, axisMain);
        Quaternion mcpRotSecondary = Quaternion.AngleAxis(currentMCP_Secondary * MCP_MAX_SPLAY, axisSecondary);
        Quaternion mcpFinalRot = mcpRotSecondary * mcpRotMain; // Combine 2DOF

        // PIP receives raw Pot1 input (smoothed)
        float pipAngle = currentPIP * PIP_MAX_ANGLE;
        Quaternion pipRot = Quaternion.AngleAxis(pipAngle, axisMain);

        // DIP auto-calculates based on PIP angle
        Quaternion dipRot = Quaternion.AngleAxis(pipAngle * DIP_RATIO, axisMain);

        // Apply starting + new rotation
        if (index_prox_14) index_prox_14.localRotation = i_prox_initial * mcpFinalRot;
        if (index_midd_13) index_midd_13.localRotation = i_midd_initial * pipRot;
        if (index_dist_12) index_dist_12.localRotation = i_dist_initial * dipRot;

        if (midd_prox_10) midd_prox_10.localRotation = m_prox_initial * mcpFinalRot;
        if (midd_midd_9)  midd_midd_9.localRotation = m_midd_initial * pipRot;
        if (midd_dist_8)  midd_dist_8.localRotation = m_dist_initial * dipRot;
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
