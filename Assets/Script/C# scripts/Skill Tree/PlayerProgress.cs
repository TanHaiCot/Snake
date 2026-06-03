using System.Collections.Generic;
using UnityEngine;

public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress Instance;

    public bool openedSkillTreeFromLevel;
    public int currentLevelBuildIndex;
    public int upgradePoints;
    public int completedLevels = 0;

    public bool HasSkill(string skillId)
    {
        return chosenSkillIds.Contains(skillId);
    }

    public List<string> chosenSkillIds = new List<string>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddUpgradePoint()
    {
        upgradePoints++;
    }
}