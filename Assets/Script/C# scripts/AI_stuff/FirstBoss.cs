using System.Collections.Generic;
using UnityEngine;

public class FirstBoss : MonoBehaviour
{
    public enum BossState
    {
        Chasing,
        Lagging, //WaitingToDash,
        Dashing,
        Stunned,
        Fleeing,
        Dead,
    }

    [Header("References")]
    [SerializeField] MapManager mapManager;
    [SerializeField] Transform player;
    [SerializeField] Transform bossBodyPrefab;
    [SerializeField] BossFightManager bossFightManager;
    [SerializeField] GameObject powerUpPrefab;

    [Header("Speed Stats")]
    private float chaseSpeed = 7.5f;          // steps/sec (1 step = a movement of 2 cells of the boss)
    private float fleeSpeed = 10f;
    private float dashSpeed = 22f;            // steps/sec during dash

    [Header("Dash")]
    private float dashTriggerRange = 10f;
    private int maxDashSteps = 8;
    private float waitingTimeToDash = 0.2f;
    private int currentDashSteps;

    [Header("Stun")]
    private float stunByWallsDuration = 3f;
    private float stunByPlayerHitDuration = 5f;     // stun time when player hits boss (e.g. with empowered bite)

    [Header("Flee")]
    [SerializeField] private int fleeSafeDistance = 12;
    [SerializeField] private int fleePanicDistance = 8;
    private int maxVisitedNodes = 20000;
    private bool hasFleeTarget;
    private Vector2Int fleeTargetAnchor;
    private int fleeTargetDistanceFloor;  //fleeDistanceLimit

    [Header("Debug")]
    [SerializeField] private bool drawBossGizmos = true;
    [SerializeField] private bool drawPath = true;
    [SerializeField] private bool drawFleeChoice = true;

    [Header("Boss Settings")]
    [SerializeField] Vector2Int startAnchor;
    private int initialBodySegments = 3;
    private const int BossCellSize = 2;
    public BossState state = BossState.Chasing;
    public bool IsDangerous => state != BossState.Dead && state != BossState.Stunned;

    private Vector2Int bossAnchor;
    private Vector2Int moveDir = Vector2Int.right;
    private Vector2Int lastDir = Vector2Int.zero;

    private List<Transform> bossBodies = new();
    private List<Vector2Int> anchorHistory = new();

    private float moveTimer;
    private float stateTimer;

    private bool debugHasFleeChoice;
    private Vector2Int debugLastFleeBestAnchor;
    private Vector2Int debugLastFleeNextAnchor;

    Vector2Int[] moveDirs = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

    // pathfiding variables
    private float repathInterval = 0.12f;
    private float repathTimer;

    private readonly List<Vector2Int> currentPath = new();
    private int pathIndex;

    private Vector2Int cachedPlayerCell;
    private bool hasCachedPlayerCell;

    private void Start()
    {
        Restate();
    }

    private void Update()
    {
        if (state == BossState.Dead)
            return;

        if (Input.GetKeyDown(KeyCode.K))
            SpawnPowerPickup();
    }

    private void FixedUpdate()
    {
        if (state == BossState.Dead)
            return;

        UpdateBossState();

        if (state == BossState.Chasing)
            repathTimer -= Time.fixedDeltaTime;

        if (state == BossState.Lagging || state == BossState.Stunned)
            return;

        float stepInterval = 1f / GetCurrentSpeed();

        moveTimer += Time.fixedDeltaTime;
        if (moveTimer >= stepInterval)
        {
            moveTimer -= stepInterval;
            bool moved = MakeMovement();

            if (moved)
            {
                RecordAnchor();
                UpdateBody();
            }

            UpdateHeadRotation();
        }
    }

    private void UpdateBossState()
    {
        if (state == BossState.Fleeing)
        {
            if (bossFightManager != null && !bossFightManager.IsEmpowered)
            {
                state = BossState.Chasing;
                ClearPath();
            }

            return;
        }

        if (state == BossState.Lagging || state == BossState.Stunned)
        {
            stateTimer -= Time.fixedDeltaTime;

            if (stateTimer <= 0)
            {
                if (state == BossState.Lagging)
                {
                    currentDashSteps = 0;
                    state = BossState.Dashing;
                    AudioManager.Instance?.playSFX(AudioManager.Instance.bossDash);
                }

                else
                {
                    state = bossFightManager != null && bossFightManager.IsEmpowered
                        ? BossState.Fleeing : BossState.Chasing;

                    ClearPath();
                }
            }
            return;
        }
    }
    private bool MakeMovement()
    {
        if (state == BossState.Dashing)
        {
            return Dash();
        }

        if (state == BossState.Fleeing)
            return Flee();

        return Chase();
    }


