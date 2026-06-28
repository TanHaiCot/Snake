using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class SaveData
{
    public int highestCompletedLevelBuildIndex = -1;
    public int completedLevels;
    public int upgradePoints;
    public List<string> chosenSkillIds = new List<string>();
}

public static class SaveSystem
{
    private const string SaveFileName = "player-progress.json";
    public const int FirstGameplayLevelBuildIndex = 1;

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
            chosenSkillIds = new List<string>(progress.chosenSkillIds ?? new List<string>())
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
        progress.chosenSkillIds = data.chosenSkillIds ?? new List<string>();

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
        if (data.chosenSkillIds == null)
            data.chosenSkillIds = new List<string>();

        if (data.highestCompletedLevelBuildIndex < -1)
            data.highestCompletedLevelBuildIndex = -1;

        if (data.completedLevels <= 0)
            return;

        int completedLevelIndexFromCount = FirstGameplayLevelBuildIndex + data.completedLevels - 1;

        if (data.highestCompletedLevelBuildIndex < completedLevelIndexFromCount)
            data.highestCompletedLevelBuildIndex = completedLevelIndexFromCount;
    }
}
