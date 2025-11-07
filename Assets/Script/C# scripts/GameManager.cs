using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject lostMenu;

    [Header("Game Components")]
    [SerializeField] Snake snake;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] Timer timer;
    [SerializeField] DoorController door; 

    [Header("Game State")]
    private bool isLost; 
    private bool isLevelCompleted;
    private bool isPaused;

    [SerializeField] LevelData LevelData;

    void Start() 
    {
        isLost = false;
        lostMenu.SetActive(false);

        if(pauseMenu != null)
        {
            isPaused = false;
            pauseMenu.SetActive(false);
        }

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
            if(LevelData != null)
                scoreManager.SetTargetScore(LevelData.targetScore);
        }

        if(timer != null && LevelData != null)
        {
            timer.SetLevelTimer(LevelData.timeLimit);
        }
    }

    private void OnEnable()
    {
        if (timer)
        {
            timer.OnTimeUp += HandleTimeUp; 
        }
    }

    private void OnDisable()
    {
        if (timer)
        {
            timer.OnTimeUp -= HandleTimeUp; 
        }
    }

    void HandleTimeUp()
    {
        if (!scoreManager.TargetReached)
        {
            GameOver(); 
        }
    }

    public void WinLevel()
    {
        if (isLevelCompleted)
            return;

        if(scoreManager.TargetReached && !timer.TimeUp)
        {
            isLevelCompleted = true;
            door.Open();
            // Load next level or show win screen
            Debug.Log("Level Completed!");
        }
    }

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (isPaused == false)
            {
                Pause();
            }
            else
            {
                Resume();
            }
        }
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Pause()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        snake.Restate();

        isPaused = false;
        isLost = false; 

        lostMenu.SetActive(false);

        scoreManager.ResetScore();
        scoreManager.SetTargetScore(LevelData.targetScore);

        timer.SetLevelTimer(LevelData.timeLimit);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("StartMenu");
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        isLost = true;
        if (isLost == true)
        {
            lostMenu.SetActive(true);
        }
    }
}
