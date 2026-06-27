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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static PlayerProgress EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        PlayerProgress existingProgress = FindFirstObjectByType<PlayerProgress>();
        if (existingProgress != null)
        {
            existingProgress.SetAsInstance();
            return existingProgress;
        }

        GameObject progressObject = new GameObject(nameof(PlayerProgress));
        return progressObject.AddComponent<PlayerProgress>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SetAsInstance();
    }

    private void SetAsInstance()
    {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if (chosenSkillIds == null)
            chosenSkillIds = new List<string>();
    }

    public void AddUpgradePoint()
    {
        upgradePoints++;
    }
}
