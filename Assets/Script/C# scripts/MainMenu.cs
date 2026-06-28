using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject newGameButton;
    [SerializeField] private GameObject skillTreeButton;

    private void Start()
    {
        PlayerProgress progress = PlayerProgress.EnsureInstance();
        progress.LoadSavedProgress();

        bool hasSave = SaveSystem.HasPlayableSave();

        if (continueButton != null)
            continueButton.SetActive(hasSave);

        if (newGameButton != null)
            newGameButton.SetActive(true);

        if (skillTreeButton != null)
            skillTreeButton.SetActive(progress.skillTreeUnlocked);
    }

    public void NewGame()
    {
        SaveSystem.DeleteSave();
        PlayerProgress.EnsureInstance().ResetProgress();
        SceneManagement.EnsureInstance().LoadScene("GamePlay1.0_normal");
    }

    public void ContinueGame()
    {
        PlayerProgress progress = PlayerProgress.EnsureInstance();

        if (!progress.LoadSavedProgress() || !SaveSystem.HasPlayableSave())
        {
            Debug.LogWarning("Continue was pressed, but no playable save was found.");
            return;
        }

        progress.openedSkillTreeFromLevel = false;
        SceneManagement.EnsureInstance().LoadScene(progress.ContinueLevelBuildIndex);
    }

    public void OpenSkillTreeFromMenu()
    {
        PlayerProgress progress = PlayerProgress.EnsureInstance();
        progress.LoadSavedProgress();

        if (!progress.skillTreeUnlocked)
        {
            Debug.LogWarning("Skill tree button was pressed, but the skill tree has not been unlocked yet.");
            return;
        }

        progress.openedSkillTreeFromLevel = false;

        SceneManagement.EnsureInstance().LoadScene("SkillTree");
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
