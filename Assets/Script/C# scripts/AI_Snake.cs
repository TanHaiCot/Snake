using System.Collections.Generic;
using UnityEngine;

public class AI_Snake : MonoBehaviour
{
    private enum AI_snakeState { Patrol, Chase }
    private enum AI_Mode { FoodIsTarget, PlayerIsTarget }

    [SerializeField] private AI_Mode aiMode = AI_Mode.FoodIsTarget;

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
    private int sideAwarenessRange = 10;
    private float ChaseTimer;
    private float ChasingDelay = 1.5f;

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

    private Vector2Int nextPathCell;
    private bool hasNextPathCell;

    private static readonly Vector2Int[] FourDirs =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private Vector2Int previousHeadPos;
    private bool hasPreviousHeadPos;

    private void Start()
    {
        Restate();
    }
    private void FixedUpdate()
    {
        if (Time.time < nextMoveTime)
            return;

        nextMoveTime = Time.time + (1.0f / speed);

        bool gotMove = false;

        if (aiMode == AI_Mode.FoodIsTarget)
            gotMove = UpdateFoodChase();

        else if (aiMode == AI_Mode.PlayerIsTarget)
        {
            UpdateAIState(); 
            switch (currentState)
            {
                case AI_snakeState.Patrol:
                    gotMove = UpdatePatrol();
                    break;
                case AI_snakeState.Chase:
                    gotMove = UpdatePlayerChase();
                    break;
            }

        }

        UpdateHeadRotation();

        if (!gotMove)
            return;

        MoveNextStep(); 
    }

    private void MoveNextStep()
    {
        Vector2Int currentCell = pathfinding.WorldToGrid(transform.position);

        previousHeadPos = currentCell;
        hasPreviousHeadPos = true;

        Vector2Int nextCell;

        if (hasNextPathCell/*currentPath != null && currentPath.Count > 0*/)
        {
            nextCell = nextPathCell;

            Vector2Int diff = nextCell - currentCell;

            // Normal 1-cell move
            if (Mathf.Abs(diff.x) + Mathf.Abs(diff.y) == 1)
            {
                direction = diff;
            }
            // Teleport move: nextCell is far away, but valid
            else if (pathfinding.TryGetTeleportExit(currentCell, out Vector2Int teleportExit) &&
                     teleportExit == nextCell)
            {
                // Do nothing. Keep current direction.
            }
            // Invalid path step
            else
            {
                hasNextPathCell = false;
                return;
            }
        }
        else
        {
            nextCell = currentCell + direction;
        }

        Vector3 nextWorldPos = pathfinding.GridToWorld(nextCell);

        for (int i = bodies.Count - 1; i > 0; i--)
        {
            bodies[i].position = bodies[i - 1].position;
        }

        transform.position = nextWorldPos;

        hasNextPathCell = false;
    }

    private void UpdateAIState()
    {
        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            lastSeenPlayerPos = pathfinding.WorldToGrid(playerSnake.position);
            hasLastSeenPlayerPos = true;
            ChaseTimer = 0f;

            if (currentState != AI_snakeState.Chase)
            {
                currentState = AI_snakeState.Chase;
                currentPath = null;
                hasPatrolDestination = false;
                Debug.Log("Switch to Chase");
            }

            return;
        }

        if (currentState == AI_snakeState.Chase)
        {
            ChaseTimer += Time.fixedDeltaTime;

            if (ChaseTimer >= ChasingDelay)
            {
                currentState = AI_snakeState.Patrol;
                ChaseTimer = 0f;
                hasLastSeenPlayerPos = false;
                currentPath = null;
                hasPatrolDestination = false;
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

        currentPath = pathfinding.FindPath(currentPos, patrolDestination, Pathfinding.PathPurpose.Patrol);

        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.Log("Patrol destination unreachable: " + patrolDestination);
            currentPath = null;

            if (TryGetRecoveryDirection(currentPos, patrolDestination, out Vector2Int recoveryDir))
            {
                direction = recoveryDir;
                return true;
            }

            hasPatrolDestination = false;
            return false;
        }

        nextPathCell = currentPath[0];
        hasNextPathCell = true;
        Vector2Int desiredDirection = nextPathCell - currentPos;

        if (pathfinding.IsWalkable(currentPos + desiredDirection, Pathfinding.PathPurpose.Patrol))
        {
            direction = desiredDirection;
            return true;
        }

        if (TryGetRecoveryDirection(currentPos, patrolDestination, out Vector2Int backupDir))
        {
            direction = backupDir;
            return true;
        }

        return false;
    }