    #region Chase
    private bool Chase()
    {
        if (bossFightManager != null && bossFightManager.IsEmpowered)
        {
            state = BossState.Fleeing;
            ClearPath();
            return false;
        }

        Vector2Int playerCell = WorldToGrid(player.position);
        //int distace = GetDistance(bossAnchor, playerPos);

        if (CanDashAtPlayer(playerCell, out Vector2Int dashDir))
        {
            moveDir = dashDir;
            Lagging();
            ClearPath();
            return false;
        }

        // Pathfind only to a target this 2x2 boss can actually reach.
        UpdateChasePath(playerCell);
        return FollowPathStep();
    }
    private void RetracePath(ANode endNode, List<Vector2Int> path)
    {
        path.Clear();
        ANode current = endNode;
        while (current != null)
        {
            path.Add(current.position);
            current = current.parent;
        }
        path.Reverse();
    }

    private void UpdateChasePath(Vector2Int playerCell)
    {
        bool playerCellChanged = !hasCachedPlayerCell || cachedPlayerCell != playerCell;

        if (repathTimer > 0f && !playerCellChanged)
            return;

        currentPath.Clear();
        pathIndex = 0;

        cachedPlayerCell = playerCell;
        hasCachedPlayerCell = true;
        repathTimer = repathInterval;

        if (BuildChasePath(playerCell, currentPath))
            pathIndex = 1; // next step
    }

    private bool BuildChasePath(Vector2Int playerCell, List<Vector2Int> resultPath)
    {
        resultPath.Clear();

        List<Vector2Int> bestPath = null;
        int bestDistanceToPlayer = int.MaxValue;
        int bestPathLength = int.MaxValue;
        HashSet<Vector2Int> checkedCandidates = new();

        foreach (Vector2Int candidate in GetAnchorNearPlayer(playerCell))
        {
            Vector2Int goalAnchor = ParityCheck(candidate);
            if (!checkedCandidates.Add(goalAnchor) || !IsWalkable2x2(goalAnchor))
                continue;

            List<Vector2Int> candidatePath = new();
            if (!FindPath2x2_AStar(bossAnchor, goalAnchor, candidatePath))
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
            resultPath.AddRange(bestPath);
            return true;
        }

        return FindPathToClosestReachableAnchor(playerCell, resultPath);
    }

    private bool FindPathToClosestReachableAnchor(Vector2Int targetCell, List<Vector2Int> path)
    {
        path.Clear();

        if (!IsWalkable2x2(bossAnchor))
            return false;

        Queue<Vector2Int> frontier = new();
        HashSet<Vector2Int> visited = new();
        Dictionary<Vector2Int, Vector2Int> parentAnchor = new();

        frontier.Enqueue(bossAnchor);
        visited.Add(bossAnchor);

        Vector2Int bestAnchor = bossAnchor;
        int bestDistance = GetDistance(bossAnchor, targetCell);
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

            foreach (Vector2Int dir in moveDirs)
            {
                Vector2Int next = current + dir * BossCellSize;
                if (visited.Contains(next) || !IsWalkable2x2(next))
                    continue;

                visited.Add(next);
                parentAnchor[next] = current;
                frontier.Enqueue(next);
            }
        }

        Vector2Int step = bestAnchor;
        path.Add(step);

        while (step != bossAnchor)
        {
            step = parentAnchor[step];
            path.Add(step);
        }

