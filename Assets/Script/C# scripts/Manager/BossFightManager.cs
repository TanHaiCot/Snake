using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossFightManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SnakeAbilities snakeAbilities;
    [SerializeField] private Energy energy;
    [SerializeField] private FirstBoss firstBoss;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BossPowerUp bossPowerUpPrefab;
    private BossPowerUp activePowerUp;
    
    [Header("Empower Settings")]
    private float empowerDuration = 6f;
    private float energyFilledOnEmpower = 999f;

    [Header("Boss HP")]
    private int bossHP = 5;

    private float empowerEndTime;
    private bool isEmpowered;
    private int bossHits;
    private bool collisionLocked; // to prevent multiple collision handling in one hit\

    public bool IsEmpowered => isEmpowered;

    private void Start()
    {
        if(snakeAbilities != null)
            snakeAbilities.SetAbilitiesUnlocked(false);
    }

    private void Update()
    {
        if (collisionLocked && firstBoss != null && firstBoss.IsDangerous)        
            collisionLocked = false;
        
        if (!isEmpowered) return;
        
        if(Time.time > empowerEndTime)
        {
            EmpowerEnd();
            return; 
        }    

        if(energy != null) 
            energy.AddEnergy(energyFilledOnEmpower * Time.deltaTime);
    }

    public void CollectPowerUp(BossPowerUp collectedPowerUp)
    {
        if(collectedPowerUp ==  null) return;   

        if(collectedPowerUp != activePowerUp) return;

        activePowerUp = null;
        StartEmpowerMode(); 
        Destroy(collectedPowerUp.gameObject);
    }

    public void StartEmpowerMode()
    {
        isEmpowered = true;
        empowerEndTime = Time.time + empowerDuration;

        if (snakeAbilities != null)
            snakeAbilities.SetAbilitiesUnlocked(true);
    }


    private void EmpowerEnd()
    {
        if(!isEmpowered) return;

        isEmpowered = false;

        if(energy != null)
            energy.TryConsumeEnergy(energyFilledOnEmpower); //energy turn to 0 when empower ends

        if(snakeAbilities != null)
            snakeAbilities.SetAbilitiesUnlocked(false);
    }

    public bool HandleCollisionBetweenSnakeAndBoss()
    {
       
        if (firstBoss == null) return false;

        // Ignore collision while boss is stunned or dead
        if (!firstBoss.IsDangerous) return false;

        if (collisionLocked) return true;

        collisionLocked = true;

        if (isEmpowered)
        {
            RegisterBossHit();
            EmpowerEnd();
            firstBoss.StunnedByHit();
            return false; 
        }
        else
        {
            gameManager.GameOver();
            AudioManager.Instance?.playSFX(AudioManager.Instance.gameOver);
            return true;
        }
    }
    public void RegisterBossHit()
    {
        bossHits++;
        if (bossHits >= bossHP)
        {
            firstBoss?.Dead();
            CleanupPowerUp();
        }
    }

    public void ResetBossFight()
    {
        CleanupPowerUp();

        bossHits = 0;
        isEmpowered = false;
        collisionLocked = false;
        empowerEndTime = 0f; 

        if (snakeAbilities != null)
            snakeAbilities.SetAbilitiesUnlocked(false);

        firstBoss?.Restate();
    }

    public void SpawnPowerUp(Vector3 spawnPosition)
    {
        if (activePowerUp != null)
            return;

        if (bossPowerUpPrefab == null)
            return;

        activePowerUp = Instantiate(
            bossPowerUpPrefab,
            spawnPosition,
            Quaternion.identity
        );

        activePowerUp.SetManager(this);
    }

    private void CleanupPowerUp()
    {
        if (activePowerUp != null)
            Destroy(activePowerUp.gameObject);

        activePowerUp = null;
    }
}



