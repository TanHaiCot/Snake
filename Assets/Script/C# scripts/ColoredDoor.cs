using UnityEngine;

public class ColoredDoor : MonoBehaviour
{
    [SerializeField] Food.FoodColors requiredFoodColor;
    [SerializeField] int requiredFoodAmount;
    [SerializeField] GameObject doorVisual;
    //[SerializeField] Collider2D doorCollider;

    private int collectedAmount; 
    private bool isDoorOpen;

    public Food.FoodColors RequiredColor => requiredFoodColor;

    public void RecordCollectedFood(Food.FoodColors foodColor)
    {
        if (foodColor == requiredFoodColor && !isDoorOpen)
        {
            collectedAmount++;
            if (collectedAmount >= requiredFoodAmount)
            {
                OpenDoor();
            }
        }
    }

    private void OpenDoor()
    {
        isDoorOpen = true;
        doorVisual.SetActive(false);
        //doorCollider.enabled = false;
    }

    public void ResetDoor()
    {
        collectedAmount = 0;
        isDoorOpen = false;

        if (doorVisual != null)
            doorVisual.SetActive(true);

        //if (doorCollider != null)
        //    doorCollider.enabled = true;
    }

}
