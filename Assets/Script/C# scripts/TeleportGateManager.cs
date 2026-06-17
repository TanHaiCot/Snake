using System.Collections.Generic;
using UnityEngine;

public class TeleportGateManager : MonoBehaviour
{
    [SerializeField] private List<TeleportGate> gates = new List<TeleportGate>();

    public bool TryGetTeleportExit(Vector2Int entryCell, out Vector2Int exitCell)
    {
        exitCell = Vector2Int.zero;

        TeleportGate entryGate = null;
        int entryIndex = -1;

        foreach (TeleportGate gate in gates)
        {
            if (gate == null) continue;

            int index = gate.GetCellIndex(entryCell);

            if (index >= 0)
            {
                entryGate = gate;
                entryIndex = index;
                break;
            }
        }

        if (entryGate == null)
            return false;

        foreach (TeleportGate otherGate in gates)
        {
            if (otherGate == null) continue;
            if (otherGate == entryGate) continue;

            if (otherGate.gateColor == entryGate.gateColor)
            {
                exitCell = otherGate.GetCellByIndex(entryIndex);
                return true;
            }
        }

        return false;
    }

    public bool IsTeleportCell(Vector2Int cell)
    {
        foreach (TeleportGate gate in gates)
        {
            if (gate == null) continue;

            if (gate.ContainsCell(cell))
                return true;
        }

        return false;
    }
}
