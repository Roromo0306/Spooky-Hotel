using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DraggableCharacterView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ClienteSO data;
    private Canvas _canvas;
    private RectTransform _rt;
    private CanvasGroup _cg;
    private Vector3 _startPos;
    private Transform _originalParent;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _startPos = _rt.anchoredPosition3D;
        _originalParent = transform.parent;
        transform.SetParent(_canvas.transform, true);
        _cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out pos);
        _rt.anchoredPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _cg.blocksRaycasts = true;
        if (transform.parent == _canvas.transform)
        {
            transform.SetParent(_originalParent, true);
            _rt.anchoredPosition3D = _startPos;
        }
    }
}

