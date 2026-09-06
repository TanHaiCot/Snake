using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class SaveData
{
    public int highestCompletedLevelBuildIndex = -1;
    public int completedLevels;
    public int upgradePoints;   
    public bool skillTreeUnlocked;
    public bool hasSavedEnergy;
    public float savedEnergy;

    public List<OwnedSkillProgress> ownedSkills = new List<OwnedSkillProgress>();
    public List<ColumnSelectionProgress> columnSelections = new List<ColumnSelectionProgress>();
    public List<string> seenThemeIntroductionIds = new List<string>();
}

public static class SaveSystem
{
    private const string SaveFileName = "player-progress.json";
    public const int FirstGameplayLevelBuildIndex = 1;
    private const int FirstSkillTreeUnlockLevelBuildIndex = 3;

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static bool HasPlayableSave()
    {
        if (!TryReadData(out SaveData data))
            return false;

        NormalizeData(data);
        return data.highestCompletedLevelBuildIndex >= FirstGameplayLevelBuildIndex;
    }

    public static void Save(PlayerProgress progress)
    {
        if (progress == null)
            return;

        SaveData data = new SaveData
        {
            highestCompletedLevelBuildIndex = progress.highestCompletedLevelBuildIndex,

            completedLevels = progress.completedLevels,
            upgradePoints = progress.upgradePoints,
            skillTreeUnlocked = progress.skillTreeUnlocked,
            hasSavedEnergy = progress.hasSavedEnergy,
            savedEnergy = progress.savedEnergy,

            ownedSkills = new List<OwnedSkillProgress>(progress.ownedSkills ?? new List<OwnedSkillProgress>()),
            columnSelections = new List<ColumnSelectionProgress>(progress.columnSelections ?? new List<ColumnSelectionProgress>()),

            seenThemeIntroductionIds = new List<string>(progress.seenThemeIntroductionIds ?? new List<string>())
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    public static bool Load(PlayerProgress progress)
    {
        if (progress == null || !TryReadData(out SaveData data))
            return false;

        NormalizeData(data);

        progress.highestCompletedLevelBuildIndex = data.highestCompletedLevelBuildIndex;

        progress.currentLevelBuildIndex = data.highestCompletedLevelBuildIndex;
        progress.completedLevels = data.completedLevels;
        progress.upgradePoints = data.upgradePoints;
        progress.skillTreeUnlocked = data.skillTreeUnlocked;
        progress.hasSavedEnergy = data.hasSavedEnergy;
        progress.savedEnergy = data.savedEnergy;

        progress.ownedSkills = data.ownedSkills ?? new List<OwnedSkillProgress>();
        progress.columnSelections = data.columnSelections ?? new List<ColumnSelectionProgress>();

        progress.seenThemeIntroductionIds = data.seenThemeIntroductionIds ?? new List<string>();

        Save(progress);
        return true;
    }

    public static void DeleteSave()
    {
        if (HasSave())
            File.Delete(SavePath);
    }

    private static bool TryReadData(out SaveData data)
    {
        data = null;

        if (!HasSave())
            return false;

        string json = File.ReadAllText(SavePath);
        data = JsonUtility.FromJson<SaveData>(json);

        return data != null;
    }

    private static void NormalizeData(SaveData data)
    {
        if (data.ownedSkills == null)
            data.ownedSkills = new List<OwnedSkillProgress>();

        if (data.columnSelections == null)
            data.columnSelections = new List<ColumnSelectionProgress>();
       
        if (data.seenThemeIntroductionIds == null)
            data.seenThemeIntroductionIds = new List<string>();

        data.savedEnergy = Mathf.Max(0f, data.savedEnergy);

        if (data.highestCompletedLevelBuildIndex < -1)
            data.highestCompletedLevelBuildIndex = -1;

        bool hasSkillTreeProgress = data.ownedSkills.Count > 0 || data.columnSelections.Count > 0;

            //if (!data.skillTreeUnlocked && hasSkillTreeProgress)
            //    data.skillTreeUnlocked = true;

        if (data.completedLevels > 0)
        {
            int completedLevelIndexFromCount = FirstGameplayLevelBuildIndex + data.completedLevels - 1;

            if (data.highestCompletedLevelBuildIndex < completedLevelIndexFromCount)
                data.highestCompletedLevelBuildIndex = completedLevelIndexFromCount;
        }

        if (!data.skillTreeUnlocked && data.highestCompletedLevelBuildIndex >= FirstSkillTreeUnlockLevelBuildIndex)
            data.skillTreeUnlocked = true;
    }
}
