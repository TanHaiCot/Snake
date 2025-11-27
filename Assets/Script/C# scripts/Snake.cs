using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Snake : MonoBehaviour
{
    private Vector2Int direction = Vector2Int.right;    //using Vector2Int for grid-based game 
    
    private List<Transform> bodies = new List<Transform>();

    [SerializeField] Transform bodyPrefab;
    [SerializeField] GameManager gameManager;
    [SerializeField] ScoreManager scoreManager;
    private float speed = 10f; 

    private int initialBodyPart = 4;

    private float nextMoveTime;

    private bool isInputLockOpened;  //work as a lock to prevent multiple direction change in one tick

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

        if (direction.x != 0)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                direction = Vector2Int.up;
                isInputLockOpened = true;
                return; //exit the function after direction change, avoid multiple changes in one frame
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                direction = Vector2Int.down;
                isInputLockOpened = true;
                return;
            }
        }

        if (direction.y != 0)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                direction = Vector2Int.right;
                isInputLockOpened = true;
                return;
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
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
        Debug.Log("next time move of Player: " + nextMoveTime);
        UpdateHeadRotation();

        // next position of the snake head the same tick
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
                if (hit.CompareTag("Wall") || hit.CompareTag("Door") || hit.CompareTag("Opponent Snake"))
                {
                    Debug.Log("Hit snake");
                    gameManager.GameOver();
                    return;
                }
            }
        }
        
        for (int i = bodies.Count - 1; i > 0; i--)
        {
            bodies[i].position = bodies[i - 1].position;
        }

        transform.position = new Vector3(nextX, nextY, 0);

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
        body.position = bodies[bodies.Count - 1].position;
        bodies.Add(body);

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
        }

        if (collision.CompareTag("Next Level Trigger"))
        {
            SceneManager.LoadScene("GamePlay2");
        }
    }
}
