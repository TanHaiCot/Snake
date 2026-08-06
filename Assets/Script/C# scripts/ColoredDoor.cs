using UnityEngine;

public class ColoredDoor : MonoBehaviour
{
    [SerializeField] Food.FoodColors requiredFoodColor;
    [SerializeField] int requiredFoodAmount;

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
        this.gameObject.SetActive(false);
    }

    public void ResetDoor()
    {
        collectedAmount = 0;
        isDoorOpen = false;

        this.gameObject.SetActive(true);

    }

}
