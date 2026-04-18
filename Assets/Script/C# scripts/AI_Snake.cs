using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.SceneManagement;
using static UnityEditor.PlayerSettings;

public class AI_Snake : MonoBehaviour
{
    private enum AI_snakeState { Patrol, Chase }
    private enum AI_Mode { FoodChaser, PlayerChaser }

    [SerializeField] private AI_Mode aiMode = AI_Mode.FoodChaser;

    [Header("References")]
    [SerializeField] private Pathfinding pathfinding;
    [SerializeField] private Transform playerSnake;
    [SerializeField] private Transform bodyPrefab;
    [SerializeField] private LayerMask playerLayer; 
    [SerializeField] private Transform foodTarget;

    [Header("State")]
    [SerializeField] private AI_snakeState currentState = AI_snakeState.Patrol;


    [Header("AI Statistics")]
    private float speed = 8f;
    private int initialBodyPart = 4;

    [Header("AI Vision")]
    private int sideAwarenessRange = 6;
    private float lostSightTimer;
    private float loseSightDelay = 2f;

    private Vector2Int patrolDestination;
    private bool hasPatrolDestination;

    private Vector2Int lastSeenPlayerPos;
    private bool hasLastSeenPlayerPos;

    private Vector2Int direction = Vector2Int.right;    
    private float nextMoveTime;

    private List<Transform> bodies = new List<Transform>();
    private List<Vector2Int> currentPath = null;

    private int patrolPickAttempts = 30;
    private bool drawPath = true;

    private void Start()
    {
        Restate();
    }
    private void FixedUpdate()
    {
        UpdateAIState();

        if (Time.time < nextMoveTime)
            return;

        nextMoveTime = Time.time + (1.0f / speed);

        bool gotMove = false;

        if (aiMode == AI_Mode.FoodChaser)
            FoodChaserAIUpdate();


        switch (currentState)
        {
            case AI_snakeState.Patrol:
                gotMove = UpdatePatrol();
                break;
            case AI_snakeState.Chase:
                gotMove = UpdatePlayerChase();
                break;
        }

        UpdateHeadRotation();

        if (!gotMove)
            return;

        MoveNextStep(); 


    }

    private void MoveNextStep()
    {
        // next position of the snake head the same tick
        int nextX = Mathf.RoundToInt(this.transform.position.x) + direction.x;
        int nextY = Mathf.RoundToInt(this.transform.position.y) + direction.y;

        for (int i = bodies.Count - 1; i > 0; i--)
        {
            bodies[i].position = bodies[i - 1].position;
        }

        transform.position = new Vector3(nextX, nextY, 0);
    }
    private void UpdateAIState()
    {
        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            lastSeenPlayerPos = pathfinding.WorldToGrid(playerSnake.position);
            hasLastSeenPlayerPos = true;
            lostSightTimer = 0f;

            if (currentState != AI_snakeState.Chase)
            {
                currentState = AI_snakeState.Chase;
                Debug.Log("Switch to Chase");
            }

            return;
        }

