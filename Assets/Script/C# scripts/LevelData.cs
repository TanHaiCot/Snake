using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    [Header("Level Requirements")]
    public int targetScore;
    public float timeLimit; // in seconds

    [Header("Level Audio")]
    public AudioClip backgroundMusic;

    [Header("Theme Introduction")]
    public string themeIntroductionId;
    public string themeIntroductionTitle;

    [Header("Level Scoreboard")]
    [Min(0f)] public float gradeSTime = 5f;
    [Min(0f)] public float gradeATime = 10f;
    [Min(0f)] public float gradeBTime = 12f;
    [Min(0f)] public float gradeCTime = 15f;

    public bool isShowingLevelSummary; 
    public bool enableReverseMovement;
    public bool isSkillTreeUnlocked; // Whether the skill tree is unlocked after completing this level
}
