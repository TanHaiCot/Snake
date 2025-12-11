using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

public class DarknessManager : MonoBehaviour
{
    [SerializeField] private BoxCollider2D darkGrid;

    [SerializeField] private GameObject darknessTilePrefab;
    [SerializeField] private Transform snake;
    [SerializeField] LayerMask wallLayer;

    private int visableRange = 3;

    private Dictionary<Vector2Int, SpriteRenderer> darkTiles = new Dictionary<Vector2Int, SpriteRenderer>();
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

        Vector2Int startPotition = WorldToGrid(snake.position); 

        Queue<Vector2Int> posQueue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        posQueue.Enqueue(startPotition);
        visited.Add(startPotition);

        while(posQueue.Count > 0)
        {
            Vector2Int pos = posQueue.Dequeue();

            if(!darkTiles.TryGetValue(pos, out SpriteRenderer sr))
                continue;

            SetAlpha(sr, 0f);

            // Limit vision by range (Chebyshev distance)
            int distance = Mathf.Max(Mathf.Abs(pos.x - startPotition.x) + Mathf.Abs(pos.y - startPotition.y)); 
            if(distance >= visableRange)
                continue;

            bool isWall = Physics2D.OverlapBox(new Vector2(pos.x, pos.y), new Vector2(0.9f, 0.9f), 0, wallLayer) != null;
            if (isWall)
                continue;

            foreach (var dir in directions)
            {
                Vector2Int neighbor = pos + dir;

                if(!visited.Add(neighbor))
                    continue;

                posQueue.Enqueue(neighbor);
            }

        }
        
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    }

    private void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color color = sr.color;
        color.a = alpha; 
        sr.color = color;
    }

    private void GridInit()
    {
        Bounds bound = darkGrid.bounds;

        int minX = Mathf.RoundToInt(bound.min.x);
        int maxX = Mathf.RoundToInt(bound.max.x);
        int minY = Mathf.RoundToInt(bound.min.y);
        int maxY = Mathf.RoundToInt(bound.max.y);

        darkTiles.Clear(); 

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                GameObject tile = Instantiate(darknessTilePrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();

                darkTiles[pos] = sr;
            }
        }

    }
}
