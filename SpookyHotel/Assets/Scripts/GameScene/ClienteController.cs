using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class ClienteController : MonoBehaviour
{
    [Header("Datos")]
    public ClienteSO clienteData;

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public TextMeshProUGUI nameTMP;

    [Header("Movimiento")]
    public float moveSpeed = 2f;

    [Header("Progreso")]
    public ProgressBarView progressView; // asigna prefab/instance de la UI (puede ser un child o global)
    [Range(0.1f, 10f)] public float progressSecondsToFull = 10f; // tiempo hasta 100%
    public float shakeStartPercent = 0.8f; // start shake at 80%
    public float shakeMagnitude = 0.1f; // shake displacement (units)
    public float shakeSpeed = 20f; // frequency

    private Transform _target;
    private bool _isMoving = false;
    private bool _isLeaving = false;

    private Vector3 _originPosition; // base position at destination
    private Coroutine _progressCoroutine;
    private Coroutine _shakeCoroutine;

    public event Action OnReachedDestination;
    public event Action OnLeftScene;

    public void Initialize(ClienteSO data)
    {
        clienteData = data;
        ApplyVisuals();
        Debug.Log($"[ClienteController] Initialized cliente '{data?.nombre}' (id={data?.id})");
    }

    private void ApplyVisuals()
    {
        if (clienteData == null) return;
        if (spriteRenderer != null)
        {
            // default to first sprite if available
            if (clienteData.stageSprites != null && clienteData.stageSprites.Length > 0)
                spriteRenderer.sprite = clienteData.stageSprites[0];
        }

        if (nameTMP != null && !string.IsNullOrEmpty(clienteData?.nombre))
            nameTMP.text = clienteData.nombre;
    }

    public void MoveTo(Transform target)
    {
        if (target == null) { Debug.LogWarning("MoveTo target null"); return; }
        _target = target;
        _isMoving = true;
        _isLeaving = false;
        StopAllCoroutines();
        StartCoroutine(MoveCoroutine());
        Debug.Log($"[ClienteController] MoveTo '{clienteData?.nombre}' -> {target.name}");
    }

    private IEnumerator MoveCoroutine()
    {
        while (_isMoving && _target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, _target.position, moveSpeed * Time.deltaTime);
            float dist = Vector3.Distance(transform.position, _target.position);
            if (dist <= 0.02f)
            {
                transform.position = _target.position;
                _isMoving = false;
                Debug.Log($"[ClienteController] Reached destination: {transform.position}");
                // set origin base for shake
                _originPosition = transform.position;
                OnReachedDestination?.Invoke();
                // start progress routine
                StartProgress();
                yield break;
            }
            yield return null;
        }
    }

    private void StartProgress()
    {
        // show UI
        if (progressView != null) progressView.Show(0f);
        // start coroutine
        _progressCoroutine = StartCoroutine(ProgressRoutine());
    }

    private IEnumerator ProgressRoutine()
    {
        float elapsed = 0f;
        float total = Mathf.Max(0.01f, progressSecondsToFull);
        bool shakeStarted = false;

        while (elapsed < total)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / total);
            if (progressView != null) progressView.SetProgress(normalized);

            // update sprite at thresholds
            UpdateSpriteByProgress(normalized);

            // start shake at threshold
            if (!shakeStarted && normalized >= shakeStartPercent)
            {
                shakeStarted = true;
                // swap to shake sprite if exists
                SwapSpriteForStage(2); // index 2 = shake stage (convention)
                _shakeCoroutine = StartCoroutine(ShakeRoutine());
            }

            yield return null;
        }

        // reached 100%
        if (progressView != null) progressView.SetProgress(1f);

        // trigger game over
        Debug.Log("[ClienteController] Progress reached 100% -> GameOver triggered");
        var flow = ServiceLocator.Get<IGameFlowService>();
        if (flow != null)
            flow.TriggerGameOver();
        else
            Debug.LogWarning("[ClienteController] No IGameFlowService registered!");

        yield break;
    }

    private void UpdateSpriteByProgress(float normalized)
    {
        if (clienteData == null || clienteData.stageSprites == null || clienteData.stageSprites.Length == 0) return;

        // simple mapping: 0..0.5 -> sprite[0], 0.5..0.8 -> sprite[1], >=0.8 -> sprite[2] (shake stage)
        if (normalized < 0.5f)
            SwapSpriteForStage(0);
        else if (normalized < 0.8f)
            SwapSpriteForStage(1);
        else
            SwapSpriteForStage(2);
    }

    private void SwapSpriteForStage(int index)
    {
        if (clienteData == null || clienteData.stageSprites == null) return;
        if (index < 0 || index >= clienteData.stageSprites.Length) return;
        if (spriteRenderer != null && spriteRenderer.sprite != clienteData.stageSprites[index])
            spriteRenderer.sprite = clienteData.stageSprites[index];
    }

    private IEnumerator ShakeRoutine()
    {
        // small shake around _originPosition
        while (true)
        {
            float rx = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f) * 2f * shakeMagnitude;
            float ry = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f) * 2f * shakeMagnitude;
            transform.position = _originPosition + new Vector3(rx, ry, 0f);
            yield return null;
        }
    }

    // When dialog finishes and manager orders the client to leave, we must stop progress and shake
    public void CancelProgressAndLeave(Transform exitPoint, Action onFinish = null)
    {
        if (_progressCoroutine != null)
        {
            StopCoroutine(_progressCoroutine);
            _progressCoroutine = null;
        }
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
            // restore exact origin position
            transform.position = _originPosition;
        }

        if (progressView != null) progressView.Hide();

        Leave(exitPoint, onFinish);
    }

    public void Leave(Transform exitPoint, Action onFinish = null)
    {
        if (exitPoint == null)
        {
            Debug.LogWarning("Leave: exitPoint null");
            onFinish?.Invoke();
            Destroy(gameObject);
            return;
        }

        // ensure progress coroutines stopped
        if (_progressCoroutine != null)
        {
            StopCoroutine(_progressCoroutine);
            _progressCoroutine = null;
        }
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
            transform.position = _originPosition;
        }

        if (progressView != null) progressView.Hide();
        StartCoroutine(LeaveCoroutine(exitPoint, onFinish));
    }

    private IEnumerator LeaveCoroutine(Transform exitPoint, Action onFinish)
    {
        while (true)
        {
            transform.position = Vector3.MoveTowards(transform.position, exitPoint.position, (moveSpeed * 1.2f) * Time.deltaTime);
            if (Vector3.Distance(transform.position, exitPoint.position) <= 0.02f)
            {
                transform.position = exitPoint.position;
                OnLeftScene?.Invoke();
                onFinish?.Invoke();
                Destroy(gameObject);
                yield break;
            }
            yield return null;
        }
    }
}
