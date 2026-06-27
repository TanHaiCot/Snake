using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour
{
    public static SceneManagement Instance { get; private set; } 

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static SceneManagement EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        SceneManagement existingManager = FindFirstObjectByType<SceneManagement>();
        if (existingManager != null)
        {
            existingManager.SetAsInstance();
            return existingManager;
        }

        GameObject managerObject = new GameObject(nameof(SceneManagement));
        return managerObject.AddComponent<SceneManagement>();
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SetAsInstance();
    }

    private void SetAsInstance()
    {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public void NextLevel()
    {
        SceneManager.LoadSceneAsync(PlayerProgress.Instance.currentLevelBuildIndex + 1);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName);
    }

}