        path.Reverse();
        return path.Count > 0;
    }

    private bool FollowPathStep()
    {
        if (currentPath.Count == 0 || pathIndex >= currentPath.Count) return false;

        Vector2Int nextAnchor = currentPath[pathIndex];
        SetMoveDirection(nextAnchor);

        // move now (your existing mover)
        if (!MoveBossAnchorAndCheckCollisionWithPlayer(nextAnchor))
        {
            ClearPath();
            return false;
        }

        pathIndex++;
        return true;
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
            for (int i = 1; i < openList.Count; i++)
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


            foreach (var dir in moveDirs)
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
    #endregion



    #region Flee  
    private bool Flee()
    {
        Vector2Int playerCell = WorldToGrid(player.position);
        return FleeFromPlayer(playerCell);
    }

    private bool FleeFromPlayer(Vector2Int playerCell)
    {
        if (NeedsNewFleePath(playerCell) && !ChooseRandomFleePath(playerCell))
            return false;

        return FollowFleePath(playerCell);
    }

    private bool NeedsNewFleePath(Vector2Int playerCell)
    {
        if (!hasFleeTarget || currentPath.Count == 0 || pathIndex >= currentPath.Count)
            return true;

        if (pathIndex > 0 && currentPath[pathIndex - 1] != bossAnchor)
            return true;

        Vector2Int nextAnchor = currentPath[pathIndex];

        if (GetDistance(fleeTargetAnchor, playerCell) < fleeTargetDistanceFloor)
            return true;

        if (PlayerIsTooClose(playerCell) && !StepMovesAwayFromPlayer(nextAnchor, playerCell))
            return true;

        return !IsWalkable2x2(nextAnchor) || CrossesOthersDuringMove(bossAnchor, nextAnchor, playerCell);
    }

    private Vector2Int PickRandomFleeTarget(List<Vector2Int> choices, Dictionary<Vector2Int, Vector2Int> parentAnchor, Vector2Int playerCell)
    {
        List<Vector2Int> closePlayerChoices = new();
        List<Vector2Int> nonReverseChoices = new();
        Vector2Int previousAnchor = bossAnchor - lastDir * BossCellSize;
        bool playerTooClose = PlayerIsTooClose(playerCell);

        foreach (Vector2Int choice in choices)
        {
            //take the first step of each choices to evaluate if that step is non reverse, or go far away from the player
            Vector2Int firstStepOfThePath = TraceFleePath(choice, parentAnchor);                                  
            bool reversesLastMove = lastDir != Vector2Int.zero && firstStepOfThePath == previousAnchor;

            if (!reversesLastMove)
                nonReverseChoices.Add(choice);

            if (playerTooClose && !reversesLastMove && StepMovesAwayFromPlayer(firstStepOfThePath, playerCell))
                closePlayerChoices.Add(choice);
        }

        if (closePlayerChoices.Count > 0)
            return closePlayerChoices[Random.Range(0, closePlayerChoices.Count)];

        List<Vector2Int> targetChoices = nonReverseChoices.Count > 0 ? nonReverseChoices : choices;
        return targetChoices[Random.Range(0, targetChoices.Count)];
    }

    private bool ChooseRandomFleePath(Vector2Int playerCell)
    {
        if (!IsWalkable2x2(bossAnchor))
            return false;

        ClearPath();

        Queue<Vector2Int> frontier = new();
        HashSet<Vector2Int> visited = new();
        Dictionary<Vector2Int, Vector2Int> parentAnchor = new();
        List<Vector2Int> safeAnchors = new();
        List<Vector2Int> farthestAnchors = new();
        int farthestDistance = int.MinValue;

        frontier.Enqueue(bossAnchor);
        visited.Add(bossAnchor);
        int visitedNodes = 0;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            visitedNodes++;

            if (visitedNodes > maxVisitedNodes)
                break;

            if (current != bossAnchor)
            {
                int distance = GetDistance(current, playerCell);

                if (distance >= fleeSafeDistance)
                    safeAnchors.Add(current);

                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthestAnchors.Clear();
                    farthestAnchors.Add(current);
                }
                else if (distance == farthestDistance)
                {
                    farthestAnchors.Add(current);
                }
            }

            foreach (Vector2Int dir in moveDirs)
            {
                Vector2Int next = current + dir * BossCellSize;
                if (visited.Contains(next) ||
                    !IsWalkable2x2(next) ||
                    CrossesOthersDuringMove(current, next, playerCell))
                {
                    continue;
                }

                visited.Add(next);
                parentAnchor[next] = current;
                frontier.Enqueue(next);
            }
        }

        bool hasSafeChoices = safeAnchors.Count > 0;
        List<Vector2Int> choices = hasSafeChoices ? safeAnchors : farthestAnchors;
        if (choices.Count == 0)
            return false;

        Vector2Int targetAnchor = PickRandomFleeTarget(choices, parentAnchor, playerCell);
        TraceFleePath(targetAnchor, parentAnchor, currentPath);

        if (currentPath.Count < 2)
            return false;

        fleeTargetAnchor = targetAnchor;
        fleeTargetDistanceFloor = hasSafeChoices
            ? fleeSafeDistance
            : Mathf.Max(0, GetDistance(targetAnchor, playerCell) - BossCellSize);
        hasFleeTarget = true;
        pathIndex = 1;
        debugHasFleeChoice = true;
        debugLastFleeBestAnchor = fleeTargetAnchor;
        debugLastFleeNextAnchor = currentPath[pathIndex];

        return true;
    }

    


    private Vector2Int TraceFleePath(Vector2Int targetAnchor, Dictionary<Vector2Int, Vector2Int> parentAnchor, List<Vector2Int> resultPath = null)
    {
        resultPath?.Clear();

        Vector2Int currentStep = targetAnchor;         
        Vector2Int step = targetAnchor;                

        resultPath?.Add(currentStep);

        while (currentStep != bossAnchor && parentAnchor.TryGetValue(currentStep, out Vector2Int previous))
        {
            step = currentStep;
            currentStep = previous; 
            resultPath?.Add(currentStep);
        }

        resultPath?.Reverse();

        //if there was no path passed as an argument, return a first step only 
        return step;               
    }

    private bool FollowFleePath(Vector2Int playerCell)
    {
        if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
            return false;

        Vector2Int nextAnchor = currentPath[pathIndex];
        if (CrossesOthersDuringMove(bossAnchor, nextAnchor, playerCell))
        {
            ClearPath();
            return false;
        }

        SetMoveDirection(nextAnchor);

        if (!MoveBossAnchorAndCheckCollisionWithPlayer(nextAnchor))
        {
            ClearPath();
            return false;
        }

        pathIndex++;
        debugLastFleeBestAnchor = fleeTargetAnchor;
        debugLastFleeNextAnchor = pathIndex < currentPath.Count ? currentPath[pathIndex] : fleeTargetAnchor;

        return true;
    }
    #endregion



    private void Lagging()
    {
        state = BossState.Lagging;
        stateTimer = waitingTimeToDash;
    }

    private void UpdateHeadRotation()
    {
        float angle = 0f;
        if (moveDir == Vector2Int.up)
            angle = 90f;
        else if (moveDir == Vector2Int.down)
            angle = -90f;
        else if (moveDir == Vector2Int.left)
            angle = 180f;
        else if (moveDir == Vector2Int.right)
            angle = 0f;

        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private bool Dash()
    {
        Vector2Int next = bossAnchor + moveDir * BossCellSize;

        if (!MoveBossAnchorAndCheckCollisionWithPlayer(next))
        {
            Stunned();
            return false;
        }

        currentDashSteps++;
        //AudioManager.Instance?.playSFX(AudioManager.Instance.bossDash);
        if (currentDashSteps >= maxDashSteps)
        {
            state = BossState.Chasing;
            ClearPath();
        }

        return true;
    }

    private void Stunned()
    {
        state = BossState.Stunned;
        stateTimer = stunByWallsDuration;
        ClearPath();
        SpawnPowerPickup();
    }

    public void StunnedByHit()
    {
        state = BossState.Stunned;
        stateTimer = stunByPlayerHitDuration;
        ClearPath();
    }

    private void RecordAnchor()
    {
        anchorHistory.Insert(0, bossAnchor);

        int requiredHistoryLength = bossBodies.Count + 5;  // extra buffer
        if (anchorHistory.Count > requiredHistoryLength)
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

    public void Restate()
    {
        bossAnchor = startAnchor;
        transform.position = AnchorToWorld(bossAnchor);

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
        hasCachedPlayerCell = false;
        hasFleeTarget = false;
        fleeTargetDistanceFloor = 0;
        debugHasFleeChoice = false;
        lastDir = Vector2Int.zero;
        moveDir = Vector2Int.right;

        for (int i = 0; i < initialBodySegments; i++)
        {
            Grow();
        }

        SaveBodyHistory();
        UpdateBody();
    }

    private void Grow()
    {
        if (bossBodyPrefab == null) return;

        Transform body = Instantiate(bossBodyPrefab);
        body.position = bossBodies[bossBodies.Count - 1].position;
        bossBodies.Add(body);
    }

    private void SaveBodyHistory()
    {
        anchorHistory.Clear();

        for (int i = 0; i < bossBodies.Count; i++)
        {
            anchorHistory.Add(bossAnchor - moveDir * BossCellSize * i);
        }
    }

    public void Dead()
    {
        state = BossState.Dead;
        for (int i = 0; i < bossBodies.Count; i++)
            if (bossBodies[i] != null) Destroy(bossBodies[i].gameObject);
    }

    private void SpawnPowerPickup()
    {
        if (powerUpPrefab == null)
            return;

        Vector3 spawnPos = new Vector3(bossAnchor.x, bossAnchor.y, 0);

        bossFightManager.SpawnPowerUp(spawnPos);
    }

    private bool CanDashAtPlayer(Vector2Int playerPos, out Vector2Int dashDir)
    {
        dashDir = Vector2Int.zero;

        bool xOverlap = (playerPos.x == bossAnchor.x || playerPos.x == bossAnchor.x + 1);            // Check if the player's x-coordinate overlaps with the boss's x-coordinate range
        bool yOverlap = (playerPos.y == bossAnchor.y || playerPos.y == bossAnchor.y + 1);            // Check if the player's y-coordinate overlaps with the boss's y-coordinate range

        if (yOverlap)
        {
            if (playerPos.x > bossAnchor.x + 1) dashDir = Vector2Int.right;
            else if (playerPos.x < bossAnchor.x) dashDir = Vector2Int.left;
        }
        else if (xOverlap)
        {
            if (playerPos.y > bossAnchor.y + 1) dashDir = Vector2Int.up;
            else if (playerPos.y < bossAnchor.y) dashDir = Vector2Int.down;
        }

        if (dashDir == Vector2Int.zero)
            return false;

        if (GetDistance(bossAnchor, playerPos) > dashTriggerRange)
            return false;

        Vector2Int step = dashDir * BossCellSize;
        Vector2Int current = bossAnchor;

        int maxEstimateStepsToPlayer = Mathf.RoundToInt(dashTriggerRange / BossCellSize);             // Estimate the maximum number of steps to reach the player based on the dash trigger range

        for (int i = 0; i < maxEstimateStepsToPlayer; i++)
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

    private List<Vector2Int> GetAnchorNearPlayer(Vector2Int playerCell)
    {
        return new List<Vector2Int>
        {
            new Vector2Int(playerCell.x, playerCell.y),
            new Vector2Int(playerCell.x-1, playerCell.y),
            new Vector2Int(playerCell.x, playerCell.y-1),
            new Vector2Int(playerCell.x-1, playerCell.y-1),
        };
    }

    private bool StepMovesAwayFromPlayer(Vector2Int nextAnchor, Vector2Int playerCell)
    {
        return GetDistance(nextAnchor, playerCell) > GetDistance(bossAnchor, playerCell);
    }

    private void SetMoveDirection(Vector2Int nextAnchor)
    {
        Vector2Int delta = nextAnchor - bossAnchor;

        if (delta.x > 0) moveDir = Vector2Int.right;
        else if (delta.x < 0) moveDir = Vector2Int.left;
        else if (delta.y > 0) moveDir = Vector2Int.up;
        else if (delta.y < 0) moveDir = Vector2Int.down;
    }

    private void ClearPath()
    {
        currentPath.Clear();
        pathIndex = 0;
        hasFleeTarget = false;
        fleeTargetDistanceFloor = 0;
        repathTimer = 0f;
        hasCachedPlayerCell = false;
        debugHasFleeChoice = false;
    }



    #region Helpers
    private bool CrossesOthersDuringMove(Vector2Int fromAnchor, Vector2Int toAnchor, Vector2Int others)
    {
        int minX = Mathf.Min(fromAnchor.x, toAnchor.x);
        int maxX = Mathf.Max(fromAnchor.x + 1, toAnchor.x + 1);
        int minY = Mathf.Min(fromAnchor.y, toAnchor.y);
        int maxY = Mathf.Max(fromAnchor.y + 1, toAnchor.y + 1);

        return others.x >= minX && others.x <= maxX &&
               others.y >= minY && others.y <= maxY;
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

    private bool MoveBossAnchorAndCheckCollisionWithPlayer(Vector2Int nextAnchor)
    {

        if (!IsWalkable2x2(nextAnchor))
            return false;

        Vector2Int oldAnchor = bossAnchor;
        SetAnchor(nextAnchor);

        if (bossFightManager != null && player != null)
        {
            Vector2Int playerCell = WorldToGrid(player.position);

            bool hitPlayer = CrossesOthersDuringMove(oldAnchor, nextAnchor, playerCell);

            if (hitPlayer)
                bossFightManager.HandleCollisionBetweenSnakeAndBoss();

        }

        return true;
    }

    private int GetDistance(Vector2Int a, Vector2Int b)          //Mahattan distance heuristic for A* pathfinding
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return mapManager.WorldToCell(worldPos);
    }

    private Vector3 AnchorToWorld(Vector2Int anchor)
    {
        Vector3 bottomLeft = mapManager.CellToWorld(anchor);
        Vector3 topRight = mapManager.CellToWorld(anchor + Vector2Int.right + Vector2Int.up);

        return (bottomLeft + topRight) / 2f;
    }

    private bool PlayerIsTooClose(Vector2Int playerCell)
    {
        return GetDistance(bossAnchor, playerCell) <= fleePanicDistance;
    }

    private void SetAnchor(Vector2Int newAnchor)
    {
        //for(int i = bossBodies.Count - 1; i > 0 ; i--)
        //{
        //    bossBodies[i].position = bossBodies[i - 1].position;
        //}   
        Vector2Int delta = newAnchor - bossAnchor;

        if (delta == new Vector2Int(BossCellSize, 0))
            lastDir = Vector2Int.right;
        else if (delta == new Vector2Int(-BossCellSize, 0))
            lastDir = Vector2Int.left;
        else if (delta == new Vector2Int(0, BossCellSize))
            lastDir = Vector2Int.up;
        else if (delta == new Vector2Int(0, -BossCellSize))
            lastDir = Vector2Int.down;

        bossAnchor = newAnchor;
        transform.position = AnchorToWorld(newAnchor); //new Vector3(newAnchor.x + 0.5f, newAnchor.y + 0.5f, 0); 
    }

    private Vector2Int ParityCheck(Vector2Int candidate)
    {
        int dx = candidate.x - bossAnchor.x;
        int dy = candidate.y - bossAnchor.y;

        if ((dx & 1) != 0) candidate.x += (dx > 0) ? 1 : -1;
        if ((dy & 1) != 0) candidate.y += (dy > 0) ? 1 : -1;

        return candidate;
    }
    private float GetCurrentSpeed()
    {
        if (state == BossState.Dashing)
            return dashSpeed;
        if (state == BossState.Fleeing)
            return fleeSpeed;

        return chaseSpeed;
    }
    #endregion



    private void OnDrawGizmos()
    {
        if (!drawBossGizmos || !Application.isPlaying || mapManager == null)
            return;

        Vector3 bossWorld = AnchorToWorld(bossAnchor);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bossWorld, Vector3.one * BossCellSize);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(bossWorld, AnchorToWorld(bossAnchor + moveDir * BossCellSize));

        if (player != null)
        {
            Vector2Int playerCell = WorldToGrid(player.position);
            Gizmos.color = CanDashAtPlayer(playerCell, out _) ? Color.green : Color.red;
            Gizmos.DrawLine(bossWorld, player.position);
            Gizmos.DrawWireCube(mapManager.CellToWorld(playerCell), Vector3.one);
        }

        if (drawPath && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;

            Vector3 previous = bossWorld;
            for (int i = pathIndex; i < currentPath.Count; i++)
            {
                Vector3 next = AnchorToWorld(currentPath[i]);
                Gizmos.DrawLine(previous, next);
                Gizmos.DrawSphere(next, 0.12f);
                previous = next;
            }
        }

        if (drawFleeChoice && debugHasFleeChoice)
        {
            Gizmos.color = Color.magenta;
            Vector3 nextStep = AnchorToWorld(debugLastFleeNextAnchor);
            Vector3 bestAnchor = AnchorToWorld(debugLastFleeBestAnchor);
            Gizmos.DrawLine(bossWorld, nextStep);
            Gizmos.DrawSphere(nextStep, 0.18f);
            Gizmos.DrawWireCube(bestAnchor, Vector3.one * BossCellSize);
        }
    }
}


