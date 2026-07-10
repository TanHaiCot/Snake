using System.Collections.Generic;
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
    [SerializeField] private Tilemap playgroundTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase lightFloorTile;
    [SerializeField] private TileBase darkFloorTile;

    private readonly List<Vector2Int> playableCells = new();

    public void InitAndBuildMap()
    {
        CreateMap();
        CachePlayableCells();
    }

    // ---------- Tiles Code ----------
    private void CreateMap()
    {
        BoundsInt bounds = playgroundTilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (!playgroundTilemap.HasTile(pos))
                continue;

            bool isDark = (pos.x + pos.y) % 2 == 0;

            playgroundTilemap.SetTile(
                pos,
                isDark ? darkFloorTile : lightFloorTile
            );
        
        }
    }

    private void CachePlayableCells()
    {
        playableCells.Clear();

        BoundsInt bounds = playgroundTilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (!playgroundTilemap.HasTile(pos))
                continue;

            bool hasWall =
                wallTilemap != null &&
                wallTilemap.HasTile(pos);

            if (hasWall)
                continue;

            playableCells.Add(new Vector2Int(pos.x, pos.y));
        }
    }

    public bool IsWalkable(Vector2Int cell)
    {
        Vector3Int tilePos = new Vector3Int(cell.x, cell.y, 0);

        bool hasFloor = playgroundTilemap.HasTile(tilePos);
        bool hasWall = wallTilemap != null && wallTilemap.HasTile(tilePos);

        return hasFloor && !hasWall;
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        Vector3Int tilePos = new Vector3Int(cell.x, cell.y, 0);
        return playgroundTilemap.GetCellCenterWorld(tilePos);
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3Int cell = playgroundTilemap.WorldToCell(worldPos);
        return new Vector2Int(cell.x, cell.y);
    }

    public List<Vector2Int> GetWalkableCells()
    {
        List<Vector2Int> result = new();

        BoundsInt bounds = playgroundTilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            Vector2Int cell = new Vector2Int(pos.x, pos.y);

            if (IsWalkable(cell))
                result.Add(cell);
        }

        return result;
    }
}