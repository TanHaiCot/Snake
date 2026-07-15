using System.Collections.Generic;
using UnityEngine;

public class FirstBoss : MonoBehaviour
{
    public enum BossState
    {
        Chasing,
        Lagging,
        Dashing,
        Stunned,
        Fleeing,
        Die
    }

    [Header("Map Settings")]
    [SerializeField] private MapManager mapManager;

    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] Transform bossBodyPrefab; 
    [SerializeField] BossFightManager bossFightManager;

    [Header("Boss Settings")]
    int initialBodySegments = 3;   
    private float chaseSpeed = 7.5f;          // steps/sec
    private float fleeSpeed = 10f; 
    private float dashSpeed = 22f;          // steps/sec during dash
    private float dashTriggerRange = 10f;
    private int currentDashDistance; 
    private int maxDashDistance = 8; 
    private float laggingDelay = 0.2f;       // seconds between steps when lagging
    private float stunDuration = 3f;
    private float stunByHitDuration = 5f;     // stun time when player hits boss (e.g. with empowered bite)

    [Header("Power-up Drop")]
    [SerializeField] GameObject powerUpPrefab;

    public BossState state = BossState.Chasing;

    public Vector2Int AnchorCell { get; private set; }

    private List<Transform> bossBodies = new();
    const int BossCellSize = 2;
    private List<Vector2Int> anchorHistory = new();    

    private float moveTimer;
    private float stateTimer;

    private Vector2Int direction = Vector2Int.right; 

    Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

    // pathfiding variables
    private float repathInterval = 0.12f;
    private float repathTimer;
    private int maxVisitedNodes = 20000;
    private readonly List<Vector2Int> currentPath = new();
    private int pathIndex;
    private Vector2Int cachedPathPlayerCell;
    private bool hasCachedPathPlayerCell;

    private Vector2Int lastDir = Vector2Int.zero;

    public bool IsDangerous => state != BossState.Die && state != BossState.Stunned;

    private void Start()
    {
        Restate();
    }

    private void Update()
    {
        if (state == BossState.Die) return;
    
        if (Input.GetKeyDown(KeyCode.K))
        {
            SpawnPowerPickup();
            Debug.Log("K pressed: spawned boss pickup");
        }
    }

    private void FixedUpdate()
    {
        if(state == BossState.Die)
            return;
    
        UpdateState();

        if (state == BossState.Chasing || state == BossState.Fleeing)
            repathTimer -= Time.fixedDeltaTime;

        if(state == BossState.Lagging || state == BossState.Stunned)
            return;

        float currentSpeed;
        if(state == BossState.Dashing) currentSpeed = dashSpeed;
        else if(state == BossState.Fleeing) currentSpeed = fleeSpeed;
        else currentSpeed = chaseSpeed;

        float interval = 1f / currentSpeed; 


        moveTimer += Time.fixedDeltaTime;
        if (moveTimer >= interval) 
        {
            moveTimer -= interval;
            bool moved = TakeAction();

            if (moved)
            {
                RecordAnchor();
                UpdateBody();
            }

            UpdateHeadRotation();
        }


    }

    private void UpdateState()
    {
        if(state == BossState.Fleeing)
        {
            if (bossFightManager != null && !bossFightManager.IsEmpowered)
            {
                state = BossState.Chasing;
                InvalidatePath();
            }

            return; 
        }

        if(state == BossState.Lagging || state == BossState.Stunned)
        {
            stateTimer -= Time.fixedDeltaTime;

            if (stateTimer <= 0)
            {
                if(state == BossState.Lagging)
                {
                    currentDashDistance = 0;
                    state = BossState.Dashing;
                }

                else
                {
                    state = BossState.Chasing;
                    InvalidatePath();

                    if(bossFightManager != null && bossFightManager.IsEmpowered)
                        state = BossState.Fleeing;
                }
            }
            return; 
        }   

    }


    private bool CrossesCellDuringMove(Vector2Int fromAnchor, Vector2Int toAnchor, Vector2Int cell)
    {
        int minX = Mathf.Min(fromAnchor.x, toAnchor.x);
        int maxX = Mathf.Max(fromAnchor.x + 1, toAnchor.x + 1);
        int minY = Mathf.Min(fromAnchor.y, toAnchor.y);
        int maxY = Mathf.Max(fromAnchor.y + 1, toAnchor.y + 1);

        return cell.x >= minX && cell.x <= maxX &&
               cell.y >= minY && cell.y <= maxY;
    }



    private bool MoveBossAndCheckCollisionWithPlayer(Vector2Int nextAnchor)
    {
        if (!IsWalkable2x2(nextAnchor))
            return false;

        Vector2Int oldAnchor = AnchorCell;
        SetAnchor(nextAnchor);

        if (bossFightManager != null && player != null)
        {
            Vector2Int playerCell = WorldToGrid(player.position);
            
            bool hitPlayer = CrossesCellDuringMove(oldAnchor, nextAnchor, playerCell);

            if (hitPlayer) 
                bossFightManager.HandleCollisionBetweenSnakeAndBoss();
            
        } 

        return true;
      

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

    private bool TakeAction()
    {
        if (state == BossState.Lagging || state == BossState.Stunned)
            return false; // no movement during these

        if (state == BossState.Dashing)
        {
            return Dash();
        }

        if (state == BossState.Fleeing)
        {
            return Flee();
        }

        return Chase(); 
    }

    private bool Dash()
    {
        Vector2Int next = AnchorCell + direction * BossCellSize;

        if (!MoveBossAndCheckCollisionWithPlayer(next))
        {
            Stunned();
            return false;
        }

        currentDashDistance++;
        if (currentDashDistance >= maxDashDistance)
        {
            state = BossState.Chasing;
            InvalidatePath();
        }

        return true;
    }

    private void Stunned()
    {
        state = BossState.Stunned;
        stateTimer = stunDuration;  
        InvalidatePath();
        SpawnPowerPickup();
    }

    public void StunnedByHit()
    {
        state = BossState.Stunned;
        stateTimer = stunByHitDuration;
        InvalidatePath();
    }

    private bool Flee()
    {
        Vector2Int playerCell = WorldToGrid(player.position);
        UpdateFleePath(playerCell);
        return TryFollowPathStep();
    }

    private bool Chase()
    {
        if (bossFightManager != null && bossFightManager.IsEmpowered)
        {
            state = BossState.Fleeing;
            InvalidatePath();
            return false;
        }
        
        Vector2Int playerPos = WorldToGrid(player.position);
        //int distace = GetDistance(AnchorCell, playerPos);

        if (CanSeePlayer(playerPos, out var dashDir))
        {
            direction = dashDir; 
            Lagging();
            InvalidatePath();
            return false;
        }

        // Pathfind only to a target this 2x2 boss can actually reach.
        UpdatePathToPlayer(playerPos);
        return TryFollowPathStep();
    }

    private void Lagging()
    {
        state = BossState.Lagging;
        stateTimer = laggingDelay;
    }

    private bool IsWalkable2x2(Vector2Int anchor)
    {
        Vector2Int bottomLeft = anchor;
        Vector2Int bottomRight = anchor + Vector2Int.right;
        Vector2Int topLeft = anchor + Vector2Int.up;
        Vector2Int topRight = anchor + Vector2Int.right + Vector2Int.up;

        return mapManager.IsWalkable(bottomLeft)
            && mapManager.IsWalkable(bottomRight)
            && mapManager.IsWalkable(topLeft)
            && mapManager.IsWalkable(topRight);
    }

    private int GetDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

    private void SetAnchor(Vector2Int newAnchor)
    {
        //for(int i = bossBodies.Count - 1; i > 0 ; i--)
        //{
        //    bossBodies[i].position = bossBodies[i - 1].position;
        //}   
        Vector2Int delta = newAnchor - AnchorCell;

        if (delta == new Vector2Int(BossCellSize, 0)) 
            lastDir = Vector2Int.right;
        else if (delta == new Vector2Int(-BossCellSize, 0)) 
            lastDir = Vector2Int.left;
        else if (delta == new Vector2Int(0, BossCellSize)) 
            lastDir = Vector2Int.up;
        else if (delta == new Vector2Int(0, -BossCellSize)) 
            lastDir = Vector2Int.down;

        AnchorCell = newAnchor;
        transform.position = AnchorToWorld(newAnchor); //new Vector3(newAnchor.x + 0.5f, newAnchor.y + 0.5f, 0); 
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return mapManager.WorldToCell(worldPos);
    }

    private void RecordAnchor()
        {
            anchorHistory.Insert(0, AnchorCell);

            int requiredHistoryLength = bossBodies.Count +  5;  // extra buffer
            if(anchorHistory.Count > requiredHistoryLength)
            {
                anchorHistory.RemoveRange(requiredHistoryLength, anchorHistory.Count - requiredHistoryLength);
            }
        }

    private void UpdateBody()
    {
        for (int i = 1; i < bossBodies.Count; i++)
        {
            if (i < anchorHistory.Count)
            {
                Vector2Int a = anchorHistory[i];
                bossBodies[i].position = AnchorToWorld(a);
            }
        }
    }

    private void Restate()
    {
        AnchorCell = new Vector2Int(2, -2);
        transform.position = AnchorToWorld(AnchorCell);

        for (int i = 1; i < bossBodies.Count; i++)
        {
            if (bossBodies[i] != null)
                Destroy(bossBodies[i].gameObject);
        }

        bossBodies.Clear();
        bossBodies.Add(transform);

        anchorHistory.Clear();
        currentPath.Clear();
        pathIndex = 0;
        repathTimer = 0f;
        hasCachedPathPlayerCell = false;
        lastDir = Vector2Int.zero;
        direction = Vector2Int.right;

        for (int i = 0; i < initialBodySegments; i++)
        {
            Grow();
        }

        SeedBodyHistory();
        UpdateBody();
    }

    private void Grow()
    {
        if (bossBodyPrefab == null) return;

        Transform body = Instantiate(bossBodyPrefab);
        body.position = bossBodies[bossBodies.Count - 1].position;
        bossBodies.Add(body); 
    }

    private void SeedBodyHistory()
    {
        anchorHistory.Clear();

        for (int i = 0; i < bossBodies.Count + 5; i++)
        {
            anchorHistory.Add(AnchorCell - direction * BossCellSize * i);
        }
    }

    private void InvalidatePath()
    {
        currentPath.Clear();
        pathIndex = 0;
        repathTimer = 0f;
        hasCachedPathPlayerCell = false;
    }

    public void Die()
    {
        state = BossState.Die;
        for (int i = 0; i < bossBodies.Count; i++)
            if (bossBodies[i] != null) Destroy(bossBodies[i].gameObject);
    }

    private void SpawnPowerPickup()
    {
        if (powerUpPrefab == null)
            return;

        Vector3 spawnPos = AnchorToWorld(AnchorCell);

        GameObject powerup = Instantiate(
            powerUpPrefab,
            spawnPos,
            Quaternion.identity
        );

        var dropItem = powerup.GetComponent<BossPowerUp>();

        if (dropItem != null)
            dropItem.SetManager(bossFightManager);
    }

    private bool CanSeePlayer(Vector2Int playerPos, out Vector2Int dashDir)
    {
        dashDir = Vector2Int.zero;

        int anchorX = AnchorCell.x; 
        int anchorY = AnchorCell.y;

        bool xOverlap = (playerPos.x == anchorX || playerPos.x == anchorX + 1);
        bool yOverlap = (playerPos.y == anchorY || playerPos.y == anchorY + 1);

        if (yOverlap)
        {
            if(playerPos.x > anchorX + 1) dashDir = Vector2Int.right;
            else if(playerPos.x < anchorX) dashDir = Vector2Int.left;
        }
        else if (xOverlap)
        {
            if(playerPos.y > anchorY + 1) dashDir = Vector2Int.up;
            else if(playerPos.y < anchorY) dashDir = Vector2Int.down;
        }
        
        if(dashDir ==  Vector2Int.zero)
            return false;

        if(GetDistance(AnchorCell, playerPos) > dashTriggerRange)
            return false;

        Vector2Int step = dashDir * BossCellSize;
        Vector2Int current = AnchorCell;

        int maxSteps = Mathf.RoundToInt(dashTriggerRange / BossCellSize) + 2;

        for(int i = 0; i < maxSteps; i++)
        {
            current += step;
            if (!IsWalkable2x2(current))
                return false;

            if (current.x <= playerPos.x && playerPos.x <= current.x + 1 &&
                current.y <= playerPos.y && playerPos.y <= current.y + 1)
                return true;
        }

        return false; 
    }

    private List<Vector2Int> GetGoalAnchorCandidatesNearPlayer(Vector2Int playerCell)
    {
        return new List<Vector2Int>
        {
            new Vector2Int(playerCell.x, playerCell.y),
            new Vector2Int(playerCell.x-1, playerCell.y),
            new Vector2Int(playerCell.x, playerCell.y-1),
            new Vector2Int(playerCell.x-1, playerCell.y-1),
        };
    }

    // Ensure (candidate - AnchorCell) is divisible by 2 in both axes,
    // otherwise the boss can NEVER reach it using +/-2 steps.
    private Vector2Int ParityCheck(Vector2Int candidate)
    {
        int dx = candidate.x - AnchorCell.x;
        int dy = candidate.y - AnchorCell.y;

        if ((dx & 1) != 0) candidate.x += (dx > 0) ? 1 : -1;
        if ((dy & 1) != 0) candidate.y += (dy > 0) ? 1 : -1;

        return candidate;
    }

    private Vector3 AnchorToWorld(Vector2Int anchor)
    {
        Vector3 bottomLeft = mapManager.CellToWorld(anchor);
        Vector3 topRight = mapManager.CellToWorld(anchor + Vector2Int.right + Vector2Int.up);

        return (bottomLeft + topRight) / 2f;
    }

    private class ANode
    {
        public Vector2Int position;
        public int gCost;
        public int hCost;
        public int fCost => gCost + hCost;
        public ANode parent;

        public ANode(Vector2Int pos, int g, int h, ANode parent)
        {
            position = pos;
            this.gCost = g;
            this.hCost = h;
            this.parent = parent;
        }
    }

    private bool FindPath2x2_AStar(Vector2Int startAnchor, Vector2Int goal, List<Vector2Int> path)
    {
        path.Clear();

        if (!IsWalkable2x2(startAnchor))
            return false;
        if (!IsWalkable2x2(goal)) return false;

        List<ANode> openList = new();
        Dictionary<Vector2Int, ANode> allNodes = new();
        HashSet<Vector2Int> closedSet = new();

        ANode start = new(startAnchor, 0, GetDistance(startAnchor, goal), null); 
        openList.Add(start);
        allNodes[startAnchor] = start;

        int visitedNodes = 0;   

        while (openList.Count > 0)
        {
            ANode currentNode = openList[0]; 
            for(int i = 1 ; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost || 
                    (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }
            openList.Remove(currentNode);
            allNodes.Remove(currentNode.position);
            closedSet.Add(currentNode.position);

            visitedNodes++;
            if (visitedNodes > maxVisitedNodes) return false;

            //int stopDistance = 4; 
            //goal is to get close enough to player so that the boss can dash
            if (currentNode.position == goal)  /*dashTriggerRange*/
            {
                RetracePath(currentNode, path);
                return path.Count > 0;
            }


            foreach(var dir in directions)
            {
                Vector2Int neighborPos = currentNode.position + dir * BossCellSize;
                if (closedSet.Contains(neighborPos) || !IsWalkable2x2(neighborPos))
                    continue;

                int gCost = currentNode.gCost + BossCellSize;
                int hCost = GetDistance(neighborPos, goal);
                if (allNodes.TryGetValue(neighborPos, out ANode existingNode))
                {
                    if (gCost < existingNode.gCost)
                    {
                        existingNode.gCost = gCost;
                        existingNode.parent = currentNode;
                    }
                }
                else
                {
                    ANode newNode = new(neighborPos, gCost, hCost, currentNode);
                    openList.Add(newNode);
                    allNodes[neighborPos] = newNode;
                }
            }
        }

        return false;
    }

    private void RetracePath(ANode endNode, List<Vector2Int> path)
    {
        path.Clear();
        ANode current = endNode;
        while(current != null)
        {
            path.Add(current.position);
            current = current.parent;
        }
        path.Reverse();
    }

    private void UpdatePathToPlayer(Vector2Int playerCell)
    {
        bool playerCellChanged = !hasCachedPathPlayerCell || cachedPathPlayerCell != playerCell;
        bool pathCanWait = repathTimer > 0f && !playerCellChanged;

        if (pathCanWait)
            return;

        currentPath.Clear();
        pathIndex = 0;
        cachedPathPlayerCell = playerCell;
        hasCachedPathPlayerCell = true;
        repathTimer = repathInterval;

        if (TryBuildPathToPlayer(playerCell, currentPath))
            pathIndex = 1; // next step
    }

    private void UpdateFleePath(Vector2Int playerCell)
    {
        bool playerCellChanged = !hasCachedPathPlayerCell || cachedPathPlayerCell != playerCell;
        bool pathCanWait = repathTimer > 0f && !playerCellChanged;

        if (pathCanWait)
            return;

        currentPath.Clear();
        pathIndex = 0;
        cachedPathPlayerCell = playerCell;
        hasCachedPathPlayerCell = true;
        repathTimer = repathInterval;

        if (FindPathToFarthestReachableAnchor(playerCell, currentPath))
            pathIndex = 1;
    }

    private bool TryBuildPathToPlayer(Vector2Int playerCell, List<Vector2Int> path)
    {
        path.Clear();

        List<Vector2Int> bestPath = null;
        int bestDistanceToPlayer = int.MaxValue;
        int bestPathLength = int.MaxValue;
        HashSet<Vector2Int> checkedCandidates = new();

        foreach (Vector2Int candidate in GetGoalAnchorCandidatesNearPlayer(playerCell))
        {
            Vector2Int goalAnchor = ParityCheck(candidate);
            if (!checkedCandidates.Add(goalAnchor) || !IsWalkable2x2(goalAnchor))
                continue;

            List<Vector2Int> candidatePath = new();
            if (!FindPath2x2_AStar(AnchorCell, goalAnchor, candidatePath))
                continue;

            int distanceToPlayer = GetDistance(goalAnchor, playerCell);
            if (distanceToPlayer < bestDistanceToPlayer ||
                (distanceToPlayer == bestDistanceToPlayer && candidatePath.Count < bestPathLength))
            {
                bestDistanceToPlayer = distanceToPlayer;
                bestPathLength = candidatePath.Count;
                bestPath = candidatePath;
            }
        }

        if (bestPath != null)
        {
            path.AddRange(bestPath);
            return true;
        }

        return FindPathToClosestReachableAnchor(playerCell, path);
    }

    private bool FindPathToClosestReachableAnchor(Vector2Int targetCell, List<Vector2Int> path)
    {
        path.Clear();

        if (!IsWalkable2x2(AnchorCell))
            return false;

        Queue<Vector2Int> frontier = new();
        HashSet<Vector2Int> visited = new();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();

        frontier.Enqueue(AnchorCell);
        visited.Add(AnchorCell);

        Vector2Int bestAnchor = AnchorCell;
        int bestDistance = GetDistance(AnchorCell, targetCell);
        int visitedNodes = 0;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            visitedNodes++;

            if (visitedNodes > maxVisitedNodes)
                break;

            int distance = GetDistance(current, targetCell);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestAnchor = current;
            }

            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir * BossCellSize;
                if (visited.Contains(next) || !IsWalkable2x2(next))
                    continue;

                visited.Add(next);
                cameFrom[next] = current;
                frontier.Enqueue(next);
            }
        }

        Vector2Int step = bestAnchor;
        path.Add(step);

        while (step != AnchorCell)
        {
            step = cameFrom[step];
            path.Add(step);
        }

        path.Reverse();
        return path.Count > 0;
    }

    private bool FindPathToFarthestReachableAnchor(Vector2Int targetCell, List<Vector2Int> path)
    {
        path.Clear();

        if (!IsWalkable2x2(AnchorCell))
            return false;

        Queue<Vector2Int> frontier = new();
        HashSet<Vector2Int> visited = new();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Dictionary<Vector2Int, int> pathLength = new();

        frontier.Enqueue(AnchorCell);
        visited.Add(AnchorCell);
        pathLength[AnchorCell] = 0;

        Vector2Int bestAnchor = AnchorCell;
        int bestDistance = GetDistance(AnchorCell, targetCell);
        int bestPathLength = 0;
        int visitedNodes = 0;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            visitedNodes++;

            if (visitedNodes > maxVisitedNodes)
                break;

            int distance = GetDistance(current, targetCell);
            int currentPathLength = pathLength[current];
            if (distance > bestDistance ||
                (distance == bestDistance && currentPathLength > bestPathLength))
            {
                bestDistance = distance;
                bestPathLength = currentPathLength;
                bestAnchor = current;
            }

            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir * BossCellSize;
                if (visited.Contains(next) || !IsWalkable2x2(next))
                    continue;

                visited.Add(next);
                cameFrom[next] = current;
                pathLength[next] = currentPathLength + 1;
                frontier.Enqueue(next);
            }
        }

        Vector2Int step = bestAnchor;
        path.Add(step);

        while (step != AnchorCell)
        {
            step = cameFrom[step];
            path.Add(step);
        }

        path.Reverse();
        return path.Count > 0;
    }

    private bool TryFollowPathStep()
    {
        if (currentPath.Count == 0) return false;
        if (pathIndex >= currentPath.Count) return false;

        Vector2Int nextAnchor = currentPath[pathIndex];

        // derive direction from anchors
        Vector2Int delta = nextAnchor - AnchorCell;

        // delta should be (+/-2,0) or (0,+/-2)
        if (delta.x > 0) direction = Vector2Int.right;
        else if (delta.x < 0) direction = Vector2Int.left;
        else if (delta.y > 0) direction = Vector2Int.up;
        else if (delta.y < 0) direction = Vector2Int.down;

        // move now (your existing mover)
        if (!MoveBossAndCheckCollisionWithPlayer(nextAnchor))
        {
            InvalidatePath();
            return false;
        }

        pathIndex++;
        return true;
    }
}


