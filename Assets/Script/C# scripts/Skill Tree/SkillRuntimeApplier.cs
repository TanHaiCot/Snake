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

        foreach (SkillData skill in database.AllSkills)
        {
            if (skill != null && progress.HasSkill(skill.skillId))
                ApplySkill(skill);
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

        if (loadedDatabase == null || loadedDatabase.AllSkills == null || loadedProgress == null)
            return false;

        loadedProgress.LoadSavedProgress();

        foreach (SkillData skill in loadedDatabase.AllSkills)
        {
            if (skill != null && skill.effectType == effectType && loadedProgress.HasSkill(skill.skillId))
                return true;
        }

        return false;
    }

    private void ApplySkill(SkillData skill)
    {
        switch (skill.effectType)
        {
            //tier1
            case SkillEffectType.UnlockDash:
                snakeAbilities?.SetDashUnlocked(true);
                break;


            //tier2,5
            case SkillEffectType.ReduceDashCost:
                snakeAbilities?.ReduceDashCost(skill.value);
                break;

            case SkillEffectType.ReduceDashCooldown:
                snakeAbilities?.ReduceDashCooldown(skill.value);
                break;


            //tier3, 5
            case SkillEffectType.IncreaseFoodEnergyGain:
                foodEnergyGain += skill.value;
                break;

            case SkillEffectType.ExtendLevelTimerOnEarlyFood:
                snakeAbilities?.EnableEarlyFoodTimerBonus(skill.value, DefaultFoodPerkWindow);
                break;


            //tier4
            case SkillEffectType.UnlockGhost:
                snakeAbilities?.SetGhostModeUnlocked(true);
                break;


            //tier6
            case SkillEffectType.MultiplyLateFoodEnergyGain:
                snakeAbilities?.EnableLateFoodEnergyBonus(skill.value, DefaultFoodPerkWindow);
                break;

            case SkillEffectType.MaintainGhostModeAfterToggleOff:
                snakeAbilities?.MaintainGhostModeAfterToggleOff(skill.value);
                break;


            //tier7
            case SkillEffectType.SlowEnemiesUsingDash:
                slowEnemyController.EnableUpgrade(skill.value);
                break; 



            case SkillEffectType.IncreaseMaxEnergy:
                energy?.IncreaseMaxEnergy(skill.value);
                break;

            case SkillEffectType.AddStartEnergy:
                energy?.IncreaseStartEnergy(skill.value);
                break;
            case SkillEffectType.ReduceGhostDrain:
                snakeAbilities?.ReduceGhostDrain(skill.value);
                break;

            case SkillEffectType.ReduceGhostCooldown:
                snakeAbilities?.ReduceGhostCooldown(skill.value);
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
