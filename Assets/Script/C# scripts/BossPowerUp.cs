using UnityEngine;

public class BossPowerUp : MonoBehaviour
{
    private BossFightManager bossFightManager;

    public void SetManager(BossFightManager mgr) => bossFightManager = mgr;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            bossFightManager?.OnBossPowerUpEaten();
            Destroy(gameObject);
        }
    }
}
