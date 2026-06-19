using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DarknessManager : MonoBehaviour
{
    [SerializeField] private Tilemap playgroundTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private GameObject darknessTilePrefab;
    [SerializeField] private Transform snake;
    [SerializeField] private MapManager mapManager;

    private int visableRange = 5;

    private Dictionary<Vector2Int, SpriteRenderer> darkTiles = new();

    private Vector2Int[] directions = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Start()
    {
        GridInit();
        UpdateVisibility(); 
    }

    public void UpdateVisibility()
    {
        foreach (var tile in darkTiles)
        {
            SetAlpha(tile.Value, 1f);
        }

        Vector2Int startPosition = mapManager.WorldToCell(snake.position); 

        Queue<Vector2Int> posQueue = new();
        HashSet<Vector2Int> visited = new();

        posQueue.Enqueue(startPosition);
        visited.Add(startPosition);

        while (posQueue.Count > 0)
        {
            Vector2Int currentPos = posQueue.Dequeue();

            if(!darkTiles.TryGetValue(currentPos, out SpriteRenderer sr))
                continue;

            // Limit vision by range 
            int distance = Mathf.Abs(currentPos.x - startPosition.x) + Mathf.Abs(currentPos.y - startPosition.y);
            
            if (distance > visableRange)
                continue;
            
            SetAlpha(sr, 0f);


            if (IsWall(currentPos))
                continue;

            foreach (var dir in directions)
            {
                Vector2Int neighbor = currentPos + dir;

                if(visited.Contains(neighbor))
                    continue;

                if (!darkTiles.ContainsKey(neighbor))
                    continue;

                visited.Add(neighbor);
                posQueue.Enqueue(neighbor);
            }

        }
    }

    private bool IsWall(Vector2Int cell)
    {
        if (wallTilemap == null)
            return false;

        Vector3Int tilePos = new Vector3Int(cell.x, cell.y, 0);
        return wallTilemap.HasTile(tilePos);
    }


    private void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color color = sr.color;
        color.a = alpha; 
        sr.color = color;
    }

    private void GridInit()
    {
        darkTiles.Clear();

        BoundsInt bounds = wallTilemap.cellBounds;

        darkTiles.Clear();

        foreach (Vector3Int tilePos in bounds.allPositionsWithin)
        {
            bool hasFloor = playgroundTilemap.HasTile(tilePos);
            bool hasWall = wallTilemap != null && wallTilemap.HasTile(tilePos);

            if (!hasFloor && !hasWall)
                continue;

            Vector2Int cell = new Vector2Int(tilePos.x, tilePos.y);
            Vector3 worldPos = mapManager.CellToWorld(cell);

            GameObject tile = Instantiate(
                darknessTilePrefab,
                worldPos,
                Quaternion.identity,
                transform
            );

            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
            darkTiles[cell] = sr;
        }
    }

}
