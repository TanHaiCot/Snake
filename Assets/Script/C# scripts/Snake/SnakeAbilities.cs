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
    public Image dashImage;
    private bool isDashingCooldown; 
    private float dashSpeedMultiplier = 4f;
    private float dashDuration = 0.15f;
    private float dashCooldown = 0.8f;
    private float dashEnergyCost = 10f;

    [Header("Ghost Mode (Go Through Walls)")]
    [SerializeField] private KeyCode ghostModeKey = KeyCode.Q;
    [SerializeField] private Image ghostModeLockImage;
    [SerializeField] private Image ghostLockDarkImage;
    public Image ghostModeImage;
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
        dashImage.fillAmount = 1f;
        ghostModeImage.fillAmount = 1f;

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
        if( dashLocked) dashImage.fillAmount = 0f; 
        dashLockImage.gameObject.SetActive(dashLocked);
        dashLockDarkImage.gameObject.SetActive(dashLocked);
       

        bool ghostModeLocked =!abilitiesUnlocked || energy.CurrentEnergy < ghostModeEnergyDrainPerSecond;
        if(ghostModeLocked) ghostModeImage.fillAmount = 0f;
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
            dashImage.fillAmount = 0f;
            ghostModeImage.fillAmount = 0f;
        }
    }

    private void HandleDashing()
    {
        if (abilitiesUnlocked && Input.GetKeyDown(dashKey) && isDashingCooldown == false && energy.CurrentEnergy >= dashEnergyCost)
        {
            TryDash();
            //if()
            isDashingCooldown = true;
            dashImage.fillAmount = 1f;
        }

        if(isDashingCooldown)
        {
            dashImage.fillAmount -= 1f / dashCooldown * Time.deltaTime;
            if(dashImage.fillAmount <= 0f)
            {
                dashImage.fillAmount = 0f;
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
            ghostModeImage.fillAmount = 1f;
        }

        if(isGhostModeCooldown)
        {
            ghostModeImage.fillAmount -= 1f / ghostModeCooldown * Time.deltaTime;
            if(ghostModeImage.fillAmount <= 0f)
            {
                ghostModeImage.fillAmount = 0f;
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


}