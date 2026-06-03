using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private GameObject countdownTimerUI;
    [SerializeField] private GameObject energyBarUI;
    [SerializeField] private GameObject dashUI;
    [SerializeField] private GameObject ghostUI;

    [Header("Menus")]
    [SerializeField] private GameObject lostMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject score; 

    [Header("Unlock Rules")]
    [SerializeField] private int timerUnlockAfterCompletedLevels = 2;

    private void Start()
    {
        HidePauseMenu();
        HideLostMenu(); 
        RefreshHUD();
    }

    public void RefreshHUD()
    {
        PlayerProgress progress = PlayerProgress.Instance;

        bool timerUnlocked = progress != null && progress.completedLevels >= timerUnlockAfterCompletedLevels;
        bool dashUnlocked = progress != null && progress.HasSkill("1");
        bool ghostUnlocked = progress != null && progress.HasSkill("ghost_unlock");

        if (countdownTimerUI != null)
            countdownTimerUI.SetActive(timerUnlocked);

        if (energyBarUI != null)
            energyBarUI.SetActive(dashUnlocked || ghostUnlocked);

        if (dashUI != null)
        {
            dashUI.SetActive(dashUnlocked);
            score.SetActive(!dashUnlocked);
        }

        if (ghostUI != null)
            ghostUI.SetActive(ghostUnlocked);
    }

    public void ShowLostMenu()
    {
        if (lostMenu != null)
            lostMenu.SetActive(true);
    }

    public void ShowPauseMenu()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(true);
    }

    public void ShowScore()
    {
        if (score != null)
            score.SetActive(true);
    }

    public void HideLostMenu()
    {
        if (lostMenu != null)
            lostMenu.SetActive(false);     
    }

    public void HidePauseMenu()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
    }

    public void HideScore()
    {
        if (score != null)
            score.SetActive(false);
    }
}
