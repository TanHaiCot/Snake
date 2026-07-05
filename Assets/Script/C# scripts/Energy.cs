using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Energy : MonoBehaviour
{
    [SerializeField] Image[] energyPoints;

    float currentEnergy;
    float startEnergy = 50f;
    float maxEnergy = 100f;
    private bool baseSettingsCached;
    private float baseStartEnergy;
    private float baseMaxEnergy;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;

    private void Awake()
    {
        CacheBaseSettings();
        InitializeEnergy();
    }

    private void Start()
    {
        UpdateEnergyUI();
    }

    private void InitializeEnergy()
    {
        if (SceneManager.GetActiveScene().name == "Boss1Fight")
            startEnergy = 0; 

        currentEnergy = startEnergy;
    }

    private void Update()
    {
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

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
        if (energyPoints == null)
            return;

        for (int i = 0; i < energyPoints.Length; i++)
        {
            if (energyPoints[i] != null)
                energyPoints[i].enabled = !DisplayEnergyPoint(currentEnergy, i);
        }
    }

    private bool DisplayEnergyPoint(float energy, int index)
    {
        return ((index * 10) >= energy);
    }

    public void AddEnergy(float amount)
    {
        if (amount <= 0f)
            return;

        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
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
        InitializeEnergy();
    }

    public void ResetSkillAdjustedStats()
    {
        CacheBaseSettings();
        startEnergy = baseStartEnergy;
        maxEnergy = baseMaxEnergy;
    }

    private void CacheBaseSettings()
    {
        if (baseSettingsCached)
            return;

        baseStartEnergy = startEnergy;
        baseMaxEnergy = maxEnergy;
        baseSettingsCached = true;
    }




    //change statistic methods for skill tree 
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
