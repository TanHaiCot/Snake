using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SkillRuntimeApplier
{
    private const string SkillDatabaseResourcePath = "SkillTreeData";
    private const string SkillDatabaseEditorPath = "Assets/Resources/SkillTreeData.asset";
    private const float BaseFoodEnergyGain = 10f;
    private const float DefaultFoodPerkWindow = 10f;

    private readonly SkillTreeData database;
    private readonly PlayerProgress progress;
    private readonly SnakeAbilities snakeAbilities;
    private readonly Energy energy;
    private readonly Timer timer;
    private readonly SlowEnemyController slowEnemyController;

    private float foodEnergyGain = BaseFoodEnergyGain;

    public SkillRuntimeApplier(SnakeAbilities snakeAbilities, Energy energy, Timer timer, SlowEnemyController slowEnemyController)
    {
        database = LoadSkillDatabase();
        progress = PlayerProgress.EnsureInstance();
        this.snakeAbilities = snakeAbilities;
        this.energy = energy;
        this.timer = timer;
        this.slowEnemyController = slowEnemyController;
    }

    public void ApplyLearnedSkills()
    {
        if (database == null || database.AllSkills == null || progress == null)
            return;

        progress.LoadSavedProgress();
        foodEnergyGain = BaseFoodEnergyGain;

        foreach (OwnedSkillProgress ownedSkill in progress.ownedSkills)
        {
            SkillData skill = database.GetSkillByRootId(ownedSkill.rootId);

            if (skill == null)
                continue;

            SkillRank rank = skill.GetRank(ownedSkill.rank);

            if(rank == null)
                continue;

            ApplySkill(skill.effectType, rank.value);

        }
    }

    public void ApplyFoodEatenEffects()
    {
        if (energy != null)
        {
            float energyGain = snakeAbilities != null
                ? snakeAbilities.GetTotalFoodEnergyGain(foodEnergyGain, timer)
                : foodEnergyGain;

            energy.AddEnergy(energyGain);
        }

        if (snakeAbilities != null)
            snakeAbilities.ApplyFoodTimerPerks(timer);
    }

    public static bool HasLearnedEffect(SkillEffectType effectType)
    {
        SkillTreeData loadedDatabase = LoadSkillDatabase();
        PlayerProgress loadedProgress = PlayerProgress.EnsureInstance();

        if (loadedDatabase == null || loadedProgress == null)
            return false;

        loadedProgress.LoadSavedProgress();

        foreach (OwnedSkillProgress owned in loadedProgress.ownedSkills)
        {
            SkillData skill = loadedDatabase.GetSkillByRootId(owned.rootId);

            if (skill != null && skill.effectType == effectType && owned.rank > 0)
                return true;
        }

        return false;
    }

    private void ApplySkill(SkillEffectType effectType, float value)
    {
        switch (effectType)
        {
            case SkillEffectType.UnlockDash:
                snakeAbilities?.SetDashUnlocked(true);
                break;

            case SkillEffectType.ReduceDashCost:
                snakeAbilities?.ReduceDashCost(value);
                break;

            case SkillEffectType.ReduceDashCooldown:
                snakeAbilities?.ReduceDashCooldown(value);
                break;

            case SkillEffectType.IncreaseFoodEnergyGain:
                foodEnergyGain += value;
                break;

            case SkillEffectType.ExtendLevelTimerOnEarlyFood:
                snakeAbilities?.EnableEarlyFoodTimerBonus(value, DefaultFoodPerkWindow);
                break;

            case SkillEffectType.UnlockGhost:
                snakeAbilities?.SetGhostModeUnlocked(true);
                break;

            case SkillEffectType.MultiplyLateFoodEnergyGain:
                snakeAbilities?.EnableLateFoodEnergyBonus(value, DefaultFoodPerkWindow);
                break;

            case SkillEffectType.MaintainGhostModeAfterToggleOff:
                snakeAbilities?.MaintainGhostModeAfterToggleOff(value);
                break;

            case SkillEffectType.SlowEnemiesUsingDash:
                slowEnemyController.EnableUpgrade(value);
                break; 

            case SkillEffectType.IncreaseMaxEnergy:
                energy?.IncreaseMaxEnergy(value);
                break;

            case SkillEffectType.AddStartEnergy:
                energy?.IncreaseStartEnergy(value);
                break;
            case SkillEffectType.ReduceGhostDrain:
                snakeAbilities?.ReduceGhostDrain(value);
                break;

            case SkillEffectType.ReduceGhostCooldown:
                snakeAbilities?.ReduceGhostCooldown(value);
                break;
        }
    }

    private static SkillTreeData LoadSkillDatabase()
    {
        SkillTreeData loadedDatabase = Resources.Load<SkillTreeData>(SkillDatabaseResourcePath);

#if UNITY_EDITOR
        if (loadedDatabase == null)
            loadedDatabase = AssetDatabase.LoadAssetAtPath<SkillTreeData>(SkillDatabaseEditorPath);
#endif

        if (loadedDatabase == null)
            Debug.LogWarning("SkillRuntimeApplier could not find SkillTreeData.");

        return loadedDatabase;
    }
}
