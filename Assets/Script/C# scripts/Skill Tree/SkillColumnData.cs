using UnityEngine;

public enum SkillOptionType
{
    NewSkill,
    SkillUpgradeChosen,
    SkillUpgradeSkipped
}

[System.Serializable]
public class SkillOptionBaseInfo
{
    public SkillOptionType optionType;

    [Tooltip("Used only for new skill.")]
    public SkillData newSkill;

    [Tooltip("Used by UpgradeChosen and UpgradeSkipped.")]
    public SkillColumnData sourceColumn;

    public Vector2 uiPosition;
}

[CreateAssetMenu(fileName = "SkillColumnData", menuName = "Scriptable Objects/Column Data")]
public class SkillColumnData : ScriptableObject
{
    [Header("Identity")]
    public string columnId;
    public int columnNumber;

    [Header("Unlock")]
    public SkillColumnData[] prerequisiteColumns;

    [Header("Options")]
    public SkillOptionBaseInfo[] options;
}

