using UnityEngine;
using UnityEngine.UI;

public class SnakeAbilities : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Energy energy;

    [Header("Dash")]
    [SerializeField] private KeyCode dashKey = KeyCode.E;
    [SerializeField] private Image dashImage;
    private bool isDashingCooldown; 
    private float dashSpeedMultiplier = 4f;
    private float dashDuration = 0.15f;
    private float dashCooldown = 0.8f;
    private float dashEnergyCost = 10f;

    [Header("Ghost Mode (Go Through Walls)")]
    [SerializeField] private KeyCode ghostModeKey = KeyCode.Q;
    [SerializeField] private Image ghostModeImage;
    private bool isGhostModeCooldown;
    private float ghostModeCooldown = 3.0f;
    private float ghostModeEnergyDrainPerSecond = 5.0f;
    private float minEnergyForGhostMode = 15.0f;

    //dash state
    private float dashActiveTime;  // when dash effect ends
    private float dashReadyTime;

    //ghost mode state
    private bool ghostModeRequested;
    private int ghostModeStepsRemaining; // keeps ghost active long enough for body to follow
    private float ghostModeReadyTime;

    public bool GhostActive => ghostModeRequested || ghostModeStepsRemaining > 0;

    private void Start()
    {
        dashImage.fillAmount = 0f;
        ghostModeImage.fillAmount = 0f;

        isDashingCooldown = false;
        isGhostModeCooldown = false;
    }

    private void Update()
    {
        HandleDashing();
        HandleGhostMode();
        GhostModeEnergyDrain();
    }

    private void HandleDashing()
    {
        if (Input.GetKeyDown(dashKey) && isDashingCooldown == false && energy.CurrentEnergy >= dashEnergyCost)
        {
            TryDash();
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
        if (Input.GetKeyDown(ghostModeKey) && isGhostModeCooldown == false && energy.CurrentEnergy >= ghostModeEnergyDrainPerSecond) 
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
            ghostModeReadyTime = Time.time + ghostModeCooldown;
        }
        
    }

    public void NotifyHeadEnteredWall(int snakeLength)
    {
        if(ghostModeStepsRemaining <= 0)
            ghostModeStepsRemaining = Mathf.Max(1, snakeLength);
    }

    public void AfterSnakeMoved()
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