    private bool TryPickReachablePatrolDestination(Vector2Int startPos)
    {
        for (int i = 0; i < patrolPickAttempts; i++)
        {
            Vector2Int candidate;
            if (!pathfinding.TryToGetRandomWalkablePosition(out candidate))
                return false;

            List<Vector2Int> path = pathfinding.FindPath(startPos, candidate, Pathfinding.PathPurpose.Patrol);

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

    private bool UpdateFoodChase()
    {
        if (pathfinding == null || foodTarget == null)
        {
            Debug.Log("Pathfinding or FoodTarget is not assigned in AI_Snake");
            return false;
        }

        Vector2Int startPos = pathfinding.WorldToGrid(transform.position);
        Vector2Int targetPos = pathfinding.WorldToGrid(foodTarget.position);

        currentPath = pathfinding.FindPath(
            startPos,
            targetPos,
            Pathfinding.PathPurpose.FoodChasing
        );

        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.Log("No path to food: " + targetPos);
            return false;
        }

        nextPathCell = currentPath[0];
        hasNextPathCell = true;

        Vector2Int newDirection = nextPathCell - startPos;

        if (Mathf.Abs(newDirection.x) + Mathf.Abs(newDirection.y) == 1)
        {
            direction = newDirection;
            return true;
        }

        if (pathfinding.TryGetTeleportExit(startPos, out Vector2Int teleportExit) && teleportExit == nextPathCell)
        {
            return true; // teleport move is valid even though it is not adjacent
        }

        return false; 
    }

    private bool UpdatePlayerChase()
    {
        if (playerSnake == null || pathfinding == null)
            return false;

        Vector2Int currentPos = pathfinding.WorldToGrid(transform.position);

        if (CanSeePlayer())
        {
            lastSeenPlayerPos = pathfinding.WorldToGrid(playerSnake.position);
            hasLastSeenPlayerPos = true;
        }

        if (!hasLastSeenPlayerPos)
            return false;

        Vector2Int targetPos = lastSeenPlayerPos;

        currentPath = pathfinding.FindPath(currentPos, targetPos, Pathfinding.PathPurpose.PlayerChasing);

        if (currentPath == null || currentPath.Count == 0)
        {
            currentPath = null;
            if (TryGetRecoveryDirection(currentPos, targetPos, out Vector2Int recoveryDir))
            {
                direction = recoveryDir;
                return true;
            }

            return false;
        }

        nextPathCell = currentPath[0];
        hasNextPathCell = true;
        Vector2Int desiredDir = nextPathCell - currentPos;

        if (pathfinding.IsWalkable(currentPos + desiredDir, Pathfinding.PathPurpose.PlayerChasing) || currentPos + desiredDir == targetPos)
        {
            direction = desiredDir;
            return true;
        }

        if (TryGetRecoveryDirection(currentPos, targetPos, out Vector2Int fallbackDir))
        {
            direction = fallbackDir;
            return true;
        }

        return false;
    }

 

    private bool CanSeePlayer()
    {
        if (playerSnake == null || pathfinding == null)
            return false;

        Vector2Int myPos = pathfinding.WorldToGrid(transform.position);
        Vector2Int playerPos = pathfinding.WorldToGrid(playerSnake.position);

        int dist = Mathf.Abs(myPos.x - playerPos.x) + Mathf.Abs(myPos.y - playerPos.y);

        if (dist > sideAwarenessRange)
            return false;

        return HasLineOfSight(myPos, playerPos);
    }

    private bool HasLineOfSight(Vector2Int fromGrid, Vector2Int toGrid)
    {
        //Vector2 from = new Vector2(fromGrid.x, fromGrid.y);
        //Vector2 to = new Vector2(toGrid.x, toGrid.y);
        //Vector2 dir = (to - from).normalized;
        //float distance = Vector2.Distance(from, to);

        Vector3 fromWorld = pathfinding.GridToWorld(fromGrid);
        Vector3 toWorld = pathfinding.GridToWorld(toGrid);

        Vector3 direction = toWorld - fromWorld;
        float distance = direction.magnitude;

        if (distance <= 0f)
            return true;

        direction.Normalize();

        //check every quarter of a tile along the line to see if there's an obstacle
        //smaller step size = more accurate but more expensive
        //adjust as needed for performance vs accuracy
        float stepSizePerCheck = 0.25f;  
        float travelled = stepSizePerCheck;

        while (travelled < distance)
        {
            Vector3 checkWorldPos = fromWorld + direction * travelled;
            Vector2Int checkCell = pathfinding.WorldToGrid(checkWorldPos);

            // Ignore start and target cells
            if (checkCell != fromGrid && checkCell != toGrid)
            {
                if (!pathfinding.IsWalkable(checkCell, Pathfinding.PathPurpose.Patrol))
                    return false;
            }

            travelled += stepSizePerCheck;
        }

        return true;
    }


    private bool TryGetRecoveryDirection(Vector2Int currentPos, Vector2Int targetPos, out Vector2Int bestDir)
    {
        bestDir = Vector2Int.zero;

        int bestScore = int.MinValue;

        for (int i = 0; i < FourDirs.Length; i++)
        {
            Vector2Int dir = FourDirs[i];
            Vector2Int next = currentPos + dir;

            if (!pathfinding.IsWalkable(next, Pathfinding.PathPurpose.Patrol))
                continue;

            bool isBacktrack = hasPreviousHeadPos && next == previousHeadPos;

            int distanceScore = -Mathf.Abs(targetPos.x - next.x) - Mathf.Abs(targetPos.y - next.y);
            int exitScore = CountWalkableNeighbors(next);

            int totalScore = distanceScore + (exitScore * 2);

            if (!isBacktrack)
            {
                totalScore += 10; // strongly prefer not to bounce back
            }

            if (exitScore <= 1)
            {
                totalScore -= 5;
            }


            if (totalScore > bestScore)
            {
                bestScore = totalScore;
                bestDir = dir;
            }
        }

        if (bestDir == Vector2Int.zero)
            return false;

        return true;
    }

    // count how many walkable tiles around this position
    // as a heuristic to avoid going into dead ends when chasing the player
    private int CountWalkableNeighbors(Vector2Int pos)
    {
        int count = 0;

        for (int i = 0; i < FourDirs.Length; i++)
        {
            Vector2Int next = pos + FourDirs[i];
            if (pathfinding.IsWalkable(next, Pathfinding.PathPurpose.Patrol))
                count++;
        }

        return count;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || pathfinding == null)
            return;

        Vector2Int myPos = pathfinding.WorldToGrid(transform.position);

        if (hasPatrolDestination)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(pathfinding.GridToWorld(patrolDestination), Vector3.one);
            Gizmos.DrawLine(pathfinding.GridToWorld(myPos), pathfinding.GridToWorld(patrolDestination));
        }

        if (playerSnake != null)
        {
            Vector2Int playerPos = pathfinding.WorldToGrid(playerSnake.position);
            Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(pathfinding.GridToWorld(myPos), pathfinding.GridToWorld(playerPos));
        }

        if (drawPath && currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;

            Vector3 prev = pathfinding.GridToWorld(myPos);
            for (int i = 0; i < currentPath.Count; i++)
            {
                Vector3 next = pathfinding.GridToWorld(currentPath[i]   );
                Gizmos.DrawLine(prev, next);
                Gizmos.DrawSphere(next, 0.12f);
                prev = next;
            }
        }
    }



    public void Restate()
    {
        direction = Vector2Int.right;
        Vector2Int startCell = pathfinding.WorldToGrid(Vector3.zero);
        transform.position = pathfinding.GridToWorld(startCell);

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
        Vector2Int targetCell = new Vector2Int(x, y);

        foreach (Transform body in bodies)
        {
            Vector2Int bodyCell = pathfinding.WorldToGrid(body.position);
            
            if (bodyCell == targetCell)
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
