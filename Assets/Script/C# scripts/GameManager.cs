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

    [Header("Game State")]
    private bool isLost; 
    private bool isLevelCompleted;
    private bool isPaused;

    [SerializeField] LevelData LevelData;
    [SerializeField] private Color color1;
    [SerializeField] private Color color2;
    private bool[] isBlackOut;
    private static readonly Color32 GRAY = new Color32(128, 128, 128, 255);
    private Texture2D mapTexture;

  
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

    private void CreateMap()
    {
        Bounds bound = gridArea.bounds;

        int minX = Mathf.RoundToInt(bound.min.x);
        int minY = Mathf.RoundToInt(bound.min.y);

        int mapWidth = Mathf.RoundToInt(bound.size.x) + 1;
        int mapHeight = Mathf.RoundToInt(bound.size.y) + 1;

        GameObject gameMap = new GameObject("GameMap");
        SpriteRenderer sr = gameMap.AddComponent<SpriteRenderer>();

        mapTexture = new Texture2D(mapWidth, mapHeight);

        isBlackOut = new bool[mapWidth * mapHeight];

        for (int x = 0; x < mapTexture.width; x++)
        {
            for (int y = 0; y < mapTexture.height; y++)
            {

               
                if (x % 2 != 0 && y % 2 != 0 || x % 2 == 0 && y % 2 == 0)
                {
                    mapTexture.SetPixel(x, y, SetColor( 0, 226, 21, 220));
                }
                else
                {
                    mapTexture.SetPixel(x, y, SetColor( 0, 165, 6, 220));
                }
            }
        }

        mapTexture.filterMode = FilterMode.Point;
        mapTexture.Apply();

        Rect rect = new Rect(0, 0, mapTexture.width, mapTexture.height);

        Sprite sprite = Sprite.Create(mapTexture, rect, new Vector2(0f, 0f), 1f, 0, SpriteMeshType.FullRect);
        sr.sprite = sprite;

        gameMap.transform.position = new Vector3(minX - 0.5f, minY - 0.5f, 0f);

        sr.sortingOrder = -10; 
    }

    public void BlackOutRandomTiles(int count = 50)
    {
        if (mapTexture == null || isBlackOut == null) return; 

        int totalTiles = mapTexture.width * mapTexture.height;  

        List<int> availableTiles = new List<int>(totalTiles); 
        for(int i = 0; i < totalTiles; i++)
        {
            if (!isBlackOut[i])
                availableTiles.Add(i); 
        }   

        if(availableTiles.Count == 0)
            return;

        int pick = Mathf.Min(count, availableTiles.Count);
        for(int n = 0; n <pick; n++)
        {
            int random = UnityEngine.Random.Range(0, availableTiles.Count);    
            int index = availableTiles[random];
            availableTiles.RemoveAt(random);

            isBlackOut[index] = true;
                
            int x = index % mapTexture.width; // column
            int y = index / mapTexture.width; // row

            mapTexture.SetPixel(x, y, GRAY);
        }
        mapTexture.Apply();
    }

    private Color32 SetColor(byte r, byte g, byte b, byte alpha)
    {
        return new Color32(r, g, b, alpha); 
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
            // Load next level or show win screen
            Debug.Log("Level Completed!");
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
