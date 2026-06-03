using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "ScriptableObjects/LevelData", order = 1)]
public class LevelData : ScriptableObject
{
    [Header("Level Requirements")]
    public int targetScore;
    public float timeLimit; // in seconds

    public bool enableReverseMovement;
    public bool isSkillTreeUnlocked; // Whether the skill tree is unlocked after completing this level
}
