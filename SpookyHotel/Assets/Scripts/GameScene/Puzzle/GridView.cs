using UnityEngine;
using System.Collections.Generic;

public class GridView : MonoBehaviour
{
    public GameObject cellPrefab;
    public Transform container;
    private List<CellView> _cells = new List<CellView>();
    public CellView[] Cells => _cells.ToArray();

    public void BuildGrid()
    {
        if (cellPrefab == null || container == null)
        {
            Debug.LogError("[GridView] cellPrefab or container not assigned.");
            return;
        }

        for (int i = container.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (Application.isPlaying) Destroy(container.GetChild(i).gameObject); else DestroyImmediate(container.GetChild(i).gameObject);
#else
            Destroy(container.GetChild(i).gameObject);
#endif
        }
        _cells.Clear();

        for (int i = 0; i < PuzzleModel.CellCount; i++)
        {
            var go = Instantiate(cellPrefab, container);
            var cv = go.GetComponent<CellView>();
            if (cv == null)
            {
                Debug.LogError("[GridView] cellPrefab does not contain CellView.", go);
                return;
            }
            cv.index = i;
            cv.SetAllowed(false);
            _cells.Add(cv);
        }
    }

    /// <summary>
    /// marca las celdas permitidas según el array booleando allowedIndices.
    /// </summary>
    public void SetAllowedIndices(bool[] allowedIndices)
    {
        if (_cells == null || _cells.Count == 0) return;
        if (allowedIndices == null || allowedIndices.Length != _cells.Count)
        {
            // si hay mismatch, limpia
            foreach (var c in _cells) c.SetAllowed(false);
            return;
        }

        for (int i = 0; i < _cells.Count; i++)
        {
            _cells[i].SetAllowed(allowedIndices[i]);
        }
    }

    public void ClearAllowedMarks()
    {
        foreach (var c in _cells) c.SetAllowed(false);
    }
}
