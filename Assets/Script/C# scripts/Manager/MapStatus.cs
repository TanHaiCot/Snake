using System.Collections.Generic;
using UnityEngine;

public class MapStatus : MonoBehaviour
{
    public static MapStatus Instance { get; private set; }

    public int width; 
    public int height;

    public bool[] greyedTiles; 

    public HashSet<Vector2Int> GreyedWallPositions  = new HashSet<Vector2Int>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null); 
        DontDestroyOnLoad(gameObject);
    }   

    public void SaveTiles(int w, int h, bool[] greyed)
    {
        width = w;
        height = h;
        greyedTiles = (bool[])greyed.Clone();
    }

    public bool IsTheMapValidToSave(int w, int h)
    {
        return greyedTiles != null && width == w && height == h;
    }

    public void ClearWalls()
    {
        GreyedWallPositions.Clear();
    }
}
