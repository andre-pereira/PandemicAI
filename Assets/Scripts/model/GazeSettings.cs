using OPEN.PandemicAI;
using UnityEngine;
using UnityEngine.XR;

[CreateAssetMenu(fileName = "GazeSettings", menuName = "Furhat/Settings/Gaze")]
public class GazeSettings : ScriptableObject
{
    [Header("Idle State Weights (should some to 1)")]
    [Range(0, 1)] public float WeightScanBoard = 0.5f;
    [Range(0, 1)] public float WeightLookAtUser = 0.45f;
    //[Range(0, 1)] public float WeightJointAttention = 0.2f;
    [Range(0, 1)] public float WeightLookAround = 0.05f;

    [Header("Event Driven Probabilities")]
    [Range(0, 1)] public float ProbabilityUserSpeech = 1f;
    [Range(0, 1)] public float ProbabilityBoardClick = 0.8f;
    [Range(0, 1)] public float ProbabilityBoardAnimation = 0.65f;
    //[Range(0, 1)] public float ProbabilityJointAttention = 0.4f; // Different than the idle state as it can be both reactive or idle

    [Header("Gaze Aversion Calibration")]
    [Range(0, 10)] public float gazeAversionXmin = 4f;
    [Range(0, 10)] public float gazeAversionXmax = 6f;
    [Range(-10, 10)] public float gazeAversionYmin = -2f;
    [Range(-10, 10)] public float gazeAversionYmax = 2f;
    [Range(0, 30)] public float gazeAversionZmin = 10f;
    [Range(0, 30)] public float gazeAversionZmax = 15f;

    [Header("State Durations (Seconds) for idle")]
    public Vector2 LookAtUserTR = new Vector2(1f, 3f);
    public Vector2 ScanBoardTR = new Vector2(0.8f, 2f);
    //public Vector2 JointAttentionTR = new Vector2(0.7f, 1.5f);
    public Vector2 LookAroundTR = new Vector2(1f, 3f);

    [Header("State Durations (Seconds) for event driven")]
    public Vector2 BoardClickTR = new Vector2(0.7f, 1.5f);
    public Vector2 BoardAnimationTR = new Vector2(0.7f, 1.5f);
    public Vector2 UserSpeechTR = new Vector2(15f, 20f);

    [Header("Zones to detect where user is looking at")]
    public Zone robot;
    public Zone board;

    [Header("Gaze Calibration Targets")]
    public Vector3 CalibrationTargetBR = new Vector3(0f, 0f, 0f);
    public Vector3 CalibrationTargetBL = new Vector3(0f, 0f, 0f);
    public Vector3 CalibrationTargetTR = new Vector3(0f, 0f, 0f);
    public Vector3 CalibrationTargetTL = new Vector3(0f, 0f, 0f);

    [Header("Other Settings")]
    public int UserLostTimeMS = 1000; // Time in miliseconds before considering a user lost

    //The original calibration values were:
    //BR(-0.1f, -0.15f, 0.15f),
    //BL(0.1f, -0.15f, 0.15f),
    //TR(-0.2f, -0.35f, 1.1f),
    //TL(0.2f, -0.35f, 1.1f)

    

    public Zone GetZone(UserGazingAt state) => state switch
    {
        UserGazingAt.Robot => robot,
        UserGazingAt.Board => board,
        _ => default
    };

}

[System.Serializable]
public struct Zone
{
    /* -------- ORIENTATION (head yaw/pitch) -------- */
    [Tooltip("Yaw, degrees. ↘ decreases left of centre (≈180°), ↗ increases right")]
    public float yawMin, yawMax;      // e.g. 120–240 deg
    [Tooltip("Pitch, degrees. 0 = straight, ↓ negative, ↑ positive")]
    public float pitchMin, pitchMax;  // e.g. −30 – +30 deg
}