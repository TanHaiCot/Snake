using UnityEngine;

public class SlowEnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SnakeAbilities snakeAbilities;
    [SerializeField] private FirstBoss firstBoss;
    [SerializeField] private AI_Snake[] normalEnemies;

    [Header("SlowSettings")]
    private float slowPercentage;
    private float slowDurationPerDash = 2f;
    
    private bool isUpgradeEnabled = false;

    private void OnEnable()
    {
        if (snakeAbilities != null)
            snakeAbilities.OnDashUsed.AddListener(HandleDashUsed);
    }

    private void OnDisable()
    {
        if (snakeAbilities != null)
            snakeAbilities.OnDashUsed.RemoveListener(HandleDashUsed);
    }

    public void EnableUpgrade(float percentage)
    {
        isUpgradeEnabled = true;
        slowPercentage = Mathf.Clamp01(percentage);
    }

    public void DisableUpgrade()
    {
        isUpgradeEnabled = false;
    }

    private void HandleDashUsed()
    {
        if (!isUpgradeEnabled)
            return; 

        foreach(AI_Snake enemy in normalEnemies)
        {
            if(enemy != null && enemy.isActiveAndEnabled)
            {
                enemy.ApplySlowEffect(slowPercentage, slowDurationPerDash);
            }
        }

        if(firstBoss != null && firstBoss.isActiveAndEnabled)
        {
            firstBoss.ApplySlowEffect(slowPercentage, slowDurationPerDash);   
        }
    }
}
