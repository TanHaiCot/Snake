using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class Food : MonoBehaviour
{
    [SerializeField] BoxCollider2D gridArea;
   
    [SerializeField] Snake snake;
    [SerializeField] AI_Snake opponentSnake;

    [SerializeField] LayerMask wallLayer; 

    private void Start()
    {
        RandomizedSpawn(); 
    }

    public void RandomizedSpawn()
    {
        Bounds bounds = this.gridArea.bounds;

        int minX = Mathf.RoundToInt(bounds.min.x);
        int maxX = Mathf.RoundToInt(bounds.max.x);
        int minY = Mathf.RoundToInt(bounds.min.y);
        int maxY = Mathf.RoundToInt(bounds.max.y);

        List<Vector2Int> freeSpots = new List<Vector2Int>((maxX - minX + 1) * (maxY - minY + 1));
        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                bool occupiedByPlayer = snake.SpotOccupied(x, y);
                bool occupiedByOpponent = (opponentSnake != null && opponentSnake.SpotOccupied(x, y));

                if (!occupiedByPlayer && !occupiedByOpponent && !IsWall(x, y))
                {
                    freeSpots.Add(new Vector2Int(x, y));
                }
            }
        }

        Vector2Int chosenSpot = freeSpots[Random.Range(0, freeSpots.Count)];
        this.transform.position = new Vector2(chosenSpot.x, chosenSpot.y);

    }

    private bool IsWall(int x, int y)
    {
        return Physics2D.OverlapBox(new Vector2(x, y), new Vector2(0.9f, 0.9f), 0f, wallLayer) != null;  //OverlapBox returns null if no collider found
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player" || collision.CompareTag("Opponent Snake"))
        {
            RandomizedSpawn();
        }
    }
}
