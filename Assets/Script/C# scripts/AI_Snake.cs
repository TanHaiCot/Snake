using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AI_Snake : MonoBehaviour
{
    private Vector2Int direction = Vector2Int.right;    //using Vector2Int for grid-based game
    private List<Transform> bodies = new List<Transform>();

    private float nextMoveTime;
    private float speed = 10f;

    [SerializeField] Transform bodyPrefab;

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

    private void FixedUpdate()
    {
        if (Time.time < nextMoveTime)
            return;

        nextMoveTime = Time.time + (1.0f / speed);

        UpdateHeadRotation();
        UpdateAIDirection(); 

        // next position of the snake head the same tick
        int nextX = Mathf.RoundToInt(this.transform.position.x) + direction.x;
        int nextY = Mathf.RoundToInt(this.transform.position.y) + direction.y;


        //check all the collision on the world space to see which one is overlap with the next position

        for (int i = bodies.Count - 1; i > 0; i--)
        {
            bodies[i].position = bodies[i - 1].position;
        }

        transform.position = new Vector3(nextX, nextY, 0);
    }

    private void UpdateAIDirection()
    {
        
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
            Grow();
        }
    }
}
