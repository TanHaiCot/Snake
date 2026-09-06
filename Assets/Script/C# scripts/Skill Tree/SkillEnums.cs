
public enum SkillNodeVisualState
{
    Blank,
    Available,
    Learned,
    Blocked
}

public enum SkillEffectType
{
    UnlockDash = 0,
  
    ReduceDashCost = 1,
    ReduceDashCooldown = 2,
    
    IncreaseFoodEnergyGain = 3,
    ExtendLevelTimerOnEarlyFood = 4,
    
    UnlockGhost = 5,
    
    MultiplyLateFoodEnergyGain = 6,
    MaintainGhostModeAfterToggleOff = 7,
    
    SlowEnemiesUsingDash = 8,

    IncreaseMaxEnergy = 9,
    AddStartEnergy = 10,

    ReduceGhostDrain = 11,
    ReduceGhostCooldown = 12,
}
