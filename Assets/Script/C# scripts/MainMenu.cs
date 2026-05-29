using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void NewGame()
    {
        SceneManager.LoadScene("GamePlay"); 
    }

    public void OpenSkillTreeFromMenu()
    {
        PlayerProgress.Instance.openedSkillTreeFromLevel = false;
        SceneManagement.Instance.LoadScene("SkillTree");
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
