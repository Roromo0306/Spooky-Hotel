using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableCharacterView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ClienteSO data;

    private Transform originalParent;
    private Vector3 originalPosition;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPosition = transform.localPosition;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Si no se movió a una celda permitida, revertimos
        if (transform.parent == originalParent || transform.parent == null)
        {
            Revert();
        }
    }

    public void Revert()
    {
        transform.SetParent(originalParent, false);
        transform.localPosition = originalPosition;
    }
}


