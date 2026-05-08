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

    //[Header("Grid / Map")]
    //[SerializeField] private BoxCollider2D gridArea;
    //[SerializeField] private int sortingOrder = -10;

    [Header("Walls")]
    [SerializeField] private Transform wallContainer;
    [SerializeField] private WallMode wallMode = WallMode.Original;

    private readonly List<Vector2Int> playableCells = new();

    public void InitAndBuildMap()
    {
        CreateMap();
        CachePlayableCells();
    }

    // ---------- Tiles Code ----------
    private void CreateMap()
    {
        BoundsInt bounds = floorTilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (!floorTilemap.HasTile(pos))
                continue;

            bool isLight = (pos.x + pos.y) % 2 == 0;

            floorTilemap.SetTile(
                pos,
                isLight ? lightFloorTile : darkFloorTile
            );
        
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
}