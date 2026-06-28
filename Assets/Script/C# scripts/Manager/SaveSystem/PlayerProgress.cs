using System.Collections.Generic;
using UnityEngine;

public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress Instance;

    public bool openedSkillTreeFromLevel;
    public int currentLevelBuildIndex = -1;
    public int highestCompletedLevelBuildIndex = -1;
    public int upgradePoints;
    public int completedLevels = 0;
    public int ContinueLevelBuildIndex => Mathf.Max(highestCompletedLevelBuildIndex + 1, SaveSystem.FirstGameplayLevelBuildIndex);

    public bool HasSkill(string skillId)
    {
        if (chosenSkillIds == null)
            return false;

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

        LoadSavedProgress();
    }

    public void AddUpgradePoint()
    {
        upgradePoints++;
    }

    public bool CompleteLevel(int levelBuildIndex)
    {
        currentLevelBuildIndex = levelBuildIndex;

        if (levelBuildIndex <= highestCompletedLevelBuildIndex)
            return false;

        highestCompletedLevelBuildIndex = levelBuildIndex;
        completedLevels++;
        AddUpgradePoint();
        SaveProgress();

        return true;
    }

    public void SaveProgress()
    {
        SaveSystem.Save(this);
    }

    public bool LoadSavedProgress()
    {
        return SaveSystem.Load(this);
    }

    public void ResetProgress()
    {
        openedSkillTreeFromLevel = false;
        currentLevelBuildIndex = -1;
        highestCompletedLevelBuildIndex = -1;
        upgradePoints = 0;
        completedLevels = 0;

        if (chosenSkillIds == null)
            chosenSkillIds = new List<string>();

        chosenSkillIds.Clear();
    }
}
