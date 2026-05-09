using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{

    [Header("UI Components")]
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject lostMenu;

    [Header("Game Components")]
    [SerializeField] Snake snake;
    [SerializeField] Snake secondSnake; 
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] Timer timer;
    [SerializeField] DoorController door;
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
        
        mapManager.InitAndBuildMap();
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

    public void WinLevel()
    {
        if (isLevelCompleted)
            return;

        if(scoreManager.TargetReached && !timer.TimeUp)
        {
            isLevelCompleted = true;
            door.Open();
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

        mapManager.InitAndBuildMap(); 

        snake.Restate();
        food.RandomizedSpawn();

        if (secondSnake != null)
            secondSnake.Restate();

        if (ai_Snake != null)
            ai_Snake.Restate();

        isPaused = false;
        isLost = false; 

        lostMenu.SetActive(false);

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
            lostMenu.SetActive(true);
        }
    }
}
