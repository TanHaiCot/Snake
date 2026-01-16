using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public enum WallMode
    {
        Original,
        GreyOutOnScore,
        AllGreyed
    }

    [Header("Grid / Map")]
    [SerializeField] private BoxCollider2D gridArea;
    [SerializeField] private int sortingOrder = -10;

    [Header("Walls")]
    [SerializeField] private Transform wallContainer;
    [SerializeField] private WallMode wallMode = WallMode.Original;

    private Texture2D mapTexture;
    private SpriteRenderer mapRenderer;

    private bool[] isGreyedTile;
    private bool[] isLightColorTile;

    private readonly List<SpriteRenderer> wallRenderers = new();
    private readonly HashSet<SpriteRenderer> greyedWalls = new();

    private static readonly Color32 LIGHT_GREEN = new(0, 226, 21, 255);
    private static readonly Color32 DARK_GREEN = new(0, 165, 6, 255);
    private static readonly Color32 LIGHT_GRAY = new(170, 170, 170, 220);
    private static readonly Color32 DARK_GRAY = new(140, 140, 140, 220);

    private static readonly Color32 defaultWallColor = new(120, 75, 30, 255);
    private static readonly Color32 greyWallColor = new(75, 75, 75, 255);

    private Vector3 mapOrigin;

    public WallMode CurrentWallMode => wallMode;

    public void InitAndBuildMap()
    {
        CreateMap();
        InitWalls();
        ApplyWallMode();    
    }

    // ---------- Tiles Code ----------
    private void CreateMap()
    {
        if (gridArea == null)
        {
            Debug.LogError("Grid Area is not assigned.");
            return;
        }

        Bounds bound = gridArea.bounds;

        int minX = Mathf.RoundToInt(bound.min.x);
        int minY = Mathf.RoundToInt(bound.min.y);

        int mapWidth = Mathf.RoundToInt(bound.size.x) + 1;
        int mapHeight = Mathf.RoundToInt(bound.size.y) + 1;

        mapOrigin = new Vector3(minX - 0.5f, minY - 0.5f, 0f);

        if (mapRenderer == null)
        {
            GameObject gameMap = new GameObject("GameMap");
            gameMap.transform.SetParent(transform);
            mapRenderer = gameMap.AddComponent<SpriteRenderer>();
            mapRenderer.sortingOrder = sortingOrder;
        }

        mapTexture = new Texture2D(mapWidth, mapHeight);
        isLightColorTile = new bool[mapWidth * mapHeight];
        isGreyedTile = new bool[mapWidth * mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int index = y * mapWidth + x;
                bool light = (x % 2 == y % 2);

                isLightColorTile[index] = light;
                mapTexture.SetPixel(x, y, light ? LIGHT_GREEN : DARK_GREEN);
            }

            mapTexture.filterMode = FilterMode.Point;
            mapTexture.Apply();

            Rect rect = new Rect(0, 0, mapWidth, mapHeight);
            Sprite sprite = Sprite.Create(mapTexture, rect, new Vector2(0f, 0f), 1f, 0, SpriteMeshType.FullRect);
            mapRenderer.sprite = sprite;

            mapRenderer.transform.position = mapOrigin;
        }
    }

    public void GreyOutRandomTiles(int count)
    {
        if (mapTexture == null) return;

        if (isGreyedTile == null)
            isGreyedTile = new bool[mapTexture.width * mapTexture.height];

        int totalTiles = mapTexture.width * mapTexture.height;
        List<int> availableTiles = new(totalTiles);

        for (int i = 0; i < totalTiles; i++)
        {
            if (!isGreyedTile[i])
                availableTiles.Add(i);
        }

        if(availableTiles.Count == 0) return;

        int pick = Mathf.Min(count, availableTiles.Count);
        for (int n = 0; n < pick; n++)
        {
            int random = Random.Range(0, availableTiles.Count);
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

    // ---------- Walls Code ----------
    private void InitWalls()
    {
        wallRenderers.Clear();
        greyedWalls.Clear();

        if (wallContainer == null) return;

        foreach (Transform wall in wallContainer)
        {
            SpriteRenderer sr = wall.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                wallRenderers.Add(sr);
                sr.color = defaultWallColor;
            }
        }
    }

    public void GreyOutRandomWalls(int count)
    {
        if (wallRenderers == null) return;

        List<SpriteRenderer> available = new();
        foreach (var sr in wallRenderers)
            if (!greyedWalls.Contains(sr))
                available.Add(sr);

        if (available.Count == 0) return;

        int pick = Mathf.Min(count, available.Count);

        for (int n = 0; n < pick; n++)
        {
            int random = Random.Range(0, available.Count);
            var chosen = available[random];
            available.RemoveAt(random);

            greyedWalls.Add(chosen);
            chosen.color = greyWallColor;
        }
    }

    private void ApplyWallMode()
    {
        if (wallMode != WallMode.AllGreyed) return;

        foreach (var sr in wallRenderers)
        {
            sr.color = greyWallColor;
            greyedWalls.Add(sr);
        }
    }
}