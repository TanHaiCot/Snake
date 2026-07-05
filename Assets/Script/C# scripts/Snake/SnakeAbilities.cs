using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SnakeAbilities : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Energy energy;
    [SerializeField] private Snake snake;

    [Header("Dash")]
    [SerializeField] private KeyCode dashKey = KeyCode.E;
    [SerializeField] private Image dashLockImage;
    [SerializeField] private Image dashLockDarkImage;
    [FormerlySerializedAs("dashImage")]
    public Image dashCooldownImage;
    private float dashSpeedMultiplier = 4f;
    private float dashDuration = 0.15f;  
    private float dashCooldown = 1.5f;
    private float dashEnergyCost = 15f;

    [Header("Ghost Mode (Go Through Walls)")]
    [SerializeField] private KeyCode ghostModeKey = KeyCode.Q;
    [SerializeField] private Image ghostModeLockImage;
    [SerializeField] private Image ghostLockDarkImage;
    [FormerlySerializedAs("ghostModeImage")]
    public Image ghostModeCooldownImage;
    private float ghostModeCooldown = 3.0f;
    private float ghostModeEnergyDrainPerSecond = 5.0f;
    [SerializeField] private float ghostModeExtensionDuration = 0f;
    private float ghostModeExtensionEndTime;

    [Header("Food Perks")]
    [SerializeField] private float earlyFoodPeriod = 10f;
    [SerializeField] private float extraTimePerFood = 5f;
    [SerializeField] private float lateFoodPeriod = 10f;
    [SerializeField] private float lateFoodEnergyMultiplier = 1.5f;

    private float dashActiveTime;
    private float dashReadyTime;

    private bool ghostModeRequested;
    private int ghostModeStepsRemaining;
    private float ghostModeReadyTime;

    public bool abilitiesUnlocked;
    private bool dashUnlocked;
    private bool ghostModeUnlocked;
    private bool earlyFoodTimeBonusEnabled;
    private bool lateFoodEnergyBonusEnabled;
    private bool lastGhostState;

    private bool baseSettingsCached;
    private float baseDashSpeedMultiplier;
    private float baseDashDuration;
    private float baseDashCooldown;
    private float baseDashEnergyCost;
    private float baseGhostModeCooldown;
    private float baseGhostModeEnergyDrainPerSecond;
    private float baseEarlyFoodPeriod;
    private float baseExtraTimePerFood;
    private float baseLateFoodPeriod;
    private float baseLateFoodEnergyMultiplier;

    public bool GhostActive => ghostModeRequested || ghostModeStepsRemaining > 0 || Time.time < ghostModeExtensionEndTime;

    private bool CanUseDash => abilitiesUnlocked && dashUnlocked;
    private bool CanUseGhostMode => abilitiesUnlocked && ghostModeUnlocked;

    private void Awake()
    {
        CacheBaseSettings();
        ResetAbilities(SceneManager.GetActiveScene().name != "Boss1Fight");
    }

    private void Update()
    {
        HandleDashing();
        HandleGhostMode();
        DrainGhostModeEnergy();
        UpdateAbilitiesUI();
        UpdateGhostVisual(); 
    }

    private void UpdateGhostVisual()
    {
        if (snake == null)
            return;

        bool current = GhostActive;

        if (current != lastGhostState)
        {
            snake.SetGhostVisual(current);
            lastGhostState = current;
        }
    }

    private void UpdateAbilitiesUI()
    {
        bool dashLocked = !CanUseDash || !HasEnoughEnergy(dashEnergyCost);
        SetLockVisible(dashLockImage, dashLocked);
        SetLockVisible(dashLockDarkImage, dashLocked);
        UpdateCooldownFill(dashCooldownImage, dashReadyTime, dashCooldown);

        bool ghostModeLocked = !CanUseGhostMode || !HasEnoughEnergy(ghostModeEnergyDrainPerSecond);
        SetLockVisible(ghostModeLockImage, ghostModeLocked);
        SetLockVisible(ghostLockDarkImage, ghostModeLocked);
        UpdateCooldownFill(ghostModeCooldownImage, ghostModeReadyTime, ghostModeCooldown);
    }

    private void HandleDashing()
    {
        if (CanUseDash && Input.GetKeyDown(dashKey))
            TryDash();
    }

    private void HandleGhostMode()
    {
        if (CanUseGhostMode && Input.GetKeyDown(ghostModeKey))
            ToggleGhostMode();
    }

    private void ToggleGhostMode()
    {
        if (!ghostModeRequested)
        {
            TryStartGhostMode();
            return;
        }

        StopGhostMode(true, true);
    }

    private void TryStartGhostMode()
    {
        if (Time.time < ghostModeReadyTime || !HasEnoughEnergy(ghostModeEnergyDrainPerSecond))
            return;

        ghostModeRequested = true;
    }

    private void StopGhostMode(bool startCooldown, bool maintainAfterStop)
    {
        if (!ghostModeRequested && !GhostActive)
            return;

        ghostModeRequested = false;

        if (maintainAfterStop && ghostModeExtensionDuration > 0f)
            ghostModeExtensionEndTime = Time.time + ghostModeExtensionDuration;
        else
            ghostModeExtensionEndTime = 0f;

        if (startCooldown)
        {
            ghostModeReadyTime = Time.time + ghostModeCooldown;
            UpdateCooldownFill(ghostModeCooldownImage, ghostModeReadyTime, ghostModeCooldown);
        }
    }

    private void DrainGhostModeEnergy()
    {
        if (!ghostModeRequested || energy == null)
            return;

        float drain = ghostModeEnergyDrainPerSecond * Time.deltaTime;
        if (energy.TryConsumeEnergy(drain))
            return;

        StopGhostMode(true, false);
        Debug.Log("Energy depleted, exiting ghost mode.");
    }

    public void NotifyHeadEnteredWall(int snakeLength)
    {
        if(ghostModeStepsRemaining <= 0)
            ghostModeStepsRemaining = Mathf.Max(1, snakeLength);
    }

    public void GhostModeRemaining()
    {
        if(ghostModeStepsRemaining > 0)
            ghostModeStepsRemaining--;
    }

    private bool TryDash()
    {
        if (Time.time < dashReadyTime)
            return false;

        if (energy != null && dashEnergyCost > 0 && !energy.TryConsumeEnergy(dashEnergyCost))
            return false;

        dashActiveTime = Time.time + dashDuration;
        dashReadyTime = Time.time + dashCooldown;
        UpdateCooldownFill(dashCooldownImage, dashReadyTime, dashCooldown);
        return true;
    }

    public float ModifySpeed(float baseSpeed)
    {
        if (Time.time < dashActiveTime)
        {
            return baseSpeed * dashSpeedMultiplier;
        }

        return baseSpeed;
    }

    public float GetTotalFoodEnergyGain(float baseEnergyGain, Timer levelTimer)
    {
        if (lateFoodEnergyBonusEnabled && levelTimer != null && levelTimer.RemainingTime <= lateFoodPeriod)
            return baseEnergyGain * lateFoodEnergyMultiplier;

        return baseEnergyGain;
    }

    public void ApplyFoodTimerPerks(Timer levelTimer)
    {
        if (!earlyFoodTimeBonusEnabled || levelTimer == null)
            return;

        if (levelTimer.ElapsedTime <= earlyFoodPeriod)
            levelTimer.AddTime(extraTimePerFood);
    }

    public void SetAbilitiesUnlocked(bool value)
    {
        abilitiesUnlocked = value;

        if (!abilitiesUnlocked)
            StopGhostMode(false, false);
    }

    public void SetDashUnlocked(bool value)
    {
        dashUnlocked = value;
    }

    public void SetGhostModeUnlocked(bool value)
    {
        ghostModeUnlocked = value;
    }

    public void ResetAbilities(bool unlockAbilities = true)
    {
        CacheBaseSettings();
        ResetSkillAdjustedStats();

        dashActiveTime = 0f;
        dashReadyTime = 0f;

        ghostModeRequested = false;
        ghostModeStepsRemaining = 0;
        ghostModeReadyTime = 0f;
        ghostModeExtensionEndTime = 0f;

        dashUnlocked = false;
        ghostModeUnlocked = false;
        abilitiesUnlocked = unlockAbilities;

        SetImageFill(dashCooldownImage, 0f);
        SetImageFill(ghostModeCooldownImage, 0f);

        UpdateAbilitiesUI();

        if (snake != null)
            snake.SetGhostVisual(false);

        lastGhostState = false;
    }

    private void CacheBaseSettings()
    {
        if (baseSettingsCached)
            return;

        baseDashSpeedMultiplier = dashSpeedMultiplier;
        baseDashDuration = dashDuration;
        baseDashCooldown = dashCooldown;
        baseDashEnergyCost = dashEnergyCost;
        baseGhostModeCooldown = ghostModeCooldown;
        baseGhostModeEnergyDrainPerSecond = ghostModeEnergyDrainPerSecond;
        baseEarlyFoodPeriod = earlyFoodPeriod;
        baseExtraTimePerFood = extraTimePerFood;
        baseLateFoodPeriod = lateFoodPeriod;
        baseLateFoodEnergyMultiplier = lateFoodEnergyMultiplier;
        baseSettingsCached = true;
    }

    private void ResetSkillAdjustedStats()
    {
        dashSpeedMultiplier = baseDashSpeedMultiplier;
        dashDuration = baseDashDuration;
        dashCooldown = baseDashCooldown;
        dashEnergyCost = baseDashEnergyCost;
        ghostModeCooldown = baseGhostModeCooldown;
        ghostModeEnergyDrainPerSecond = baseGhostModeEnergyDrainPerSecond;
        ghostModeExtensionDuration = 0f;
        earlyFoodPeriod = baseEarlyFoodPeriod;
        extraTimePerFood = baseExtraTimePerFood;
        lateFoodPeriod = baseLateFoodPeriod;
        lateFoodEnergyMultiplier = baseLateFoodEnergyMultiplier;
        earlyFoodTimeBonusEnabled = false;
        lateFoodEnergyBonusEnabled = false;
    }

    public void ReduceDashCost(float amount)
    {
        dashEnergyCost = Mathf.Max(1f, dashEnergyCost - amount);
    }

    public void ReduceDashCooldown(float amount)
    {
        dashCooldown = Mathf.Max(0.1f, dashCooldown - amount);
    }

    public void IncreaseDashSpeed(float amount)
    {
        dashSpeedMultiplier += amount;
    }

    public void ReduceGhostDrain(float amount)
    {
        ghostModeEnergyDrainPerSecond = Mathf.Max(1f, ghostModeEnergyDrainPerSecond - amount);
    }

    public void ReduceGhostCooldown(float amount)
    {
        ghostModeCooldown = Mathf.Max(0.5f, ghostModeCooldown - amount);
    }

    public void EnableEarlyFoodTimerBonus(float extraSecondsPerFood, float firstSeconds)
    {
        earlyFoodTimeBonusEnabled = true;
        extraTimePerFood = Mathf.Max(0f, extraSecondsPerFood);
        earlyFoodPeriod = Mathf.Max(0f, firstSeconds);
    }

    public void EnableLateFoodEnergyBonus(float energyMultiplier, float lastSeconds)
    {
        lateFoodEnergyBonusEnabled = true;
        lateFoodEnergyMultiplier = Mathf.Max(1f, energyMultiplier);
        lateFoodPeriod = Mathf.Max(0f, lastSeconds);
    }

    public void MaintainGhostModeAfterToggleOff(float duration)
    {
        ghostModeExtensionDuration = Mathf.Max(0f, duration);
    }

    private bool HasEnoughEnergy(float cost)
    {
        return energy == null || energy.CurrentEnergy >= cost;
    }

    private void UpdateCooldownFill(Image image, float readyTime, float cooldownDuration)
    {
        if (image == null)
            return;

        float remaining = Mathf.Max(readyTime - Time.time, 0f);
        image.fillAmount = cooldownDuration > 0f ? remaining / cooldownDuration : 0f;
    }

    private void SetImageFill(Image image, float amount)
    {
        if (image != null)
            image.fillAmount = amount;
    }

    private void SetLockVisible(Image image, bool visible)
    {
        if (image != null)
            image.gameObject.SetActive(visible);
    }

    public bool IsDashUnlocked => dashUnlocked;
    public bool IsGhostModeUnlocked => ghostModeUnlocked;
    public bool HasEarlyFoodTimerBonus => earlyFoodTimeBonusEnabled;
    public bool HasLateFoodEnergyBonus => lateFoodEnergyBonusEnabled;
    public float GhostModeExtensionDuration => ghostModeExtensionDuration;
}
