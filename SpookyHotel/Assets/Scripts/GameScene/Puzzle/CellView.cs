using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class CellView : MonoBehaviour, IDropHandler
{
    public int index;
    public Image background;
    public Transform contentParent;
    public event Action<int, DraggableCharacterView> OnDropped;

    // Visuals
    [Header("Visuals")]
    public Color normalColor = Color.white;
    public Color allowedColor = new Color(0.6f, 1f, 0.6f, 1f); // greenish
    public Color forbiddenColor = new Color(1f, 0.6f, 0.6f, 1f); // redish (optional)

    private bool _isAllowed = false;

    public void SetAllowed(bool allowed)
    {
        _isAllowed = allowed;
        if (background != null)
        {
            background.color = allowed ? allowedColor : normalColor;
        }
    }

    public bool IsAllowed() => _isAllowed;

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag;
        if (dragged == null) return;
        var draggable = dragged.GetComponent<DraggableCharacterView>();
        if (draggable == null) return;

        // If not allowed, reject immediately (revert will be handled by draggable)
        if (!_isAllowed)
        {
            Debug.Log("[CellView] Drop rejected - cell not allowed: " + index);
            // keep draggable parent as is (it will revert on EndDrag)
            // but notify controller with the attempt so it can show message if needed
            OnDropped?.Invoke(index, draggable);
            return;
        }

        // move object into this cell
        draggable.transform.SetParent(contentParent, false);
        var rt = draggable.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = Vector2.zero;

        OnDropped?.Invoke(index, draggable);
    }

    public void Clear()
    {
        foreach (Transform t in contentParent) Destroy(t.gameObject);
    }
}
