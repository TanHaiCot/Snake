using UnityEngine;

public class BossFightManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SnakeAbilities snakeAbilities;
    [SerializeField] private Energy energy;
    [SerializeField] private FirstBoss firstBoss;

    [Header("Empower Settings")]
    private float empowerDuration = 6f;
    private float energyFilledOnEmpower = 999f;

    [Header("Boss HP")]
    private int bossHP = 5;

    private float empowerEndTime;
    private bool isEmpowered;
    private int bossHits; 

    public bool IsEmpowered => isEmpowered;

    private void Start()
    {
        if(snakeAbilities != null)
            snakeAbilities.enabled = false;
    }

    private void Update()
    {
        if (!isEmpowered) return;
        
        if(Time.time > empowerEndTime)
        {
            isEmpowered = false;    
            if(snakeAbilities != null)
                snakeAbilities.enabled = false;
            return; 
        }    

        if(energy != null) 
            energy.AddEnergy(energyFilledOnEmpower * Time.deltaTime);
    }

    public void OnBossPowerUpEaten()
    {
        isEmpowered = true;
        empowerEndTime = Time.time + empowerDuration;

        if (snakeAbilities != null)
            snakeAbilities.enabled = true;
    }

    public void RegisterBossHit()
    {
        bossHits++;
        if (bossHits >= bossHP)
            firstBoss?.Die();
    }
}



