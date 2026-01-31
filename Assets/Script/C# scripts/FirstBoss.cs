using System;
using System.Collections.Generic;
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
    //[SerializeField] Snake playerSnake;
    [SerializeField] BossFightManager bossFightManager;

    [Header("Boss Settings")]
    int initialBodySegments = 3;   
    private float moveSpeed = 5f;          // steps/sec
    private float dashSpeed = 22f;          // steps/sec during dash
    private float dashTriggerRange = 10f;
    private int currentDashDistance; 
    private int maxDashDistance = 8; 
    private float laggingDelay = 0.2f;       // seconds between steps when lagging
    private float stunDuration = 3f;
    private float fleeDuration = 8f;           

    public BossState state = BossState.Chasing;

    public Vector2Int AnchorCell { get; private set; }

    private List<Transform> bossBodies = new();
    //int bossBodyGap = 2;
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

    private void Start()
    {
        Bounds bound = gridArea.bounds;
        minX = Mathf.RoundToInt(bound.min.x);
        maxX = Mathf.RoundToInt(bound.max.x);
        minY = Mathf.RoundToInt(bound.min.y);
        maxY = Mathf.RoundToInt(bound.max.y);

        Restate(); 
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
                    state = BossState.Fleeing;
                    stateTimer = fleeDuration;
                }
            }
            return; 
        }   

        if(state == BossState.Fleeing)
        {
            stateTimer -= Time.fixedDeltaTime;
            if (stateTimer <= 0f)
                state = BossState.Chasing;

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
    }

    private void Flee()
    {

        Vector2Int playerCell = WorldToGrid(player.position);
        direction = ChooseBestWayToRunAway(playerCell);
        TryToMove(direction);
    }

    private void Chase()
    {
        //if (bossFightManager != null && bossFightManager.PlayerEmpowered)
        //{
        //    State = BossState.Fleeing;
        //    return; 
        //}
             
        Vector2Int playerPos = WorldToGrid(player.position);
        int distace = GetDistance(AnchorCell, playerPos);

        if (distace <= dashTriggerRange)
        {
            direction = ChooseBestWayTowards(playerPos);
            Lagging();
            return;
        }
        direction = ChooseBestWayTowards(playerPos);
        TryToMove(direction); 
    }

    private void TryToMove(Vector2Int dir)
    {
        Vector2Int next = AnchorCell + dir * BossCellSize;

        if (IsWalkable2x2(next))
        {
            SetAnchor(next);
            return;
        }

        Vector2Int otherWay = ChooseAnyWalkableWay();
        if (otherWay != Vector2Int.zero)
        {
            SetAnchor(AnchorCell + otherWay * BossCellSize); 
        }
    }

    private Vector2Int ChooseAnyWalkableWay()
    {
        foreach(var dir in directions)
        {
            if (IsWalkable2x2(AnchorCell + dir * BossCellSize))
                return dir;
        }

        return Vector2Int.zero; 
    }

    private void Lagging()
    {
        state = BossState.Lagging;
        stateTimer = laggingDelay;
    }


    private Vector2Int ChooseBestWayTowards(Vector2Int targetPos)
    {
        Vector2Int bestDir = direction;
        int closestDistance = int.MaxValue;

        foreach(var dir in directions)
        {
            Vector2Int next = AnchorCell + dir * BossCellSize;
            if(!IsWalkable2x2(next))
                continue;   

            int nextDistance = GetDistance(next, targetPos);

            if (nextDistance < closestDistance)
            {
                closestDistance = nextDistance;
                bestDir = dir;
            }
        }

        return bestDir;
    }

    private Vector2Int ChooseBestWayToRunAway(Vector2Int targetCell)
    {
        Vector2Int bestDir = direction;
        int bestScore = int.MinValue;

        foreach (var dir in directions)
        {
            Vector2Int next = AnchorCell + dir * BossCellSize;
            if (!IsWalkable2x2(next)) continue;

            int score = GetDistance(next, targetCell);
            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }
        return bestDir;
    }

    private bool IsWalkable2x2(Vector2Int anchor)
    {
        if (anchor.x < minX || anchor.x + 1 > maxX || anchor.y < minY || anchor.y + 1 > maxY)
            return false;

        Vector2 center = new Vector2(anchor.x + 0.5f, anchor.y + 0.5f); 
        if (Physics2D.OverlapBox(center, new Vector2(1.9f, 1.9f), 0f, obstacleLayer) != null)
            return false;

        return true;    
    }

  
    private int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private void SetAnchor(Vector2Int newAnchor)
    {
        for(int i = bossBodies.Count - 1; i > 0 ; i--)
        {
            bossBodies[i].position = bossBodies[i - 1].position;
        }   

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
        AnchorCell = new Vector2Int(0, 0);
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
        // simple: destroy bodies
        for (int i = 0; i < bossBodies.Count; i++)
            if (bossBodies[i] != null) Destroy(bossBodies[i].gameObject);
    }
}
