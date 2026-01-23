using UnityEngine;

[CreateAssetMenu(fileName = "Config", menuName = "Furhat/Settings/Config")]

public class FurhatConfig : ScriptableObject
{
    public string JSONFileName = "event-dialog-mapping-flow.json";
    public float reactiveCommentDelay = 1.0f; // Time in seconds before making a reactive comment
    public float handleEventTime = 3.0f; // Time in seconds to wait before handling the next event
    public float planningPhase = 5.0f;
    public float startedSpeakingCutoff = 10.0f;
    public int DialogActStartingUtterance;
}
