using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AI_Snake : MonoBehaviour
{
    [SerializeField] private Pathfinding pathfinding;
    [SerializeField] private Transform foodTarget;

    private Vector2Int direction = Vector2Int.right;    //using Vector2Int for grid-based game
    private List<Transform> bodies = new List<Transform>();

    private float nextMoveTime;
    private float speed = 10f;
    private int initialBodyPart = 4;

    [SerializeField] Transform bodyPrefab;

    private void Start()
    {
        Restate();
    }

    private void UpdateHeadRotation()
    {
        float angle = 0f;

        if (direction == Vector2Int.up)
            angle = 90f;
        else if (direction == Vector2Int.down)
            angle = -90f;
        else if (direction == Vector2Int.left)
            angle = 180f;
        else if (direction == Vector2Int.right)
            angle = 0f;

        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void UpdateAIDirection()
    {
        Vector2Int startPos = pathfinding.WorldToGrid(this.transform.position);
        Vector2Int targetPos = pathfinding.WorldToGrid(foodTarget.position);

        List<Vector2Int> path = pathfinding.FindPath(startPos, targetPos);

        // path[0] is tile next to Start; path[1] is one step after that, and so on.
        if (path != null)
        {
            Vector2Int firstStep = path[0]; //in Pathfinding, we dont count the start position, so the first step is path[0] 
            Vector2Int newDirection = firstStep - startPos; //Calculate direction: right, left, up, down
            direction = newDirection;
        }
    }
    private void FixedUpdate()
    {
        if (Time.time < nextMoveTime)
            return;

        nextMoveTime = Time.time + (1.0f / speed);
        Debug.Log("next time move of AI" + nextMoveTime);
        UpdateHeadRotation();
        UpdateAIDirection(); 

        // next position of the snake head the same tick
        int nextX = Mathf.RoundToInt(this.transform.position.x) + direction.x;
        int nextY = Mathf.RoundToInt(this.transform.position.y) + direction.y;


        //check all the collision on the world space to see which one is overlap with the next position

        for (int i = bodies.Count - 1; i > 0; i--)
        {
            bodies[i].position = bodies[i - 1].position;
        }

        transform.position = new Vector3(nextX, nextY, 0);
    }

    public void Restate()
    {
        direction = Vector2Int.right;
        this.transform.position = Vector3.zero;

        for (int i = 1; i < bodies.Count; i++)
        {
            Destroy(bodies[i].gameObject);
        }
        bodies.Clear();
        bodies.Add(this.transform);

        for (int i = 0; i < initialBodyPart - 1; i++)   //not count the Head 
        {
            Grow();
        }
    }

    public bool SpotOccupied(int x, int y)
    {
        foreach (Transform body in bodies)
        {
            if (Mathf.RoundToInt(body.position.x) == x &&
                Mathf.RoundToInt(body.position.y) == y)
            {
                return true;
            }
        }
        return false;
    }


    private void Grow()
    {
        Transform bodyPart = Instantiate(bodyPrefab);
        bodyPart.position = bodies[bodies.Count - 1].position;
        bodies.Add(bodyPart);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Food"))
        {
            Debug.Log("AI Snake ate food");
            Grow();
        }
    }
}
