using System;
using UnityEngine;
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

    [Header("Game State")]
    private bool isLost; 
    private bool isLevelCompleted;
    private bool isPaused;

    [SerializeField] LevelData LevelData;
    [SerializeField] private Color color1;
    [SerializeField] private Color color2;


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

        CreateMap(); 
    }

    private void CreateMap()
    {
        Bounds bound = gridArea.bounds;

        int minX = Mathf.RoundToInt(bound.min.x);
        int minY = Mathf.RoundToInt(bound.min.y);

        int width = Mathf.RoundToInt(bound.size.x);
        int height = Mathf.RoundToInt(bound.size.y);


        GameObject gameMap = new GameObject("GameMap");
        SpriteRenderer sr = gameMap.AddComponent<SpriteRenderer>();

        Texture2D texture = new Texture2D(width + 1, height + 1);
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                if (x % 2 != 0 && y % 2 != 0 || x % 2 == 0 && y % 2 == 0)
                {
                    texture.SetPixel(x, y, color1);
                }
                else
                {
                    texture.SetPixel(x, y, color2);
                }
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.Apply();

        Rect rect = new Rect(0, 0, texture.width, texture.height);

        Sprite sprite = Sprite.Create(texture, rect, new Vector2(0f, 0f), 1f, 0, SpriteMeshType.FullRect);
        sr.sprite = sprite;

        gameMap.transform.position = new Vector3(minX - 0.5f, minY - 0.5f, 0f);

        sr.sortingOrder = -10; 
    }


    //private void CreateMap()
    //{
    //    Bounds bound = gridArea.bounds;

    //    int width = Mathf.RoundToInt(bound.size.x);
    //    int height = Mathf.RoundToInt(bound.size.y);

    //    Texture2D texture = new Texture2D(width, height);
    //    texture.filterMode = FilterMode.Point;
    //    texture.wrapMode = TextureWrapMode.Clamp;

    //    for (int x = 0; x < width; x++)
    //    {
    //        for (int y = 0; y < height; y++)
    //        {
    //            bool sameParity = (x + y) % 2 == 0;
    //            texture.SetPixel(x, y, sameParity ? color1 : color2);
    //        }
    //    }

    //    texture.Apply();

    //    GameObject gameMap = new GameObject("GameMap");
    //    SpriteRenderer sr = gameMap.AddComponent<SpriteRenderer>();

    //    sr.sprite = Sprite.Create(
    //        texture,
    //        new Rect(0, 0, width, height),
    //        new Vector2(0.5f, 0.5f),
    //        16f
    //    );

    //    gameMap.transform.position = bound.center;
    //}


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
