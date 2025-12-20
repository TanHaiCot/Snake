using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.SceneManagement;

public class AI_Snake : MonoBehaviour
{
    private enum AI_snakeState { Patrol, Chase }
    private enum AI_Mode {  FoodChaser, PlayerChaser }

    [SerializeField] private AI_snakeState currentState = AI_snakeState.Patrol;
    [SerializeField] private AI_Mode aiMode = AI_Mode.FoodChaser;

    [SerializeField] private Pathfinding pathfinding;
    [SerializeField] private Transform foodTarget;
    [SerializeField] private Transform playerSnake;
    [SerializeField] private LayerMask playerLayer; 

    private Vector2Int patrolDestination;
    private int visionRange = 10; 
    private bool hasPatrolDestination;
    
    private Vector2Int direction = Vector2Int.right;    //using Vector2Int for grid-based game
    private List<Transform> bodies = new List<Transform>();

    private float nextMoveTime;
    private float speed = 14f;
    private int initialBodyPart = 4;

    [SerializeField] Transform bodyPrefab;

    private void Start()
    {
        Restate();
    }
    private void FixedUpdate()
    {
        if (Time.time < nextMoveTime)
            return;

        nextMoveTime = Time.time + (1.0f / speed);

        UpdateAIState();

        UpdatePatrolProgress();
        UpdateHeadRotation();
        
        if(aiMode == AI_Mode.PlayerChaser)
            PlayerChaserAIUpdate();
        else if(aiMode == AI_Mode.FoodChaser)   
            FoodChaserAIUpdate(); 


        // next position of the snake head the same tick
        int nextX = Mathf.RoundToInt(this.transform.position.x) + direction.x;
        int nextY = Mathf.RoundToInt(this.transform.position.y) + direction.y;

        for (int i = bodies.Count - 1; i > 0; i--)
        {
            bodies[i].position = bodies[i - 1].position;
        }

        transform.position = new Vector3(nextX, nextY, 0);
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

    private void FoodChaserAIUpdate()
    {
        if(pathfinding == null || foodTarget == null)
        {
            Debug.Log("Pathfinding or FoodTarget is not assigned in AI_Snake");
            return;
        }
        Vector2Int startPos = pathfinding.WorldToGrid(this.transform.position);
        Vector2Int targetPos = pathfinding.WorldToGrid(foodTarget.position);

        List<Vector2Int> path = pathfinding.FindPath(startPos, targetPos, Pathfinding.PathPurpose.FoodChasing);

        // path[0] is tile next to Start; path[1] is one step after that, and so on
        // path.Count can be 0 if the snake is already on the food -> make sure path.Count > 0 to avoid error when accessing path[0] 
        if (path != null && path.Count > 0) 
        {
            Vector2Int firstStep = path[0]; //in Pathfinding, we dont count from the start position, so the first step is path[0] 
            Vector2Int newDirection = firstStep - startPos; //Calculate direction: right, left, up, down
            direction = newDirection;
        }
    }

    private void PlayerChaserAIUpdate()
    {
        if (pathfinding == null)
        {
            Debug.Log("Pathfinding is not assigned in AI_Snake");
            return;
        }
        Vector2Int startPos = pathfinding.WorldToGrid(this.transform.position);
        Vector2Int targetPos = GetCurrentTargetPos();

        List<Vector2Int> path = pathfinding.FindPath(startPos, targetPos, Pathfinding.PathPurpose.PlayerChasing);
        if (path != null && path.Count > 0)
        {
            Vector2Int firstStep = path[0]; 
            Vector2Int newDirection = firstStep - startPos;     
            direction = newDirection;
        }
        else
        {
            if (currentState == AI_snakeState.Patrol) hasPatrolDestination = false;  
        }
    }

    private Vector2Int GetCurrentTargetPos()
    {
        if(currentState == AI_snakeState.Chase)
        { 
            return pathfinding.WorldToGrid(playerSnake.position);
            
        }
        else //Patrol
        {
            EnsurePatrolDestination();
            return patrolDestination;
        }

    }

    private void UpdateAIState()
    {
        switch (currentState)
        {
            case AI_snakeState.Patrol:
                if (CanSeePlayer())
                {
                    currentState = AI_snakeState.Chase;
                    Debug.Log("AI Snake switched to Chase state");
                }
                break;
            case AI_snakeState.Chase:
                if (!CanSeePlayer())
                {
                    currentState = AI_snakeState.Patrol;
                    hasPatrolDestination = false; 
                }
                break;
        }
    }


    private bool CanSeePlayer()
    {
        if (playerSnake == null || pathfinding == null)
        {
            Debug.Log("PlayerSnake or Pathfinding is not assigned in AI_Snake");
            return false;
        }

        if (playerSnake == null || pathfinding == null) return false;

        Vector2Int myPos = pathfinding.WorldToGrid(transform.position);
        Vector2Int playerPos = pathfinding.WorldToGrid(playerSnake.position);

        int dist = Mathf.Abs(myPos.x - playerPos.x) + Mathf.Abs(myPos.y - playerPos.y);
        if (dist > visionRange) return false;

        // Optional LOS check (recommended)
        Vector2 from = new Vector2(myPos.x, myPos.y);
        Vector2 to = new Vector2(playerPos.x, playerPos.y);
        RaycastHit2D hit = Physics2D.Raycast(from, (to - from).normalized, Vector2.Distance(from, to), pathfinding.wallLayer);
        // If you don't want to expose WallLayer, you can duplicate a LayerMask field in AI_Snake and use that.

        return hit.collider == null; // no wall in between

        //Vector2 dir = new Vector2(direction.x, direction.y); 
        //if(dir == Vector2.zero)
        //    dir = Vector2.right;

        //Vector2 origin = new Vector2(transform.position.x, transform.position.y);

        //RaycastHit2D playerCheck = Physics2D.Raycast(origin, dir, visionRange, playerLayer);
        //if(playerCheck.collider == null)
        //    return false;

        //RaycastHit2D wallCheck = Physics2D.Raycast(origin, dir, visionRange, pathfinding.wallLayer);
        //if(wallCheck.collider != null && wallCheck.distance < playerCheck.distance)
        //    return false;

        //return true;
    }

    private void OnDrawGizmos()
    {
        //if (!Application.isPlaying) return;

        //Vector2 origin = (Vector2)this.transform.position;
        //Vector2 dir = new Vector2(direction.x, direction.y);
        //if(dir == Vector2.zero)
        //    dir = Vector2.right;

        //Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
        //Gizmos.DrawLine(origin, origin + dir.normalized * visionRange);

        if (!Application.isPlaying) return;
        if (playerSnake == null || pathfinding == null) return;

        Vector2Int myPos = pathfinding.WorldToGrid(transform.position);
        Vector2Int playerPos = pathfinding.WorldToGrid(playerSnake.position);

        Vector3 from = new Vector3(myPos.x + 0.5f, myPos.y + 0.5f, 0f);
        Vector3 to = new Vector3(playerPos.x + 0.5f, playerPos.y + 0.5f, 0f);

        Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
        Gizmos.DrawLine(from, to);
    }

    private void EnsurePatrolDestination()
    {
        if (hasPatrolDestination)
            return;

        if(pathfinding.TryToGetRandomWalkablePosition(out patrolDestination))
        {
            hasPatrolDestination = true;    
            Debug.Log("AI Snake new patrol destination: " + patrolDestination);
        }

    }

    private void UpdatePatrolProgress()
    {
        Vector2Int currentPos = pathfinding.WorldToGrid(this.transform.position);
        if(hasPatrolDestination && currentPos == patrolDestination)
            hasPatrolDestination = false;
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
