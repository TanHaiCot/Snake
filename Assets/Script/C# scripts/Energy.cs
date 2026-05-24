using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class Energy : MonoBehaviour
{
    [SerializeField] Image[] energyPoints;

    float currentEnergy;
    float startEnergy = 50f;
    float maxEnergy = 100f;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "Boss1Fight")
            startEnergy = 0; 

        currentEnergy = startEnergy;

    }

    private void Update()
    {
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;

        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Current Energy: " + currentEnergy);
            if (currentEnergy <= 0)
                Debug.Log("Energy no more!");

        }
        UpdateEnergyUI();
    }

    private void UpdateEnergyUI()
    {
        for (int i = 0; i < energyPoints.Length; i++)
        {
            energyPoints[i].enabled = !DisplayEnergyPoint(currentEnergy, i);
        }
    }

    private bool DisplayEnergyPoint(float energy, int index)
    {
        return ((index * 10) >= energy);
    }

    public void AddEnergy(float amount)
    {
        currentEnergy += amount;
        //Debug.Log($"Energy increased by {amount}, current energy: {currentEnergy}");
    }

    public bool TryConsumeEnergy(float amount)
    {
        if (amount <= 0) return true;

        if (currentEnergy < amount)
        {
            currentEnergy = 0;
            return false;
        }

        currentEnergy -= amount;
        return true;
    }

    public void ResetEnergy()
    {
        currentEnergy = startEnergy;
    }




    //test 
    public void IncreaseMaxEnergy(float amount)
    {
        maxEnergy += amount;
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
    }

    public void IncreaseStartEnergy(float amount)
    {
        startEnergy += amount;
        currentEnergy += amount;

        if (currentEnergy > maxEnergy)
            currentEnergy = maxEnergy;
    }
}
