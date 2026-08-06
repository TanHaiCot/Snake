using UnityEngine;
using System.Collections.Generic;


public class Pathfinding : MonoBehaviour
{
    public enum PathPurpose
    {
        FoodChasing,
        PlayerChasing,
        Patrol
    }

    [Header("Map")]
    [SerializeField] private MapManager mapManager;

    //[SerializeField] public LayerMask wallLayer;
    [SerializeField] Snake snake;
    [SerializeField] AI_Snake opponentSnake;
    [SerializeField] Food[] foodToAvoid; 

    [Header("Teleport")]
    [SerializeField] private TeleportGateManager teleportGateManager;
    [SerializeField] private int teleportMoveCost = 1;

    private static readonly Vector2Int[] directions = new Vector2Int[]
    {
        Vector2Int.up,   
        Vector2Int.down,   
        Vector2Int.left,  
        Vector2Int.right   
    };

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return mapManager.WorldToCell(worldPos);
    }

    public Vector3 GridToWorld(Vector2Int cell)
    {
        return mapManager.CellToWorld(cell);
    }

    private bool IsFood(Vector2Int pos)
    {
        if (foodToAvoid == null)
            return false; 

        foreach (var food in foodToAvoid)
        {
            if (food == null || !food.gameObject.activeInHierarchy) continue;

            Vector2Int foodPos = WorldToGrid(food.transform.position);

            if (foodPos == pos)
                return true;
        }
        return false;
    }

    public bool IsWalkable(Vector2Int position, PathPurpose purpose)
    {
        bool isTeleportCell =
        teleportGateManager != null &&
        teleportGateManager.IsTeleportCell(position);

        if (!mapManager.IsWalkable(position) && !isTeleportCell)
            return false;

        if (purpose == PathPurpose.Patrol)
        {
            if (IsFood(position))
                return false;

            return true; 
        }

        if (snake.SpotOccupied(position.x, position.y))
            return false;

        if(opponentSnake != null)
            if (opponentSnake.SpotOccupied(position.x, position.y))
                return false;

        return true;    
    }

    public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos, PathPurpose purpose)
    {
        Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();

        Node startNode = GetNode(nodes, startPos);
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startPos, targetPos);

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

                if(closedSet.Contains(neighbourPos))
                    continue;

                switch(purpose)
                {
                    case PathPurpose.FoodChasing:
                    case PathPurpose.Patrol:
                        if (!IsWalkable(neighbourPos, purpose))
                            continue;
                        break;

                    case PathPurpose.PlayerChasing:
                        if (neighbourPos != targetPos && !IsWalkable(neighbourPos, purpose)) //allow the target position even if occupied
                            continue;
                        break; 
                }

                Node neighbourNode = GetNode(nodes, neighbourPos);
                int gCostToNeighbour = currentNode.gCost + GetDistance(currentNode.position, neighbourPos); 
              
                if (gCostToNeighbour < neighbourNode.gCost || !openList.Contains(neighbourNode))
                {
                    neighbourNode.gCost = gCostToNeighbour;
                    neighbourNode.hCost = GetDistance(neighbourPos, targetPos);
                    neighbourNode.parent = currentNode;

                    if (!openList.Contains(neighbourNode))
                        openList.Add(neighbourNode);
                    
                }
            }

            if (teleportGateManager != null && teleportGateManager.TryGetTeleportExit(currentNode.position, out Vector2Int teleportExit))
            {
                if (!closedSet.Contains(teleportExit))
                {
                    bool canTeleport = false;

                    if (purpose == PathPurpose.PlayerChasing)
                    {
                        canTeleport = teleportExit == targetPos || IsWalkable(teleportExit, purpose);
                    }
                    else
                    {
                        canTeleport = IsWalkable(teleportExit, purpose);
                    }

                    if (canTeleport)
                    {
                        Node teleportNode = GetNode(nodes, teleportExit);
                        int gCostToTeleport = currentNode.gCost + teleportMoveCost;

                        if (gCostToTeleport < teleportNode.gCost || !openList.Contains(teleportNode))
                        {
                            teleportNode.gCost = gCostToTeleport;
                            teleportNode.hCost = GetDistance(teleportExit, targetPos);
                            teleportNode.parent = currentNode;

                            if (!openList.Contains(teleportNode))
                                openList.Add(teleportNode);
                        }
                    }
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

    public bool TryGetTeleportExit(Vector2Int entryCell, out Vector2Int exitCell)
    {
        exitCell = Vector2Int.zero;

        if (teleportGateManager == null)
            return false;

        return teleportGateManager.TryGetTeleportExit(entryCell, out exitCell);
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

    private int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }     

    public bool TryToGetRandomWalkablePosition(out Vector2Int result, int attempts = 200) //by default try 200 times
    {
        result = Vector2Int.zero;

        List<Vector2Int> walkableCells = mapManager.GetWalkableCells();

        if (walkableCells == null || walkableCells.Count == 0)
            return false;

        for (int i = 0; i < attempts; i++)
        {
            Vector2Int randomPos = walkableCells[Random.Range(0, walkableCells.Count)];

            if (IsWalkable(randomPos, PathPurpose.Patrol))
            {
                result = randomPos;
                return true;
            }
        }

        return false; 
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
        gCost = int.MaxValue;
        hCost = 0;
    }

}
#endregion