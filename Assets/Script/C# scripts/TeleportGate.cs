using System.Collections.Generic;
using UnityEngine;

public class TeleportGate : MonoBehaviour
{
    public enum GateColor
    {
        Yellow,
        Blue,
        Green,
        Red,
        Purple
    }

    [Header("Gate Info")]
    public GateColor gateColor;

    [Tooltip("Cells used by this gate, in order. Left-to-right or bottom-to-top.")]
    public List<Vector2Int> cells = new List<Vector2Int>();

    public bool ContainsCell(Vector2Int cell)
    {
        return cells.Contains(cell);
    }

    public int GetCellIndex(Vector2Int cell)
    {
        return cells.IndexOf(cell);
    }

    public Vector2Int GetCellByIndex(int index)
    {
        index = Mathf.Clamp(index, 0, cells.Count - 1);
        return cells[index];
    }
}

