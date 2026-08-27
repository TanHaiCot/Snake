using UnityEngine;

public class BossPowerUp : MonoBehaviour
{
    private BossFightManager bossFightManager;
    private bool collected;

    public void SetManager(BossFightManager mgr) => bossFightManager = mgr;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collected || !collision.CompareTag("Player"))
            return;

        collected = true;

        if (bossFightManager != null)
            bossFightManager.CollectPowerUp(this);
        else
            Destroy(gameObject);
        
    }
}
