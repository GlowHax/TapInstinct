using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueStep
{
    [TextArea(2, 5)]
    public string npcMessage;
    public float npcDelay = 1.0f;
    [TextArea(2, 5)]
    public string playerTargetPrompt;
    public float timeLimit = 0f; // 0 = kein Limit
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "TapInstinct/Level Data")]
public class LevelDataSO : ScriptableObject
{
    [Header("Contact Information")]
    public string levelId = "level_1";
    public string contactName = "Alex";
    public string avatarInitial = "A";
    public Color32 avatarColor = new Color32(59, 114, 214, 255);
    [TextArea(1, 3)]
    public string statusOrBio = "Zuletzt online heute um 14:20";
    public bool isUnlocked = true;

    [Header("Requirements")]
    public float requiredAccuracy = 80f;

    [Header("Dialogue Flow")]
    public List<DialogueStep> steps = new List<DialogueStep>();
}
