using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AI_Snake : MonoBehaviour
{
    private enum AI_snakeState { Patrol, Chase }
    private enum AI_Mode { FoodIsTarget, PlayerIsTarget }

    [SerializeField] private AI_Mode aiMode = AI_Mode.FoodIsTarget;

    [Header("References")]
    [SerializeField] Pathfinding pathfinding;
    [SerializeField] Transform playerSnake;
    [SerializeField] Transform bodyPrefab;
    [SerializeField] Transform foodTarget;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] Vector3 startPosition;

    [Header("State")]
    [SerializeField] private AI_snakeState currentState = AI_snakeState.Patrol;


    [Header("AI Statistics")]
    private float speed = 8f;
    private int initialBodyPart = 4;
    private float slowMultiplier = 1f;
    private float slowEndTime;

    [Header("AI Vision")]
    private int sideAwarenessRange = 10;
    private float chaseTimer;
    private float ChasingDelay = 1.5f;

    [Header("Pathfinding Performance")]
    [SerializeField, Min(0.05f)] private float chaseRepathInterval = 0.25f;

    private Vector2Int patrolDestination;
    private bool hasPatrolDestination;

    private Vector2Int lastSeenPlayerPos;
    private bool hasLastSeenPlayerPos;

    private Vector2Int direction = Vector2Int.right;    
    private float nextMoveTime;

    private List<Transform> bodies = new List<Transform>();
    private List<Vector2Int> currentPath = null;
    private int currentPathIndex;
    private Vector2Int pathTarget;
    private bool hasPathToTarget;
    private float nextChaseRepathTime;

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

    public UnityEvent<bool> OnFoodEatenByPlayer;
    public UnityEvent OnTeleGateTrigger;

    private void Start()
    {
        Restate();

        if(aiMode == AI_Mode.FoodIsTarget)
        {
            if(scoreManager != null) 
            {
                scoreManager.OnTargetReached.AddListener(() =>
                {
                    this.gameObject.SetActive(false);
                });
            }
        }
    }
    private void FixedUpdate()
    {
        float currentSpeed = GetCurrentSpped();

        if (Time.time < nextMoveTime)
            return;

        nextMoveTime = Time.time + (1.0f / currentSpeed);

        Vector2Int currentPos = pathfinding.WorldToGrid(transform.position);
        Vector2Int playerPos = playerSnake != null ? pathfinding.WorldToGrid(playerSnake.position) : default;

        bool canSeePlayer = aiMode == AI_Mode.PlayerIsTarget &&
                            playerSnake != null &&
                            CanSeePlayer(currentPos, playerPos);

        if (aiMode == AI_Mode.PlayerIsTarget)
            UpdateAIState(canSeePlayer, playerPos);

        bool gotMove = aiMode == AI_Mode.FoodIsTarget
        ? UpdateFoodChase()
        : currentState == AI_snakeState.Patrol
            ? UpdatePatrol()
            : UpdatePlayerChase(currentPos, canSeePlayer, playerPos);


        if (!gotMove)
            return;

        UpdateHeadRotation();
        MoveNextStep();
    }

    private float GetCurrentSpped()
    {
        if (Time.time < slowEndTime)
            return speed * slowMultiplier;
        
        else
          slowMultiplier = 1f;

        return speed; 
    }


    private void MoveNextStep()
    {
        Vector2Int currentCell = pathfinding.WorldToGrid(transform.position);

        previousHeadPos = currentCell;
        hasPreviousHeadPos = true;

        Vector2Int nextCell;

        if (hasNextPathCell)
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
                 OnTeleGateTrigger?.Invoke();
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

    private void UpdateAIState(bool canSeePlayer, Vector2Int playerPos)
    {
        if (canSeePlayer)
        {
            lastSeenPlayerPos = playerPos;
            hasLastSeenPlayerPos = true;
            chaseTimer = 0f;

            if (currentState != AI_snakeState.Chase)
            {
                currentState = AI_snakeState.Chase;
                ClearCurrentPath();
                hasPatrolDestination = false;
            }

            return;
        }


        if (currentState != AI_snakeState.Chase)
            return;

        chaseTimer += 1f / GetCurrentSpped();

        if (chaseTimer >= ChasingDelay)
            SwitchToPatrol();
    }

    private void SwitchToPatrol()
    {
        currentState = AI_snakeState.Patrol;

        chaseTimer = 0f;

        hasLastSeenPlayerPos = false;
        lastSeenPlayerPos = Vector2Int.zero;

        ClearCurrentPath();

        hasPatrolDestination = false;

        Debug.Log("Switch to Patrol");
    }

    private bool UpdatePatrol()
    {
        Vector2Int currentPos = pathfinding.WorldToGrid(this.transform.position);

        if(hasPatrolDestination && currentPos == patrolDestination)
        {
            hasPatrolDestination = false;
            ClearCurrentPath();
            Debug.Log("Reached patrol destination: " + patrolDestination);
        }

        if (!hasPatrolDestination)
        {
            if (!TryPickReachablePatrolDestination(currentPos))
                return false;
        }

        if (!TryGetNextPathCell(currentPos, patrolDestination, Pathfinding.PathPurpose.Patrol, out Vector2Int candidateNextCell))
        {
            Debug.Log("Patrol destination unreachable: " + patrolDestination);
            ClearCurrentPath();

            if (TryGetRecoveryDirection(currentPos, patrolDestination, out Vector2Int recoveryDir))
            {
                direction = recoveryDir;
                return true;
            }

            hasPatrolDestination = false;
            return false;
        }

        nextPathCell = candidateNextCell;
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
                currentPathIndex = 0;
                pathTarget = candidate;
                hasPathToTarget = true;
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

        if (!TryGetNextPathCell(startPos, targetPos, Pathfinding.PathPurpose.FoodChasing, out Vector2Int candidateNextCell))
        {
            Debug.Log("No path to food: " + targetPos);
            return false;
        }

        nextPathCell = candidateNextCell;
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

    private bool UpdatePlayerChase(Vector2Int currentPos, bool canSeePlayer, Vector2Int playerPos)
    {
        if (playerSnake == null || pathfinding == null)
            return false;

        if (canSeePlayer)
        {
            if(pathfinding.IsMapWalkable(playerPos)) 
            {
                lastSeenPlayerPos = playerPos;
                hasLastSeenPlayerPos = true;
            }
        }

        if (!hasLastSeenPlayerPos)
            return false;

        Vector2Int targetPos = lastSeenPlayerPos;

        if(!canSeePlayer && currentPos == targetPos)
        {
            SwitchToPatrol(); 
            return false;
        }

        if (!TryGetNextPathCell(currentPos, targetPos, Pathfinding.PathPurpose.PlayerChasing, out Vector2Int candidateNextCell))
        {
            hasNextPathCell = false;    

            if (TryGetRecoveryDirection(currentPos, targetPos, out Vector2Int recoveryDir))
            {
                direction = recoveryDir;
                return true;
            }

            return false;
        }

        Vector2Int desiredDir = candidateNextCell - currentPos;

        bool isNormalMove =
        Mathf.Abs(desiredDir.x) +
        Mathf.Abs(desiredDir.y) == 1;

        if (isNormalMove &&
        pathfinding.IsMapWalkable(candidateNextCell))
        {
            nextPathCell = candidateNextCell;
            hasNextPathCell = true;
            direction = desiredDir;
            return true;
        }

        bool isTeleportMove = pathfinding.TryGetTeleportExit(currentPos, out Vector2Int teleportExit) && teleportExit == candidateNextCell;

        if (isTeleportMove)
        {
            nextPathCell = candidateNextCell;
            hasNextPathCell = true;
            return true;
        }

        hasNextPathCell = false;

        if (TryGetRecoveryDirection(currentPos, targetPos, out Vector2Int fallbackDir))
        {
            direction = fallbackDir;
            return true;
        }

        return false;
    }

    // Keep following a valid path instead of allocating and searching the whole map every move.
    // Repath only when the target changes or the next step becomes blocked.
    private bool TryGetNextPathCell(Vector2Int currentPos, Vector2Int targetPos,
        Pathfinding.PathPurpose purpose, out Vector2Int nextCell)
    {
        nextCell = Vector2Int.zero;

        bool targetChanged = !hasPathToTarget || pathTarget != targetPos;
        bool pathFinished = currentPath == null || currentPathIndex >= currentPath.Count;
        bool chaseRepathDue = purpose != Pathfinding.PathPurpose.PlayerChasing ||
                              Time.time >= nextChaseRepathTime;

        if ((!targetChanged || !chaseRepathDue) && !pathFinished)
        {
            Vector2Int nextPathCell = currentPath[currentPathIndex];        // next cell in the saved path
            bool isTeleport = pathfinding.TryGetTeleportExit(currentPos, out Vector2Int teleportExit) &&
                              teleportExit == nextPathCell;
            bool isAdjacent = ManhattanDistance(currentPos, nextPathCell) == 1;
            bool canEnterTarget = purpose == Pathfinding.PathPurpose.PlayerChasing && nextPathCell == targetPos;

            if ((isTeleport || isAdjacent) &&
                (canEnterTarget || pathfinding.IsWalkable(nextPathCell, purpose)))
            {
                nextCell = nextPathCell;
                currentPathIndex++;
                return true;
            }
        }

        currentPath = pathfinding.FindPath(currentPos, targetPos, purpose);
        currentPathIndex = 0;
        pathTarget = targetPos;
        hasPathToTarget = true;

        if (purpose == Pathfinding.PathPurpose.PlayerChasing)
            nextChaseRepathTime = Time.time + chaseRepathInterval;

        if (currentPath == null || currentPath.Count == 0)
            return false;

        nextCell = currentPath[currentPathIndex++];
        return true;
    }

    public void ApplySlowEffect(float slowPercentage, float duration)
    {
        float multiplier = 1 - Mathf.Clamp01(slowPercentage);

        slowMultiplier = multiplier;
        slowEndTime = Mathf.Max(slowEndTime, Time.time) + duration;
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private void ClearCurrentPath()
    {
        currentPath = null;
        currentPathIndex = 0;
        hasPathToTarget = false;
        hasNextPathCell = false;
        nextChaseRepathTime = 0f;
    }

    private bool CanSeePlayer(Vector2Int myPos, Vector2Int playerPos)
    {
        int distance = Mathf.Abs(myPos.x - playerPos.x) + Mathf.Abs(myPos.y - playerPos.y);

        return distance <= sideAwarenessRange &&
           pathfinding.IsMapWalkable(playerPos) &&
           HasLineOfSight(myPos, playerPos);
    }

    private bool HasLineOfSight(Vector2Int from, Vector2Int to)        //Bresenham's Line Algorithm
    {
        int x = from.x;
        int y = from.y;
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        int stepX = from.x < to.x ? 1 : -1;
        int stepY = from.y < to.y ? 1 : -1;
        int error = dx - dy;

        while (x != to.x || y != to.y)
        {
            int doubledError = error * 2;

            if (doubledError > -dy)
            {
                error -= dy;
                x += stepX;
            }

            if (doubledError < dx)
            {
                error += dx;
                y += stepY;
            }

            Vector2Int cell = new Vector2Int(x, y);

            if (cell != to &&
                !pathfinding.IsMapWalkable(cell))
            {
                return false;
            }
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
    // to avoid going into dead ends when chasing the player
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
            Gizmos.color = CanSeePlayer(myPos, playerPos) ? Color.green : Color.red;
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
        slowMultiplier = 1f;
        slowEndTime = 0f;

        direction = Vector2Int.right;
        Vector2Int startCell = pathfinding.WorldToGrid(startPosition);
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
            OnFoodEatenByPlayer?.Invoke(false);
            Grow();
        }
    }
}
