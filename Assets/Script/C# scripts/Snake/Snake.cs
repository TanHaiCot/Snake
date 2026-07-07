using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Snake : MonoBehaviour
{
    private Vector2Int direction = Vector2Int.right;    //using Vector2Int for grid-based game 
    
    private List<Transform> bodies = new List<Transform>();

    [SerializeField] Transform bodyPrefab;
    [SerializeField] Transform bodyContainer;
    [SerializeField] Energy energy;
    [SerializeField] SnakeAbilities snakeAbilities;

    [Header("Managers")]
    [SerializeField] GameManager gameManager;
    [SerializeField] BossFightManager bossFightManager; 
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] DarknessManager darknessManager;
    [SerializeField] MapManager mapManager;
    [SerializeField] TeleportGateManager teleportGateManager;

    public UnityEvent OnFoodEaten;

    private float speed = 10f; 

    private int initialBodyPart = 4;

    private float moveTimer; 

    private bool isInputLockOpened;  //work as a lock to prevent multiple direction change in one tick

    private bool canReadInput = true;

    public enum KeyType
    {
        WASD,
        Arrows
    }

    [SerializeField] private KeyType keyType = KeyType.WASD;
    [SerializeField] private bool reverseMovement = false;

    private void Awake()
    {
        Restate();
    }

    public void SetInputEnabled(bool enabled)
    {
        canReadInput = enabled;
    }

    private void Update()
    {
        if (!canReadInput)
            return;

        UpdateSnakeMovement();  
    }

    private void UpdateSnakeMovement()
    {
        if (isInputLockOpened)
            return;

        KeyCode upKey;
        KeyCode downKey;
        KeyCode leftKey;
        KeyCode rightKey;

        if(keyType == KeyType.WASD)
        {
            upKey = KeyCode.W;
            downKey = KeyCode.S;
            leftKey = KeyCode.A;
            rightKey = KeyCode.D;
        }
        else
        {
            upKey = KeyCode.UpArrow;
            downKey = KeyCode.DownArrow;
            leftKey = KeyCode.LeftArrow;
            rightKey = KeyCode.RightArrow;
        }

        if(reverseMovement)
        {
            // Swap the key assignments for reverse movement
            KeyCode temp = upKey;
            upKey = downKey;
            downKey = temp;

            temp = leftKey;
            leftKey = rightKey;
            rightKey = temp;
        }

        if (direction.x != 0)
        {
            if (Input.GetKeyDown(upKey))
            {
                direction = Vector2Int.up;
                isInputLockOpened = true;
                return; //exit the function after direction change, avoid multiple changes in one frame
            }
            else if (Input.GetKeyDown(downKey))
            {
                direction = Vector2Int.down;
                isInputLockOpened = true;
                return;
            }
        }
        
        if (direction.y != 0)
        {
            if (Input.GetKeyDown(rightKey))
            {
                direction = Vector2Int.right;
                isInputLockOpened = true;
                return;
            }
            else if (Input.GetKeyDown(leftKey))
            {
                direction = Vector2Int.left;
                isInputLockOpened = true;
                return;
            }
        }
    }

    private void FixedUpdate()
    {
        float effectiveSpeed = snakeAbilities != null ? snakeAbilities.ModifySpeed(speed) : speed;

        float intervalForA_Move = 1.0f / effectiveSpeed;   //time waiting for a move

        moveTimer += Time.fixedDeltaTime;
        if (moveTimer < intervalForA_Move)
            return; 

        moveTimer -= intervalForA_Move;

        UpdateHeadRotation();

        // next position of the snake head at the same tick
        int nextX = Mathf.RoundToInt(this.transform.position.x) + direction.x;
        int nextY = Mathf.RoundToInt(this.transform.position.y) + direction.y;

        Vector2Int nextCell = new Vector2Int(nextX, nextY);

        if (teleportGateManager != null && teleportGateManager.TryGetTeleportExit(nextCell, out Vector2Int teleportExit))
        {
            nextCell = teleportExit;
            nextX = nextCell.x;
            nextY = nextCell.y;
        }

        if (SpotOccupied(nextX, nextY))
        {
            Debug.Log("Hit itself");
            gameManager.GameOver();
            return;
        }

        bool nextIsWallOrDoor = false;

        if (mapManager != null && !mapManager.IsWalkable(new Vector2Int(nextX, nextY)))
        {
            nextIsWallOrDoor = true;
        }

        //check all the collision on the world space to see which one is overlap with the next position
        var hits = Physics2D.OverlapPointAll(new Vector2(nextX, nextY));
        foreach (var hit in hits)
        {
            if (hit != null && hit.gameObject != this.gameObject)
            {
                if (hit.CompareTag("Opponent Snake"))
                {
                    if (bossFightManager == null)
                    {
                        gameManager.GameOver();
                        return; 
                    }
                    
                    bool stopMoving = bossFightManager.HandleCollisionBetweenSnakeAndBoss();
                    
                    if (stopMoving)
                        return;

                    continue; 
                }

                if (hit.CompareTag("Door"))
                {
                    nextIsWallOrDoor = true; 
                }
            }
        }

        if (nextIsWallOrDoor && (snakeAbilities == null || !snakeAbilities.GhostActive))
        {
            Debug.Log("Hit wall/door");
            //gameManager.GameOver();
            //return;
        }

        // let player finish the move thru wall if the ghost mode is off but the bodies still not yet thru wall
        if (nextIsWallOrDoor && snakeAbilities != null && snakeAbilities.GhostActive)
        {
            snakeAbilities.NotifyHeadEnteredWall(bodies.Count);
        }

        for (int i = bodies.Count - 1; i > 0; i--)
        {
            bodies[i].position = bodies[i - 1].position;
        }

        transform.position = new Vector3(nextCell.x, nextCell.y, 0);

        snakeAbilities?.GhostModeRemaining();

        if (darknessManager != null)
            darknessManager.UpdateVisibility(); //Update visibility after snake move (newest head position)


        isInputLockOpened = false; 
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

    public void Grow()
    {
        Transform body = Instantiate(bodyPrefab);
        body.SetParent(bodyContainer);
        body.position = bodies[bodies.Count - 1].position;
        bodies.Add(body);

    }

    public void Restate()
    {
        direction = Vector2Int.right;
        this.transform.position = new Vector3(-3, -10, 0);

        for (int i = 1; i < bodies.Count; i++)
        {
            Destroy(bodies[i].gameObject);
        }
        bodies.Clear();
        bodies.Add(this.transform); 

        for (int i = 0; i < initialBodyPart - 1;  i++)   //not count the Head 
        {
            Grow(); 
        }
    }

    public bool SpotOccupied(int x, int y)
    {
        foreach (Transform body in bodies)
        {
            if(Mathf.RoundToInt(body.position.x) == x &&
                Mathf.RoundToInt(body.position.y) == y)
            {
                return true;
            }
        }
        return false; 
    } 

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Food"))
        {
            Grow();
            if(scoreManager)
                scoreManager.AddScore(1);

            OnFoodEaten?.Invoke();

            if (gameManager != null)
                gameManager.CheckWinStatus(); 
        }

        if (collision.CompareTag("Next Level Trigger"))
        {
            gameManager.MoveToSkillTree();
        }
    }

    public void SetGhostVisual(bool isGhost)
    {
        float alpha = isGhost ? 0.65f : 1f;

        foreach (Transform body in bodies)
        {
            SpriteRenderer sr = body.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }

    public void SetReverseMovement(bool reversed)
    {
        reverseMovement = reversed;
    }
}
