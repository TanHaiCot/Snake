using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{

    [Header("MANAGERS")]
    [SerializeField] private UIManager uiManager;

    [Header("Game Components")]
    [SerializeField] Snake snake;
    [SerializeField] Snake secondSnake; 
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] Timer timer;
    [SerializeField] DoorController doorController;
    [SerializeField] Food food;
    [SerializeField] AI_Snake ai_Snake;
    [SerializeField] BoxCollider2D gridArea; 
    [SerializeField] MapManager mapManager;
    [SerializeField] Energy energy;
    [SerializeField] SnakeAbilities snakeAbilities;

    [Header("Game State")]
    private bool isLost; 
    private bool isLevelCompleted;
    private bool isPaused;

    [SerializeField] LevelData LevelData;

    void Start() 
    {
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
        
        mapManager.InitAndBuildMap();
        food.RandomizedSpawn(); 
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

    private void OnEnable()
    {
        if (timer)
            timer.OnTimeUp += HandleTimeUp; 
        
        if(snake != null)
            snake.OnFoodEaten.AddListener(HandleFoodEaten);
     
        if(scoreManager != null)
            scoreManager.OnScoreChanged.AddListener(HandleScoreChanged);
    }

    private void HandleScoreChanged(int current, int target)
    {
    }

    private void HandleFoodEaten()
    {         
    }

    private void OnDisable()
    {
        if (timer)
            timer.OnTimeUp -= HandleTimeUp; 
        
        if(snake != null) 
            snake.OnFoodEaten.RemoveListener(HandleFoodEaten);

        if(scoreManager != null)
            scoreManager.OnScoreChanged.RemoveListener(HandleScoreChanged);
    }

    private void HandleTimeUp()
    {
        if (!scoreManager.TargetReached)
        {
            GameOver(); 
        }
    }

    public void CheckWinStatus()
    {
        if (isLevelCompleted || isLost)
            return;

        if(scoreManager.TargetReached && !timer.TimeUp)
        {
            WinLevel();
        }
    }

    private void WinLevel()
    {
        isLevelCompleted = true;
        doorController.Open();
        PlayerProgress.Instance.AddUpgradePoint();
        PlayerProgress.Instance.completedLevels++;
        PlayerProgress.Instance.currentLevelBuildIndex = SceneManager.GetActiveScene().buildIndex;
    }

    public void MoveToSkillTree()
    {
        Time.timeScale = 1f; 
        PlayerProgress.Instance.openedSkillTreeFromLevel = true;
        SceneManagement.Instance.LoadScene("SkillTree");
    }

    //public void MoveToNextLevel()
    //{
    //    Time.timeScale = 1f;
    //    SceneManagement.Instance.NextLevel();
    //}

    public void Resume()
    {
        uiManager.HidePauseMenu();
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Pause()
    {
        uiManager.ShowPauseMenu();
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        mapManager.InitAndBuildMap(); 

        snake.Restate();
        food.RandomizedSpawn();

        if (secondSnake != null)
            secondSnake.Restate();

        if (ai_Snake != null)
            ai_Snake.Restate();

        isPaused = false;
        isLost = false; 

        uiManager.HideLostMenu();

        scoreManager.ResetScore();
        scoreManager.SetTargetScore(LevelData.targetScore);

        timer.SetLevelTimer(LevelData.timeLimit);

        energy.ResetEnergy();
        snakeAbilities.ResetAbilities();
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
            uiManager.ShowLostMenu();
        }
    }
}
