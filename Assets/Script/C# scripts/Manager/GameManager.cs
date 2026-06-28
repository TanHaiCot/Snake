using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{

    [Header("MANAGERS")]
    [SerializeField] UIManager uiManager;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] MapManager mapManager;
    [SerializeField] DoorController doorController;
        
    [Header("Game Components")]
    [SerializeField] Snake snake;
    [SerializeField] Snake secondSnake; 
    [SerializeField] Timer timer;
    [SerializeField] Food food;
    [SerializeField] AI_Snake ai_Snake;
    [SerializeField] Energy energy;
    [SerializeField] SnakeAbilities snakeAbilities;

    [Header("Game State")]
    private bool isLost; 
    private bool isLevelCompleted;
    private bool isPaused;

    [SerializeField] LevelData LevelData;

    private int scoreToStartReverseMovement = 3;
    private bool reverseMovementStarted;

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
        if (!LevelData.enableReverseMovement)
            return;

        if (!reverseMovementStarted && current >= scoreToStartReverseMovement)
        {
            reverseMovementStarted = true;
            snake.SetReverseMovement(true);
        }
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
        PlayerProgress.EnsureInstance().CompleteLevel(SceneManager.GetActiveScene().buildIndex);
    }

    public void MoveToSkillTree()
    {
        if(LevelData.isSkillTreeUnlocked == false)
        {
            Time.timeScale = 1f;
            SceneManagement.Instance.NextLevel();
            return; 
        }

        Time.timeScale = 1f; 
        PlayerProgress.EnsureInstance().openedSkillTreeFromLevel = true;
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

        reverseMovementStarted = false;
        snake.SetReverseMovement(false);
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
