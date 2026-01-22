using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using UnityEngine;

public class SnakeAbilities : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Energy energy;

    [Header("Input")]
    [SerializeField] private KeyCode dashKey = KeyCode.E;
    [SerializeField] private KeyCode ghostModeKey = KeyCode.Q;

    [Header("Dash")]
    private float dashSpeedMultiplier = 4f;
    private float dashDuration = 0.15f;
    private float dashCooldown = 0.8f;
    private float dashEnergyCost = 10f;

    [Header("Ghost Mode (Go Through Walls)")]
    private float ghostModeCooldown = 3.0f;
    private float ghostModeEnergyDrainPerSecond = 5.0f;
    private float minEnergyForGhostMode = 15.0f;

    //dash state
    private float dashActiveTime;
    private float dashReadyTime;

    //ghost mode state
    private bool ghostModeRequested;
    private int ghostModeStepsRemaining; // keeps ghost active long enough for body to follow
    private float ghostModeReadyTime;

    public bool GhostActive => ghostModeRequested || ghostModeStepsRemaining > 0;

    private void Update()
    {
        HandleInput();
        GhostModeEnergyDrain();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(dashKey))
            TryDash();


        if (Input.GetKeyDown(ghostModeKey))
            ToggleGhostMode();
    }

    private void ToggleGhostMode()
    {
        if (!ghostModeRequested)
        {
            if (Time.time < ghostModeReadyTime)
                return;

            if (energy != null && energy.CurrentEnergy < minEnergyForGhostMode)
                return;

            ghostModeRequested = true;
            return;
        }

        ghostModeRequested = false;
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