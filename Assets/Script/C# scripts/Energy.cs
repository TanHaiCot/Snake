using System;
using UnityEngine;
using UnityEngine.UI;

public class Energy : MonoBehaviour
{
    [SerializeField] Image[] energyPoints; 

    float currentEnergy;
    float maxEnergy = 100f;

    private void Start()
    {
        currentEnergy = 50f;
    }

    private void Update()
    {
        if(currentEnergy > maxEnergy) currentEnergy = maxEnergy;

        UpdateEnergyUI();
    }

    private void UpdateEnergyUI()
    {
        for(int i = 0; i < energyPoints.Length; i++)
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
        Debug.Log($"Energy increased by {amount}, current energy: {currentEnergy}");
    }
}
