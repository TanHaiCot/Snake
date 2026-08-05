using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorManager : MonoBehaviour
{
    [Header("Level Exit Door")]
    [SerializeField] private BoxCollider2D nextSceneTrigger;
    [SerializeField] private GameObject mainDoor;

    [Header("Colored Doors Rule")]
    [SerializeField] private ColoredDoor[] coloredDoors;
    [SerializeField] private Food.FoodColors mainDoorColor;
    [SerializeField] private bool isColoredDoorRuleActive;


    private void Awake()
    {
        if(nextSceneTrigger != null)
            nextSceneTrigger.enabled = false;
    }

    public bool CollectFood(Food.FoodColors collectedColor)
    {
        if(!isColoredDoorRuleActive)
            return true;

        foreach (ColoredDoor door in coloredDoors)
        {
            if (door != null)
                door.RecordCollectedFood(collectedColor);
        }

        // Only this color contributes toward the main exit.
        return collectedColor == mainDoorColor;
    }


    public void Open()
    {
        mainDoor.SetActive(false);
        nextSceneTrigger.enabled = true;
    }

    public void ResetDoors()
    {
        if (mainDoor != null)
            mainDoor.SetActive(true);

        if (nextSceneTrigger != null)
            nextSceneTrigger.enabled = false;

        foreach (ColoredDoor coloredDoor in coloredDoors)
        {
            if (coloredDoor != null)
                coloredDoor.ResetDoor();
        }
    }
}