        if (currentState == AI_snakeState.Chase)
        {
            lostSightTimer += Time.fixedDeltaTime;

            if (lostSightTimer >= loseSightDelay)
            {
                currentState = AI_snakeState.Patrol;
                lostSightTimer = 0f;
                hasLastSeenPlayerPos = false;
                currentPath = null;
                Debug.Log("Switch to Patrol");
            }
        }
    }

    private bool UpdatePatrol()
    {
        Vector2Int currentPos = pathfinding.WorldToGrid(this.transform.position);

        if(hasPatrolDestination && currentPos == patrolDestination)
        {
            hasPatrolDestination = false;
            currentPath = null;
            Debug.Log("Reached patrol destination: " + patrolDestination);
        }

        if (!hasPatrolDestination)
        {
            if (!TryPickReachablePatrolDestination(currentPos))
                return false;
        }

        currentPath = pathfinding.FindPath(currentPos, patrolDestination, Pathfinding.PathPurpose.PlayerChasing);

        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.Log("Patrol destination unreachable: " + patrolDestination);

            //if (pauseWhenPatrolUnreachable)
            //{
            //    Debug.Break();
            //}

            hasPatrolDestination = false;
            currentPath = null;
            return false;
        }

        Vector2Int firstStep = currentPath[0];
        direction = firstStep - currentPos;
        return true;
    }

    private bool TryPickReachablePatrolDestination(Vector2Int startPos)
    {
        for (int i = 0; i < patrolPickAttempts; i++)
        {
            Vector2Int candidate;
            if (!pathfinding.TryToGetRandomWalkablePosition(out candidate))
                return false;

            List<Vector2Int> path = pathfinding.FindPath(startPos, candidate, Pathfinding.PathPurpose.PlayerChasing);

            if (path != null && path.Count > 0)
            {
                patrolDestination = candidate;
                hasPatrolDestination = true;
                currentPath = path;
                Debug.Log("New patrol destination: " + patrolDestination);
                return true;
            }
        }

        Debug.Log("Could not find reachable patrol destination");

        //if (pauseWhenPatrolUnreachable)
        //{
        //    Debug.Break();
        //}

        return false;
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

    private bool UpdatePlayerChase()
    {
        if (!hasLastSeenPlayerPos)
            return false;

        Vector2Int currentPos = pathfinding.WorldToGrid(transform.position);
        Vector2Int targetPos = lastSeenPlayerPos;

        currentPath = pathfinding.FindPath(currentPos, targetPos, Pathfinding.PathPurpose.PlayerChasing);

        if (currentPath == null || currentPath.Count == 0)
        {
            currentPath = null;
            return false;
        }

        Vector2Int firstStep = currentPath[0];
        direction = firstStep - currentPos;
        return true;
    }

 

    private bool CanSeePlayer()
    {
        if (playerSnake == null || pathfinding == null)
            return false;

        Vector2Int myPos = pathfinding.WorldToGrid(transform.position);
        Vector2Int playerPos = pathfinding.WorldToGrid(playerSnake.position);

        Vector2Int delta = playerPos - myPos;
        int dist = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

        bool sameRow = myPos.y == playerPos.y;
        bool sameCol = myPos.x == playerPos.x;

        Vector2Int sightDir = Vector2Int.zero;

        if (sameRow)
        {
            if (playerPos.x > myPos.x) sightDir = Vector2Int.right;
            else if (playerPos.x < myPos.x) sightDir = Vector2Int.left;
        }
        else if (sameCol)
        {
            if (playerPos.y > myPos.y) sightDir = Vector2Int.up;
            else if (playerPos.y < myPos.y) sightDir = Vector2Int.down;
        }

        if (sightDir == Vector2Int.zero)
            return false;

        if (sightDir != direction)
            return false;

        if (dist > sideAwarenessRange)
            return false;

        return HasLineOfSight(myPos, playerPos);
    }

    private bool HasLineOfSight(Vector2Int fromGrid, Vector2Int toGrid)
    {
        Vector2 from = new Vector2(fromGrid.x, fromGrid.y);
        Vector2 to = new Vector2(toGrid.x, toGrid.y);
        Vector2 dir = (to - from).normalized;
        float distance = Vector2.Distance(from, to);

        RaycastHit2D hit = Physics2D.Raycast(from, dir, distance, pathfinding.wallLayer);
        return hit.collider == null;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || pathfinding == null)
            return;

        Vector2Int myPos = pathfinding.WorldToGrid(transform.position);

        if (hasPatrolDestination)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(patrolDestination.x, patrolDestination.y, 0f), Vector3.one);
            Gizmos.DrawLine(new Vector3(myPos.x, myPos.y, 0f), new Vector3(patrolDestination.x, patrolDestination.y, 0f));
        }

        if (playerSnake != null)
        {
            Vector2Int playerPos = pathfinding.WorldToGrid(playerSnake.position);
            Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(new Vector3(myPos.x, myPos.y, 0f), new Vector3(playerPos.x, playerPos.y, 0f));
        }

        if (drawPath && currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;

            Vector3 prev = new Vector3(myPos.x, myPos.y, 0f);
            for (int i = 0; i < currentPath.Count; i++)
            {
                Vector3 next = new Vector3(currentPath[i].x, currentPath[i].y, 0f);
                Gizmos.DrawLine(prev, next);
                Gizmos.DrawSphere(next, 0.12f);
                prev = next;
            }
        }
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
