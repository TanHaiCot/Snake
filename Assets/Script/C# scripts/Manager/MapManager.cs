using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    public enum WallMode
    {
        Original,
        GreyOutOnScore,
        AllGreyed
    }

    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    //[SerializeField] private Tilemap greyTilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase lightFloorTile;
    [SerializeField] private TileBase darkFloorTile;
    //[SerializeField] private TileBase greyLightTile;
    //[SerializeField] private TileBase greyDarkTile;

    [SerializeField] private int mapWidth = 45;
    [SerializeField] private int mapHeight = 23; 

    //[Header("Grid / Map")]
    //[SerializeField] private BoxCollider2D gridArea;
    //[SerializeField] private int sortingOrder = -10;

    [Header("Walls")]
    [SerializeField] private Transform wallContainer;
    [SerializeField] private WallMode wallMode = WallMode.Original;

    private readonly List<Vector2Int> playableCells = new();
    private readonly HashSet<Vector2Int> greyedCells = new();

    private Texture2D mapTexture;
    private SpriteRenderer mapRenderer;

    private bool[] isGreyedTile;
    private bool[] isLightColorTile;

    private readonly List<SpriteRenderer> wallRenderers = new();
    private readonly HashSet<SpriteRenderer> greyedWalls = new();

    private static readonly Color32 defaultWallColor = new(120, 75, 30, 255);
    private static readonly Color32 greyWallColor = new(75, 75, 75, 255);

    public WallMode CurrentWallMode => wallMode;

    public void InitAndBuildMap()
    {
        CreateMap();
        InitWalls();
        ApplyWallStatus();
        //ApplyMapStatus();
    }

    // ---------- Tiles Code ----------
    private void CreateMap()
    {
        floorTilemap.ClearAllTiles();

        int halfWidth = mapWidth / 2;
        int halfHeight = mapHeight / 2;

        for (int x = -halfWidth; x <= halfWidth; x++)
        {
            for (int y = -halfHeight; y <= halfHeight; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                bool isLight = (x + y) % 2 == 0;

                floorTilemap.SetTile(
                    pos,
                    isLight ? lightFloorTile : darkFloorTile
                );
            }
        }
    }

    private void CachePlayableCells()
    {
        playableCells.Clear();

        BoundsInt bounds = floorTilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (!floorTilemap.HasTile(pos))
                continue;

            bool hasWall =
                wallTilemap != null &&
                wallTilemap.HasTile(pos);

            if (hasWall)
                continue;

            playableCells.Add(new Vector2Int(pos.x, pos.y));
        }
    }

    //public void GreyOutRandomTiles(int count)
    //{
    //    if (mapTexture == null) return;

    //    if (isGreyedTile == null)
    //        isGreyedTile = new bool[mapTexture.width * mapTexture.height];

    //    int totalTiles = mapTexture.width * mapTexture.height;
    //    List<int> availableTiles = new(totalTiles);

    //    for (int i = 0; i < totalTiles; i++)
    //    {
    //        if (!isGreyedTile[i])
    //            availableTiles.Add(i);
    //    }

    //    if(availableTiles.Count == 0) return;

    //    int pick = Mathf.Min(count, availableTiles.Count);
    //    for (int n = 0; n < pick; n++)
    //    {
    //        int random = Random.Range(0, availableTiles.Count);
    //        int index = availableTiles[random];
    //        availableTiles.RemoveAt(random);

    //        isGreyedTile[index] = true;

    //        int x = index % mapTexture.width; // column
    //        int y = index / mapTexture.width; // row

    //        Color32 grey = isLightColorTile[index] ? LIGHT_GRAY : DARK_GRAY;
    //        mapTexture.SetPixel(x, y, grey);
    //    }
    //    mapTexture.Apply();
    //}

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

    private void ApplyWallStatus()
    {
        if (wallMode != WallMode.AllGreyed) return;

        foreach (var sr in wallRenderers)
        {
            sr.color = greyWallColor;
            greyedWalls.Add(sr);
        }
    }

    public void SaveMapStatus()
    {
        if (MapStatus.Instance == null || mapTexture == null)
            return;

        MapStatus.Instance.SaveTiles(mapTexture.width, mapTexture.height, isGreyedTile);

        MapStatus.Instance.ClearWalls();
        //foreach (var sr in greyedWalls)  //may not be used but so far so good for now 
        //{
        //    Vector2Int cell = WorldToCell(sr.transform.position);
        //    MapStatus.Instance.GreyedWallPositions.Add(cell);
        //}

    }

    //private void ApplyMapStatus()
    //{
    //    if(MapStatus.Instance == null || mapTexture == null)
    //        return;

    //    if (MapStatus.Instance.IsTheMapValidToSave(mapTexture.width, mapTexture.height))
    //    {
    //        isGreyedTile = (bool[])MapStatus.Instance.greyedTiles.Clone();

    //        for (int i = 0; i < isGreyedTile.Length; i++)
    //        {
    //            if (isGreyedTile[i])
    //            {
    //                int x = i % mapTexture.width;
    //                int y = i / mapTexture.width;
    //                Color32 grey = isLightColorTile[i] ? LIGHT_GRAY : DARK_GRAY;
    //                mapTexture.SetPixel(x, y, grey);

    //            }            
    //        }

    //        mapTexture.Apply();
    //    }
    //}

    //private Vector2Int WorldToCell(Vector3 worldPos)
    //{
    //    // Your tiles are drawn with origin at (minX-0.5, minY-0.5)
    //    // Convert world to tile index coordinates.
    //    float localX = worldPos.x - mapOrigin.x;
    //    float localY = worldPos.y - mapOrigin.y;
    //    return new Vector2Int(Mathf.RoundToInt(localX), Mathf.RoundToInt(localY));
    //}
}