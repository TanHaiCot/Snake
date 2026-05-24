using System.Collections.Generic;
using UnityEngine;

public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress Instance;

    public int upgradePoints;
    public int currentLevel = 1;

    public List<string> chosenSkillIds = new List<string>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddUpgradePoint()
    {
        upgradePoints++;
    }
}