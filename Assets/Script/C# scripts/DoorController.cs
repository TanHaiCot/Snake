using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorController : MonoBehaviour
{
    [SerializeField] private BoxCollider2D nextSceneTrigger;
    [SerializeField] private GameObject door;

    private void Awake()
    {
        //nextSceneTrigger = GetComponent<BoxCollider2D>();
        
        nextSceneTrigger.enabled = false;
    }
    public void Open()
    {
        door.SetActive(false);
        nextSceneTrigger.enabled = true;
    }
}
