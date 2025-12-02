using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;


public class Pathfinding : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private BoxCollider2D gridArea;

    [SerializeField] LayerMask wallLayer;
    [SerializeField] Snake snake;
    [SerializeField] AI_Snake opponentSnake;

    private int minX, maxX, minY, maxY;
    private static readonly Vector2Int[] directions = new Vector2Int[]
    {
        Vector2Int.up,   
        Vector2Int.down,   
        Vector2Int.left,  
        Vector2Int.right   
    };

    private void Awake()
    {
        Bounds bound = gridArea.bounds;

        minX = Mathf.RoundToInt(bound.min.x);
        maxX = Mathf.RoundToInt(bound.max.x);
        minY = Mathf.RoundToInt(bound.min.y);
        maxY = Mathf.RoundToInt(bound.max.y);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.y);
        return new Vector2Int(x, y);
    }

    private bool IsWalkable(Vector2Int position)
    {
        if(position.x < minX || position.x >= maxX || position.y < minY || position.y >= maxY)
            return false;

        Collider2D obstacles = Physics2D.OverlapBox(new Vector2(position.x, position.y), new Vector2(0.9f, 0.9f), 0, wallLayer);
        if(obstacles != null)
            return false;

        if (snake.SpotOccupied(position.x, position.y))
            return false;

        if (opponentSnake.SpotOccupied(position.x, position.y))
            return false;

        return true;    
    }

    public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos)
    {
        Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();

        Node startNode = GetNode(nodes, startPos); 

        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();  
        openList.Add(startNode);

        while(openList.Count > 0)
        {
            Node currentNode = openList[0]; 
            for(int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost || 
                    (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }
            openList.Remove(currentNode);
            closedSet.Add(currentNode.position);

            if(currentNode.position == targetPos)
            {
                return RetracePath(startNode, currentNode); 
            }

            foreach(Vector2Int dir in directions)
            {
                Vector2Int neighbourPos = currentNode.position + dir;

                if(closedSet.Contains(neighbourPos) || !IsWalkable(neighbourPos))
                    continue;

                Node neighbourNode = GetNode(nodes, neighbourPos);
                int gCostToNeighbour = currentNode.gCost + 1; 
              
                if (gCostToNeighbour < neighbourNode.gCost || !openList.Contains(neighbourNode))
                {
                    neighbourNode.gCost = gCostToNeighbour;
                    neighbourNode.hCost = hCostCalculation(neighbourPos, targetPos);
                    neighbourNode.parent = currentNode;
                    openList.Add(neighbourNode);
                }
            }
        }
        return null;
    }

    private List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;

        while(currentNode != startNode)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    private Node GetNode(Dictionary<Vector2Int, Node> nodes, Vector2Int position)
    {
        if(!nodes.TryGetValue(position, out Node nodeFound))
        {
            nodeFound = new Node(position);
            nodes[position] = nodeFound;
        }
        return nodeFound; 
    }

    private int hCostCalculation(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }  


    
}




#region Node Class 
public class Node
{
    public Vector2Int position;
    public Node parent;
    public int gCost; // Cost from start node
    public int hCost; // Heuristic cost to end node
    public int fCost => gCost + hCost; // Total cost
    public Node(Vector2Int pos)
    {
        position = pos;
    }

}
#endregion