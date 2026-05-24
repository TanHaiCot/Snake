using UnityEngine;


[System.Serializable]
public class SkillRequirementGroup
{
    public SkillData[] oneOfTheseSkills;
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
    public SkillRequirementGroup[] requirementGroups;
    public SkillData oppositeChoice;
}

public enum SkillType
{
    TriggerSkill,
    Perk
}

public enum SkillEffectType
{
    //None,

    //UnlockAbilities,
    UnlockDash,
    UnlockGhost,

    IncreaseMaxEnergy,
    AddStartEnergy,

    ReduceDashCost,
    ReduceDashCooldown,
    //IncreaseDashSpeed,

    ReduceGhostDrain,
    ReduceGhostCooldown,

    IncreaseFoodEnergyGain
}