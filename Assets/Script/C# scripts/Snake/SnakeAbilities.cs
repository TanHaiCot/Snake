using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SnakeAbilities : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Energy energy;

    [Header("Dash")]
    [SerializeField] private KeyCode dashKey = KeyCode.E;
    [SerializeField] private Image dashLockImage;
    [SerializeField] private Image dashLockDarkImage;
    public Image dashCooldownImage;
    private bool isDashingCooldown; 
    private float dashSpeedMultiplier = 4f;
    private float dashDuration = 0.15f;  
    private float dashCooldown = 1.5f;   //old value 0.8
    private float dashEnergyCost = 15f;  //old value 10 

    [Header("Ghost Mode (Go Through Walls)")]
    [SerializeField] private KeyCode ghostModeKey = KeyCode.Q;
    [SerializeField] private Image ghostModeLockImage;
    [SerializeField] private Image ghostLockDarkImage;
    public Image ghostModeCooldownImage;
    private bool isGhostModeCooldown;
    private float ghostModeCooldown = 3.0f;
    private float ghostModeEnergyDrainPerSecond = 5.0f;
    //private float minEnergyForGhostMode = 15.0f;

    
    //dash state
    private float dashActiveTime;  // when dash effect ends
    private float dashReadyTime;

    //ghost mode state
    private bool ghostModeRequested;
    private int ghostModeStepsRemaining; // keeps ghost active long enough for body to follow
    private float ghostModeReadyTime;

    public bool abilitiesUnlocked;
    private float debugTimer;

    [SerializeField] private Snake snake;
    private bool lastGhostState;

    public bool GhostActive => ghostModeRequested || ghostModeStepsRemaining > 0;

    private void Start()
    {
        dashCooldownImage.fillAmount = 1f;
        ghostModeCooldownImage.fillAmount = 1f;

        isDashingCooldown = true;
        isGhostModeCooldown = true;

        if (SceneManager.GetActiveScene().name == "Boss1Fight")
            abilitiesUnlocked = false;
        else
            abilitiesUnlocked = true;

    }

    private void Update()
    {
        HandleDashing();
        HandleGhostMode();
        GhostModeEnergyDrain(); 
        UpdateAbilitiesUI();

        DebugEnergy();

        UpdateGhostVisual(); 
    }

    private void UpdateGhostVisual()
    {
        bool current = GhostActive;

        if (current != lastGhostState)
        {
            snake.SetGhostVisual(current);
            lastGhostState = current;
        }
    }

    private void DebugEnergy()
    {
        debugTimer += Time.deltaTime;

        if (debugTimer >= 1f)
        {
            debugTimer = 0f;
            if (energy != null)
                Debug.Log($"Current Energy: {energy.CurrentEnergy}");
        }
    }

    private void UpdateAbilitiesUI()
    {
        bool dashLocked = !abilitiesUnlocked || energy.CurrentEnergy < dashEnergyCost;
        if( dashLocked) dashCooldownImage.fillAmount = 0f; 
        dashLockImage.gameObject.SetActive(dashLocked);
        dashLockDarkImage.gameObject.SetActive(dashLocked);
       

        bool ghostModeLocked =!abilitiesUnlocked || energy.CurrentEnergy < ghostModeEnergyDrainPerSecond;
        if(ghostModeLocked) ghostModeCooldownImage.fillAmount = 0f;
        ghostModeLockImage.gameObject.SetActive(ghostModeLocked);
        ghostLockDarkImage.gameObject.SetActive(ghostModeLocked); 
   
            
    }

    public void SetAbilitiesUnlocked(bool value)
    {
        abilitiesUnlocked = value;

        if(!abilitiesUnlocked)
        {
            ghostModeRequested = false;
        }
        else
        {
            dashCooldownImage.fillAmount = 0f;
            ghostModeCooldownImage.fillAmount = 0f;
        }
    }

    private void HandleDashing()
    {
        if (abilitiesUnlocked && Input.GetKeyDown(dashKey) && isDashingCooldown == false && energy.CurrentEnergy >= dashEnergyCost)
        {
            TryDash();
            //if()
            isDashingCooldown = true;
            dashCooldownImage.fillAmount = 1f;
        }

        if(isDashingCooldown)
        {
            dashCooldownImage.fillAmount -= 1f / dashCooldown * Time.deltaTime;
            if(dashCooldownImage.fillAmount <= 0f)
            {
                dashCooldownImage.fillAmount = 0f;
                isDashingCooldown = false;
            }
        }
    }

    private void HandleGhostMode()
    {
        if (abilitiesUnlocked && Input.GetKeyDown(ghostModeKey) && isGhostModeCooldown == false && energy.CurrentEnergy >= ghostModeEnergyDrainPerSecond) 
        {
            ToggleGhostMode();
            //isGhostModeCooldown = true;
            ghostModeCooldownImage.fillAmount = 1f;
        }

        if(isGhostModeCooldown)
        {
            ghostModeCooldownImage.fillAmount -= 1f / ghostModeCooldown * Time.deltaTime;
            if(ghostModeCooldownImage.fillAmount <= 0f)
            {
                ghostModeCooldownImage.fillAmount = 0f;
                isGhostModeCooldown = false;
            }
        }
    }

    private void ToggleGhostMode()
    {
        if (!ghostModeRequested)
        {
            if (Time.time < ghostModeReadyTime)
                return;

            if (energy != null && energy.CurrentEnergy < ghostModeEnergyDrainPerSecond)
                return;
            
            ghostModeRequested = true;
            return;
        }

        ghostModeRequested = false;
        isGhostModeCooldown = true;
        ghostModeReadyTime = Time.time + ghostModeCooldown;
    }

    private void GhostModeEnergyDrain()
    {
        if (!ghostModeRequested || energy == null)
            return;

        float drain = ghostModeEnergyDrainPerSecond * Time.deltaTime;
        bool stillHaveEnergyToSpend = energy.TryConsumeEnergy(drain);

        if (!stillHaveEnergyToSpend)
        {
            ghostModeRequested = false;
            isGhostModeCooldown = true;
            Debug.Log("Energy depleted, exiting ghost mode.");
            ghostModeReadyTime = Time.time + ghostModeCooldown;
        }
        
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

    private void TryDash()
    {
        if (Time.time < dashReadyTime)
            return;

        if (energy != null && dashEnergyCost > 0)
            if (!energy.TryConsumeEnergy(dashEnergyCost))
                return;

        dashActiveTime = Time.time + dashDuration;
        dashReadyTime = Time.time + dashCooldown;
    }

    public float ModifySpeed(float baseSpeed)
    {
        if (Time.time < dashActiveTime)
        {
            return baseSpeed * dashSpeedMultiplier;
        }

        return baseSpeed;
    }

    public void ResetAbilities(bool unlockAbilities = true)
    {
        // Dash
        isDashingCooldown = true;
        dashActiveTime = 0f;
        dashReadyTime = 0f;

        if (dashCooldownImage != null)
            dashCooldownImage.fillAmount = 1f;

        if (dashLockImage != null)
            dashLockImage.gameObject.SetActive(false);

        if (dashLockDarkImage != null)
            dashLockDarkImage.gameObject.SetActive(false);

        // Ghost
        ghostModeRequested = false;
        ghostModeStepsRemaining = 0;
        ghostModeReadyTime = 0f;
        isGhostModeCooldown = true;

        if (ghostModeCooldownImage != null)
            ghostModeCooldownImage.fillAmount = 1f;

        if (ghostModeLockImage != null)
            ghostModeLockImage.gameObject.SetActive(false);

        if (ghostLockDarkImage != null)
            ghostLockDarkImage.gameObject.SetActive(false);

        // Abilities state
        abilitiesUnlocked = unlockAbilities;

        if (snake != null)
            snake.SetGhostVisual(false);

        lastGhostState = false;
    }



    //change statistic from skill tree
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

}