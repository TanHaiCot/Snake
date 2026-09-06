using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OwnedSkillProgress
{
    public string rootId;
    public int rank;
}

[System.Serializable]
public class ColumnSelectionProgress
{
    public string columnId;

    public string firstGivenRootId;
    public int firstGivenRank;

    public string secondGivenRootId;
    public int secondGivenRank;

    public int chosenOptionIndex;
    public string chosenRootId;
    public int chosenRank;

    public string GetUnchosenRootId()
    {
        return chosenOptionIndex == 0 ? secondGivenRootId : firstGivenRootId;   
    }

    public int GetUnchosenRank()
    {
        return chosenOptionIndex == 0 ? secondGivenRank : firstGivenRank;
    }
}

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
    
    public List<OwnedSkillProgress> ownedSkills = new();
    public List<ColumnSelectionProgress> columnSelections = new();

    public List<string> seenThemeIntroductionIds = new();

    public int ContinueLevelBuildIndex => Mathf.Max(highestCompletedLevelBuildIndex + 1, SaveSystem.FirstGameplayLevelBuildIndex);



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

    public void EnsureLists()
    {
        ownedSkills ??= new List<OwnedSkillProgress>();
        columnSelections ??= new List<ColumnSelectionProgress>();
        seenThemeIntroductionIds ??= new List<string>();    
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

        EnsureLists(); 
        LoadSavedProgress();
        EnsureLists(); 
    }

    public OwnedSkillProgress GetOwnedSkill(string rootId)
    {
        if(ownedSkills == null)
            ownedSkills = new List<OwnedSkillProgress>();

        return ownedSkills.Find(skill => skill.rootId == rootId);
    }

    public int GetSkillRank(string rootId)
    {
        OwnedSkillProgress ownedSkill = GetOwnedSkill(rootId);
        return ownedSkill != null ? ownedSkill.rank : 0;
    }

    public bool CanUpgradeSkill(SkillData skill)
    {
        return skill != null && GetSkillRank(skill.rootId) < skill.MaxRank;
    }

    public bool UpgradeSkill(SkillData skill)
    {
        if (!CanUpgradeSkill(skill))
            return false;

        OwnedSkillProgress ownedSkillProgress = GetOwnedSkill(skill.rootId);

        if (ownedSkillProgress == null)
        {
            ownedSkillProgress = new OwnedSkillProgress
            {
                rootId = skill.rootId,
                rank = 1
            };
            ownedSkills.Add(ownedSkillProgress);
        }
        else
        {
            ownedSkillProgress.rank = Mathf.Min(ownedSkillProgress.rank + 1, skill.MaxRank);
        }

        return true; 
    }

    public ColumnSelectionProgress GetColumnSelection(string columnId)
    {
        if (columnSelections == null)
            columnSelections = new List<ColumnSelectionProgress>();
     
        return columnSelections.Find(selection => selection.columnId == columnId);
    }

    public bool HasColumnSelection(string columnId)
    {
        return GetColumnSelection(columnId) != null;
    }

    public bool HasSkillRoot(string rootId)
    {
        return GetSkillRank(rootId) > 0;
    }

    public void RecordColumnSelection(
        string columnId, 
        string firstGivenRootId, int firstGivenRank, 
        string secondGivenRootId, int secondGivenRank, 
        int chosenOptionIndex, string chosenRootId, int chosenRank)
    {

        if (HasColumnSelection(columnId))
            return;
        
        columnSelections.Add(new ColumnSelectionProgress
        {
            columnId = columnId,

            firstGivenRootId = firstGivenRootId,
            firstGivenRank = firstGivenRank,

            secondGivenRootId = secondGivenRootId,
            secondGivenRank = secondGivenRank,

            chosenOptionIndex = chosenOptionIndex,
            chosenRootId = chosenRootId,
            chosenRank = chosenRank
        });
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

        EnsureLists(); 

        ownedSkills.Clear();
        columnSelections.Clear();
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
