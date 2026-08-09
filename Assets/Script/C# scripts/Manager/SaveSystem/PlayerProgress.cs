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
    public bool skillTreeUnlocked;
    public bool hasSavedEnergy;
    public float savedEnergy;
    
    public List<string> chosenSkillIds = new List<string>();
    public List<string> seenThemeIntroductionIds = new List<string>();

    public int ContinueLevelBuildIndex => Mathf.Max(highestCompletedLevelBuildIndex + 1, SaveSystem.FirstGameplayLevelBuildIndex);

    public bool HasSkill(string skillId)
    {
        if (chosenSkillIds == null)
            return false;

        return chosenSkillIds.Contains(skillId);
    }


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

        if (seenThemeIntroductionIds == null)
            seenThemeIntroductionIds = new List<string>();

        LoadSavedProgress();
    }

    public void AddUpgradePoint()
    {
        upgradePoints++;
    }

    public bool CompleteLevel(int levelBuildIndex, bool levelUnlocksSkillTree)
    {
        currentLevelBuildIndex = levelBuildIndex;

        if (levelBuildIndex <= highestCompletedLevelBuildIndex)
            return false;

        bool shouldAwardUpgradePoint = skillTreeUnlocked || levelUnlocksSkillTree;
        skillTreeUnlocked = skillTreeUnlocked || levelUnlocksSkillTree;
        highestCompletedLevelBuildIndex = levelBuildIndex;
        completedLevels++;

        if (shouldAwardUpgradePoint)
            AddUpgradePoint();

        SaveProgress();

        return true;
    }

    public void SaveProgress()
    {
        SaveSystem.Save(this);
    }

    public void SetSavedEnergy(float energyAmount)
    {
        savedEnergy = Mathf.Max(0f, energyAmount);
        hasSavedEnergy = true;
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
        skillTreeUnlocked = false;
        hasSavedEnergy = false;
        savedEnergy = 0f;

        if (chosenSkillIds == null)
            chosenSkillIds = new List<string>();

        if (seenThemeIntroductionIds == null)
            seenThemeIntroductionIds = new List<string>();

        chosenSkillIds.Clear();
        seenThemeIntroductionIds.Clear();
    }

    public bool HasSeenThemeIntroduction(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId))
            return true;

        return seenThemeIntroductionIds != null && 
               seenThemeIntroductionIds.Contains(themeId);
    }

    public void MarkThemeIntroductionSeen(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId))
            return;

        if (seenThemeIntroductionIds == null)
            seenThemeIntroductionIds = new List<string>();

        if (seenThemeIntroductionIds.Contains(themeId))
            return;

        seenThemeIntroductionIds.Add(themeId);
        SaveProgress();
    }
}
