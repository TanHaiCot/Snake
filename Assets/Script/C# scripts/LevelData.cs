using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "ScriptableObjects/LevelData", order = 1)]
public class LevelData : ScriptableObject
{
    [Header("Level Requirements")]
    public int targetScore;
    public float timeLimit; // in seconds

}
