
public enum SkillNodeVisualState
{
    Blank,
    Available,
    Learned,
    Blocked
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