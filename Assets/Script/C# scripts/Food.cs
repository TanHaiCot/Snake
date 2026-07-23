using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Food : MonoBehaviour
{
    [SerializeField] MapManager mapManager;
   
    [SerializeField] Snake snake;
    [SerializeField] AI_Snake opponentSnake;
    [SerializeField] ScoreManager scoreManager;

    private void Start()
    {
        scoreManager.OnTargetReached.AddListener( () =>
        {
            this.gameObject.SetActive(false);
        });
    }

    public void RandomizedSpawn()
    {
        if (mapManager == null)
        {
            Debug.LogError("MapManager is not assigned!");
            return;
        }

        List<Vector2Int> freeSpots = new();
        
        foreach (Vector2Int cell in mapManager.GetWalkableCells())
        {
            bool occupiedByPlayer = snake.SpotOccupied(cell.x, cell.y);

            bool occupiedByOpponent = (opponentSnake != null && opponentSnake.SpotOccupied(cell.x, cell.y));

            if (!occupiedByPlayer && !occupiedByOpponent)
            {
                freeSpots.Add(cell);
            }
            
        }

        if (freeSpots.Count == 0)
        {
            Debug.LogWarning("No free spot found for food.");
            return;
        }

        Vector2Int chosenSpot = freeSpots[Random.Range(0, freeSpots.Count)];

        //Debug.Log($"Chosen Food Cell: {chosenSpot} | Walkable: {mapManager.IsWalkable(chosenSpot)}");

        transform.position = new Vector2(chosenSpot.x, chosenSpot.y);

        //Debug.Log($"Food spawned at: {chosenSpot.x}, {chosenSpot.y}");  

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player" || collision.CompareTag("Opponent Snake"))
        {
            RandomizedSpawn();
        }
    }
}
