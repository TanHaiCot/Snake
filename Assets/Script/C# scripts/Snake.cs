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
    [SerializeField] GameManager gameManager;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] DarknessManager darknessManager;

    public UnityEvent OnFoodEaten;

    private float speed = 15f; 

    private int initialBodyPart = 4;

    private float nextMoveTime;

    private bool isInputLockOpened;  //work as a lock to prevent multiple direction change in one tick

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

    private void Update()
    {
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
        if(Time.time < nextMoveTime)
            return;
        
        nextMoveTime = Time.time + (1.0f / speed);
        
        UpdateHeadRotation();

        // next position of the snake head at the same tick
        int nextX = Mathf.RoundToInt(this.transform.position.x) + direction.x;
        int nextY = Mathf.RoundToInt(this.transform.position.y) + direction.y;

        if (SpotOccupied(nextX, nextY))
        {
            Debug.Log("Hit itself");
            gameManager.GameOver();
            return;
        }

        //check all the collision on the world space to see which one is overlap with the next position
        var hits = Physics2D.OverlapPointAll(new Vector2(nextX, nextY));
        foreach (var hit in hits)
        {
            if (hit != null && hit.gameObject != this.gameObject)
            {
                if (/*hit.CompareTag("Wall") || hit.CompareTag("Door") ||*/ hit.CompareTag("Opponent Snake"))
                {
                    Debug.Log("Hit snake");
                    gameManager.GameOver();
                    //return;
                }
            }
        }

        for (int i = bodies.Count - 1; i > 0; i--)
        {
            bodies[i].position = bodies[i - 1].position;
        }
        transform.position = new Vector3(nextX, nextY, 0);


        if(darknessManager != null)
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
        Transform body = Instantiate(bodyPrefab, bodyContainer);
        body.position = bodies[bodies.Count - 1].position;
        bodies.Add(body);

    }

    public void Restate()
    {
        direction = Vector2Int.right;
        this.transform.position = new Vector3(0, 0, 0);

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
        }

        if (collision.CompareTag("Next Level Trigger"))
        {
            SceneManagement.Instance.NextLevel();
        }
    }
}
