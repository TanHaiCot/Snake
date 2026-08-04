using UnityEngine;

public class ColoredDoorManager : MonoBehaviour
{
    [SerializeField] private ColoredDoor[] coloredDoors;

    [Header("Main Door")]
    [SerializeField] private Food.FoodColors mainDoorColor;

    public bool CollectFood(Food.FoodColors collectedColor)
    {
        foreach (ColoredDoor door in coloredDoors)
        {
            if (door != null)
                door.RecordFood(collectedColor);
        }

        // Only this color contributes toward the main exit.
        return collectedColor == mainDoorColor;
    }

    public void ResetDoors()
    {
        foreach (ColoredDoor door in coloredDoors)
        {
            if (door != null)
                door.ResetDoor();
        }
    }
}
