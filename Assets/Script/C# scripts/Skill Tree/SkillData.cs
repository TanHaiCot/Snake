using UnityEngine;


[System.Serializable]
public class SkillRequirementGroup
{
    public SkillData[] oneOfTheseSkills;
}

public enum RequirementGroupMode
{
    AllGroups,
    AnyGroup
}

[CreateAssetMenu(fileName = "SkillData", menuName = "Scriptable Objects/SkillData")]
public class SkillData : ScriptableObject
{
    public string skillId;
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;

    public SkillType skillType;
    public SkillEffectType effectType;
    public float value;

    public Vector2 uiPosition;

    [Header("Unlock Rules")]
    [Tooltip("All Groups = every group must pass (AND). Any Group = at least one group must pass (OR). Skills inside each group are always OR choices.")]
    public RequirementGroupMode requirementGroupMode = RequirementGroupMode.AllGroups;
    public SkillRequirementGroup[] requirementGroups;
    public SkillData[] oppositeChoices;
}

