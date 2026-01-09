using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public enum WallMode
    {
        Original,
        GreyOutOnScore,
        AllGreyed
    }

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

    [Header("Map")]
    private bool[] isGreyedTile;
    private bool[] isLightColorTile;
    private Texture2D mapTexture;

    private static readonly Color32 LIGHT_GREEN = new Color32(0, 226, 21, 220);
    private static readonly Color32 DARK_GREEN = new Color32(0, 165, 6, 220);

    private static readonly Color32 LIGHT_GRAY = new Color32(170, 170, 170, 220);
    private static readonly Color32 DARK_GRAY = new Color32(140, 140, 140, 220);


    [Header("Walls")]
    [SerializeField] private Transform wallContainer;
    [SerializeField] private WallMode wallMode = WallMode.Original;

    private static readonly Color32 defaultWallColor = new Color32(120, 75, 30, 255);
    private static readonly Color32 greyWallColor = new Color32(75, 75, 75, 255);

    private List<SpriteRenderer> wallRenderers = new List<SpriteRenderer>();
    private HashSet<SpriteRenderer> greyedWalls = new HashSet<SpriteRenderer>();
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

        CreateMap();
        InitWalls();
        ApplyMapStatus();
        ApplyWallMode(); 
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

        isLightColorTile = new bool[mapWidth * mapHeight];

        for (int x = 0; x < mapTexture.width; x++)
        {
            for (int y = 0; y < mapTexture.height; y++)
            {
                int index = y * mapTexture.width + x;

                if (x % 2 != 0 && y % 2 != 0 || x % 2 == 0 && y % 2 == 0)
                {
                    mapTexture.SetPixel(x, y, LIGHT_GREEN); 
                    isLightColorTile[index] = true;
                }
                else
                {
                    mapTexture.SetPixel(x, y,DARK_GREEN);
                    isLightColorTile[index] = false;
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
    private void InitWalls()
    {
        wallRenderers.Clear();
        greyedWalls.Clear();
        wallGreyOutWave = 0;

        if(wallContainer == null)
            return; 

        foreach(Transform transform in wallContainer)
        {
            SpriteRenderer sr = transform.GetComponent<SpriteRenderer>();
            if(sr != null)
            {
                sr.color = defaultWallColor;
                wallRenderers.Add(sr);
            }
        }
    }

    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    }

    private void GreyOutRandomWalls(int count)
    {
        if (wallRenderers == null) return; 

        List<SpriteRenderer> available = new List<SpriteRenderer> ();
        foreach(var sr in wallRenderers)
            if(!greyedWalls.Contains(sr))
                available.Add(sr);  
        
        if(available.Count == 0) return;

        int pick = Mathf.Min(count, available.Count); 

        for(int n = 0; n < pick; n++)
        {
            int random = UnityEngine.Random.Range(0, available.Count);
            var chosen = available[random]; 
            available.RemoveAt(random);

            greyedWalls.Add(chosen); 
            chosen.color = greyWallColor;
        }
    }

    private void ApplyWallMode()
    {
        if(wallRenderers == null) return;

        switch (wallMode)
        {
            case WallMode.Original:
                break;

            case WallMode.GreyOutOnScore:
                break;

            case WallMode.AllGreyed:
                foreach(var sr in wallRenderers)
                {
                    sr.color = greyWallColor;
                    greyedWalls.Add(sr);
                }
                break;
        }
    }

    public void GreyOutRandomTiles(int count = 50)
    {
        if (mapTexture == null) return;

        if (isGreyedTile == null)
            isGreyedTile = new bool[mapTexture.width * mapTexture.height];

        int totalTiles = mapTexture.width * mapTexture.height;

        List<int> availableTiles = new List<int>(totalTiles);
        for (int i = 0; i < totalTiles; i++)
        {
            if (!isGreyedTile[i])
                availableTiles.Add(i);
        }

        if (availableTiles.Count == 0)
            return;

        int pick = Mathf.Min(count, availableTiles.Count);
        for (int n = 0; n < pick; n++)
        {
            int random = UnityEngine.Random.Range(0, availableTiles.Count);
            int index = availableTiles[random];
            availableTiles.RemoveAt(random);

            isGreyedTile[index] = true;

            int x = index % mapTexture.width; // column
            int y = index / mapTexture.width; // row

            Color32 grey = isLightColorTile[index] ? LIGHT_GRAY : DARK_GRAY;
            mapTexture.SetPixel(x, y, grey);
        }
        mapTexture.Apply();
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
        if(wallMode != WallMode.GreyOutOnScore)
            return;

        if (current == target - 1 && wallGreyOutWave < 1)
        {
            GreyOutRandomWalls(3);
            wallGreyOutWave = 1; 
        }

        else if(current == target && wallGreyOutWave < 2)
        {
            GreyOutRandomWalls(3);
            wallGreyOutWave = 2;
        }
    }

    private void HandleFoodEaten()
    {
        GreyOutRandomTiles(500); 
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

            SaveMapStatus(); 

            // Load next level or show win screen
            Debug.Log("Level Completed!");
        }
    }

    private void SaveMapStatus()
    {
        if(MapStatus.Instance == null)
            return;

        MapStatus.Instance.width = mapTexture.width;
        MapStatus.Instance.height = mapTexture.height;

        if(isGreyedTile == null)
            isGreyedTile = new bool[mapTexture.width * mapTexture.height];

        MapStatus.Instance.greyedTiles = (bool[])isGreyedTile.Clone();   
    }

    private void ApplyMapStatus()
    {
        if(MapStatus.Instance == null || MapStatus.Instance.greyedTiles == null)
            return;

        if(MapStatus.Instance.width != mapTexture.width || MapStatus.Instance.height != mapTexture.height)
            return;
            
        isGreyedTile = (bool[])MapStatus.Instance.greyedTiles.Clone();
       
        for(int i = 0; i < isGreyedTile.Length; i++)
        {
            if(isGreyedTile[i])
            {
                int x = i % mapTexture.width;
                int y = i / mapTexture.width;

                Color32 grey = isLightColorTile[i] ? LIGHT_GRAY : DARK_GRAY;
                mapTexture.SetPixel(x, y, grey);
            }
        }
        mapTexture.Apply();
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
