using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using UnityEditor;
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

    [Header("Mape Settings")]
    [SerializeField] BoxCollider2D gridArea;
    [SerializeField] LayerMask obstacleLayer;

    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] Transform bossBodyPrefab; 
    [SerializeField] BossFightManager bossFightManager;

    [Header("Boss Settings")]
    int initialBodySegments = 3;   
    private float moveSpeed = 10f;          // steps/sec
    private float dashSpeed = 22f;          // steps/sec during dash
    private float dashTriggerRange = 10f;
    private int currentDashDistance; 
    private int maxDashDistance = 8; 
    private float laggingDelay = 0.2f;       // seconds between steps when lagging
    private float stunDuration = 3f;

    [Header("Power-up Drop")]
    [SerializeField] GameObject powerUpPrefab;
    //private float fleeDuration = 5f;      //flee time when player is empowered

    public BossState state = BossState.Chasing;

    public Vector2Int AnchorCell { get; private set; }

    private List<Transform> bossBodies = new();
    const int BossCellSize = 2;
    private List<Vector2Int> anchorHistory = new();    

    private float moveTimer;
    private float stateTimer;

    private Vector2Int direction = Vector2Int.right; 

    private int minX, maxX, minY, maxY;

    Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

    // pathfiding variables
    private float repathInterval = 0.2f;
    private int maxVisitedNodes = 20000;
    private float repathTimer;
    private readonly List<Vector2Int> currentPath = new();
    private int pathIndex;

    private Vector2Int lastDir = Vector2Int.zero;

    private void Start()
    {
        Bounds bound = gridArea.bounds;
        //minX = Mathf.RoundToInt(bound.min.x);
        //maxX = Mathf.RoundToInt(bound.max.x);
        //minY = Mathf.RoundToInt(bound.min.y);
        //maxY = Mathf.RoundToInt(bound.max.y);
        minX = Mathf.FloorToInt(bound.min.x);
        maxX = Mathf.CeilToInt(bound.max.x);
        minY = Mathf.FloorToInt(bound.min.y);
        maxY = Mathf.CeilToInt(bound.max.y);

        Debug.Log($"Boss movement bounds: minX={minX}, maxX={maxX}, minY={minY}, maxY={maxY}");

        Restate();
        Debug.Log($"Boss start anchor={AnchorCell} walkable={IsWalkable2x2(AnchorCell)}");
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

        if(state == BossState.Lagging || state == BossState.Stunned)
            return;

        float currentSpeed = (state == BossState.Dashing) ? dashSpeed : moveSpeed;
        float interval = 1f / currentSpeed; 


        moveTimer += Time.fixedDeltaTime;
        if (moveTimer >= interval) 
        {
            moveTimer -= interval;
            MakeMovement(); 
            RecordAnchor();
            UpdateBody();
            UpdateHeadRotation();
        }


    }

    private void UpdateState()
    {
        if(state == BossState.Fleeing)
        {
            if (bossFightManager != null && !bossFightManager.IsEmpowered)
                state = BossState.Chasing;

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

                    if(bossFightManager != null && bossFightManager.IsEmpowered)
                        state = BossState.Fleeing;
                }
            }
            return; 
        }   

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

    private void MakeMovement()
    {
        if (state == BossState.Lagging || state == BossState.Stunned)
            return; // no movement during these

        if (state == BossState.Dashing)
        {
            Dash();
            return;
        }

        if (state == BossState.Fleeing)
        {
            Flee();
            return;
        }

        Chase(); 
    }

    private void Dash()
    {
        currentDashDistance++;  
    
        Vector2Int next = AnchorCell + direction * BossCellSize;

        if (!IsWalkable2x2(next))
        {
            Stunned();
            return;
        }

        SetAnchor(next);

        if (currentDashDistance >= maxDashDistance)
        {
            state = BossState.Chasing;
        }
    }

    private void Stunned()
    {
        state = BossState.Stunned;
        stateTimer = stunDuration;  

        SpawnPowerPickup();
    }

    private void Flee()
    {

        Vector2Int playerCell = WorldToGrid(player.position);
        direction = ChooseBestWayToRunAway(playerCell);
        TryToMove(direction);
    }

    private void Chase()
    {
        if (bossFightManager != null && bossFightManager.IsEmpowered)
        {
            state = BossState.Fleeing;
            return;
        }
        
        Vector2Int playerPos = WorldToGrid(player.position);
        //int distace = GetDistance(AnchorCell, playerPos);

        if (CanSeePlayer(playerPos, out var dashDir))
        {
            direction = dashDir; //ChooseBestWayTowards(playerPos);
            Lagging();
            return;
        }

        // Pathfind to a REACHABLE anchor near player (fixes “unreachable goal” oscillation)
        Vector2Int goalAnchor = GetBestReachableGoalAnchorNearPlayer(playerPos);

        UpdatePathToPlayer(goalAnchor, Time.fixedDeltaTime);

        if (!TryFollowPathStep())
        {
            Vector2Int anyway = ChooseBestWayTowards(goalAnchor);
            TryToMove(anyway);
        }
    }

    private void TryToMove(Vector2Int dir)
    {
        Vector2Int next = AnchorCell + dir * BossCellSize;

        if (IsWalkable2x2(next))
        {
            direction = dir; // update direction
            SetAnchor(next);
            return;
        }

        Vector2Int otherWay = ChooseAnyWalkableWay();
        if (otherWay != Vector2Int.zero && IsWalkable2x2(AnchorCell + otherWay * BossCellSize))
        {
            direction = otherWay;
            SetAnchor(AnchorCell + otherWay * BossCellSize); 
        }
    }

    private Vector2Int ChooseAnyWalkableWay()
    {
        Vector2Int reverse = -lastDir;

        foreach (var dir in directions)
        {
            if (dir == reverse) continue;
            if (IsWalkable2x2(AnchorCell + dir * BossCellSize))
                return dir;
        }
        if (reverse != Vector2Int.zero && IsWalkable2x2(AnchorCell + reverse * BossCellSize))
            return reverse;

        return Vector2Int.zero; 
    }

    private void Lagging()
        {
            state = BossState.Lagging;
            stateTimer = laggingDelay;
        }

    private Vector2Int ChooseBestWayTowards(Vector2Int targetPos)
        {
            Vector2Int bestDir = Vector2Int.zero;
            int bestDistance = int.MaxValue;

        Vector2Int reverse = -lastDir;

        foreach (var dir in directions)
            {
            if (dir == reverse) continue;
                Vector2Int next = AnchorCell + dir * BossCellSize;
                if(!IsWalkable2x2(next))
                    continue;   

                int nextDistance = GetDistance(next, targetPos);

                if (nextDistance < bestDistance)
                {
                    bestDistance = nextDistance;
                    bestDir = dir;
                }
            }

        if (bestDir == Vector2Int.zero && reverse != Vector2Int.zero && IsWalkable2x2(AnchorCell + reverse * BossCellSize))
            bestDir = reverse;

        return bestDir;
        }

    private Vector2Int ChooseBestWayToRunAway(Vector2Int targetCell)
        {
            Vector2Int bestDir = Vector2Int.zero;
            int bestDistance = int.MinValue;

        Vector2Int reverse = -lastDir;

        foreach (var dir in directions)
            {
            if (dir == reverse) continue;

                Vector2Int next = AnchorCell + dir * BossCellSize;
                if (!IsWalkable2x2(next)) continue;

                int nextDistance = GetDistance(next, targetCell);
                if (nextDistance > bestDistance)
                {
                bestDistance = nextDistance;
                    bestDir = dir;
                }
            }

        if (bestDir == Vector2Int.zero && reverse != Vector2Int.zero && IsWalkable2x2(AnchorCell + reverse * BossCellSize))
            bestDir = reverse;

        return bestDir;
        }

    private bool IsWalkable2x2(Vector2Int anchor)
    {
        if (anchor.x < minX || anchor.x + 1 > maxX || anchor.y < minY || anchor.y + 1 > maxY)
            return false;

        Vector2 center = new Vector2(anchor.x + 0.5f, anchor.y + 0.5f);
        return Physics2D.OverlapBox(center, new Vector2(1.9f, 1.9f), 0f, obstacleLayer) == null;
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
        if (delta == new Vector2Int(BossCellSize, 0)) lastDir = Vector2Int.right;
        else if (delta == new Vector2Int(-BossCellSize, 0)) lastDir = Vector2Int.left;
        else if (delta == new Vector2Int(0, BossCellSize)) lastDir = Vector2Int.up;
        else if (delta == new Vector2Int(0, -BossCellSize)) lastDir = Vector2Int.down;

        AnchorCell = newAnchor;
        transform.position = new Vector3(newAnchor.x + 0.5f, newAnchor.y + 0.5f, 0); 
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
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
                    bossBodies[i].position = new Vector3(a.x + 0.5f, a.y + 0.5f, 0);
                }
            }
        }

    private void Restate()
        {
            AnchorCell = new Vector2Int(2, -2);
            transform.position = new Vector3(AnchorCell.x + 0.5f, AnchorCell.y + 0.5f, 0);

            for (int i = 1; i < bossBodies.Count; i++)
                if (bossBodies[i] != null)
                    Destroy(bossBodies[i].gameObject);


            bossBodies.Clear();
            bossBodies.Add(transform);

            //headPosHistory.Clear();

            //// Fill with the current cell so all segments start aligned
            //int required = (initialBodySegments) * bossBodyGap + 10;
            //for (int i = 0; i < required; i++)
            //    headPosHistory.Add(AnchorCell);
       
            for (int i = 0; i < initialBodySegments; i++)
                Grow();
        }

    private void Grow()
        {
            if (bossBodyPrefab == null) return;

            Transform body = Instantiate(bossBodyPrefab);
            body.position = bossBodies[bossBodies.Count - 1].position;
            bossBodies.Add(body); 
        }

    public void Die()
        {
            state = BossState.Die;
            for (int i = 0; i < bossBodies.Count; i++)
                if (bossBodies[i] != null) Destroy(bossBodies[i].gameObject);
        }

    private void SpawnPowerPickup()
    {
        if(powerUpPrefab == null) return;

        GameObject powerup = Instantiate(powerUpPrefab, new Vector3(transform.position.x + 0.5f, transform.position.y + 0.5f, transform.position.z), Quaternion.identity);

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

    private Vector2Int GetBestReachableGoalAnchorNearPlayer(Vector2Int playerCell)
    {
        // Candidate anchors that make the 2x2 overlap around the player.
        // (Because anchor is bottom-left of 2x2)
        var candidates = new List<Vector2Int>
        {
            new Vector2Int(playerCell.x, playerCell.y),
            new Vector2Int(playerCell.x-1, playerCell.y),
            new Vector2Int(playerCell.x, playerCell.y-1),
            new Vector2Int(playerCell.x-1, playerCell.y-1),
        };

        Vector2Int best = ParityCheck(candidates[0]);
        int bestDist = int.MaxValue;
        bool found = false;

        foreach (var c in candidates)
        {
            Vector2Int check = ParityCheck(c);
            if (!IsWalkable2x2(check)) continue;

            int d = GetDistance(check, playerCell);
            if (d < bestDist)
            {
                bestDist = d;
                best = check;
                found = true;
            }
        }

        // If none walkable, still return a parity-snapped position near player.
        if (!found)
            best = ParityCheck(playerCell);

        return best;
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

    private void UpdatePathToPlayer(Vector2Int goalAnchor, float dt)
    {
        repathTimer -= dt;
        if (repathTimer > 0f) return;

        repathTimer = repathInterval;

        currentPath.Clear();
        pathIndex = 0;

        if (FindPath2x2_AStar(AnchorCell, goalAnchor, currentPath))
        {
            // currentPath[0] is current anchor
            pathIndex = 1; // next step
        }
    }

    private bool TryFollowPathStep()
    {
        if (currentPath.Count == 0) return false;
        if (pathIndex >= currentPath.Count) return false;

        Vector2Int nextAnchor = currentPath[pathIndex];

        // derive direction from anchors
        Vector2Int delta = nextAnchor - AnchorCell;

        // delta should be (±2,0) or (0,±2)
        if (delta.x > 0) direction = Vector2Int.right;
        else if (delta.x < 0) direction = Vector2Int.left;
        else if (delta.y > 0) direction = Vector2Int.up;
        else if (delta.y < 0) direction = Vector2Int.down;

        // move now (your existing mover)
        if (!IsWalkable2x2(nextAnchor))
            return false;

        SetAnchor(nextAnchor);
        pathIndex++;
        return true;
    }
}


