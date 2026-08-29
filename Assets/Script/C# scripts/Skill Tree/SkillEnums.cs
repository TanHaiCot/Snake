
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

    //tier1
    UnlockDash,

    //tier2, 5
    ReduceDashCost,
    ReduceDashCooldown,

    //tier3, 5
    IncreaseFoodEnergyGain,
    ExtendLevelTimerOnEarlyFood,

    //tier4
    UnlockGhost,

    //tier6
    MultiplyLateFoodEnergyGain,
    MaintainGhostModeAfterToggleOff,
    
    //tier7
    SlowEnemiesUsingDash,

    IncreaseMaxEnergy,
    AddStartEnergy,

    ReduceGhostDrain,
    ReduceGhostCooldown,
    
}
