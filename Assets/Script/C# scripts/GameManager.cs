using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    [Header("Game State")]
    private bool isLost; 
    private bool isLevelCompleted;
    private bool isPaused;

    [SerializeField] LevelData LevelData;
   
    private int wallGreyOutWave = 0; 

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
        //ApplyMapStatus();
       
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


    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
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
        if(mapManager.CurrentWallMode == MapManager.WallMode.GreyOutOnScore)
            return;

        if (current == target - 1 && wallGreyOutWave < 1)
        {
            mapManager.GreyOutRandomWalls(3);
            wallGreyOutWave = 1; 
        }

        else if(current == target && wallGreyOutWave < 2)
        {
            mapManager.GreyOutRandomWalls(3);
            wallGreyOutWave = 2;
        }
    }

    private void HandleFoodEaten()
    {
        mapManager.GreyOutRandomTiles(500); 
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

            //SaveMapStatus(); 

            // Load next level or show win screen
            Debug.Log("Level Completed!");
        }
    }

    //private void SaveMapStatus()
    //{
    //    if(MapStatus.Instance == null)
    //        return;

    //    MapStatus.Instance.width = mapTexture.width;
    //    MapStatus.Instance.height = mapTexture.height;

    //    if(isGreyedTile == null)
    //        isGreyedTile = new bool[mapTexture.width * mapTexture.height];

    //    MapStatus.Instance.greyedTiles = (bool[])isGreyedTile.Clone();   
    //}

    //private void ApplyMapStatus()
    //{
    //    if(MapStatus.Instance == null || MapStatus.Instance.greyedTiles == null)
    //        return;

    //    if(MapStatus.Instance.width != mapTexture.width || MapStatus.Instance.height != mapTexture.height)
    //        return;
            
    //    isGreyedTile = (bool[])MapStatus.Instance.greyedTiles.Clone();
       
    //    for(int i = 0; i < isGreyedTile.Length; i++)
    //    {
    //        if(isGreyedTile[i])
    //        {
    //            int x = i % mapTexture.width;
    //            int y = i / mapTexture.width;

    //            Color32 grey = isLightColorTile[i] ? LIGHT_GRAY : DARK_GRAY;
    //            mapTexture.SetPixel(x, y, grey);
    //        }
    //    }
    //    mapTexture.Apply();
    //}

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
