using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro

[RequireComponent(typeof(Collider2D))]
public class ClienteController : MonoBehaviour
{
    [Header("Datos")]
    public ClienteSO clienteData;

    [Header("Visuals (optional)")]
    public SpriteRenderer spriteRenderer;   // para 2D normal
    public Image uiImage;                   // si usas UI Image en world space
    public TextMeshProUGUI nameTMP;         // TextMeshPro para nombre

    [Header("Movimiento")]
    public float moveSpeed = 2f;

    // pequeño epsilon para considerar llegada al punto
    private const float ArrivalEpsilon = 0.02f;

    private Transform _target;
    private bool _isMoving = false;
    private bool _isLeaving = false;

    public event Action OnReachedDestination;
    public event Action OnLeftScene;

    // Inicializa datos y visuales desde el ScriptableObject
    public void Initialize(ClienteSO data)
    {
        clienteData = data;
        ApplyVisuals();
        Debug.Log($"[ClienteController] Initialized cliente '{data?.nombre}' (id={data?.id})");
    }

    private void ApplyVisuals()
    {
        if (clienteData == null) return;

        // Asignar sprite al SpriteRenderer si existe
        if (spriteRenderer != null && clienteData.sprite != null)
        {
            spriteRenderer.sprite = clienteData.sprite;
            spriteRenderer.color = Color.white;
        }

        // Asignar a UI Image si lo usamos
        if (uiImage != null && clienteData.sprite != null)
        {
            uiImage.sprite = clienteData.sprite;
            uiImage.SetNativeSize();
            uiImage.color = Color.white;
        }

        // Asignar nombre a TextMeshPro
        if (nameTMP != null && !string.IsNullOrEmpty(clienteData.nombre))
        {
            nameTMP.text = clienteData.nombre;
        }
    }

    // Inicia movimiento hacia el transform objetivo (spawn -> destination)
    public void MoveTo(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("[ClienteController] MoveTo: target es null");
            return;
        }

        _target = target;
        _isMoving = true;
        _isLeaving = false;
        StopAllCoroutines();
        StartCoroutine(MoveCoroutine());
        Debug.Log($"[ClienteController] MoveTo llamado para '{clienteData?.nombre}' hacia '{target.name}' (pos {target.position})");
    }

    private IEnumerator MoveCoroutine()
    {
        Debug.Log($"[ClienteController] MoveCoroutine START para '{clienteData?.nombre}'");
        if (_target == null)
        {
            _isMoving = false;
            yield break;
        }

        while (_isMoving && _target != null)
        {
            Vector3 current = transform.position;
            Vector3 goal = _target.position;
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(current, goal, step);

            float dist = Vector3.Distance(transform.position, goal);
            if (dist <= ArrivalEpsilon)
            {
                transform.position = goal;
                _isMoving = false;
                Debug.Log($"[ClienteController] Ha llegado '{clienteData?.nombre}' a destino {goal}");
                OnReachedDestination?.Invoke();
                yield break;
            }

            yield return null;
        }
    }

    // Hace que el cliente se vaya hacia exitPoint
    public void Leave(Transform exitPoint, Action onFinish = null)
    {
        if (exitPoint == null)
        {
            Debug.LogWarning("[ClienteController] Leave: exitPoint es null");
            onFinish?.Invoke();
            Destroy(gameObject);
            return;
        }

        if (_isLeaving) return;
        _isLeaving = true;
        _target = exitPoint;
        _isMoving = true;
        StopAllCoroutines();
        StartCoroutine(LeaveCoroutine(onFinish));
        Debug.Log($"[ClienteController] Leave llamado para '{clienteData?.nombre}' hacia '{exitPoint.name}' (pos {exitPoint.position})");
    }

    private IEnumerator LeaveCoroutine(Action onFinish)
    {
        Debug.Log($"[ClienteController] LeaveCoroutine START para '{clienteData?.nombre}'");
        while (_isMoving && _target != null)
        {
            Vector3 current = transform.position;
            Vector3 goal = _target.position;
            float step = moveSpeed * 1.2f * Time.deltaTime; // sale algo más rápido
            transform.position = Vector3.MoveTowards(current, goal, step);

            float dist = Vector3.Distance(transform.position, goal);
            if (dist <= ArrivalEpsilon)
            {
                transform.position = goal;
                _isMoving = false;
                Debug.Log($"[ClienteController] Ha salido '{clienteData?.nombre}' (pos {goal})");
                OnLeftScene?.Invoke();
                onFinish?.Invoke();
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }
}
