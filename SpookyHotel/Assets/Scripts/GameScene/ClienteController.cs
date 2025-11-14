using System;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Control del cliente (View/Behaviour).
/// Soporta: Initialize(ClienteSO), MoveTo(target), llegada->StartProgress(),
/// CancelProgressAndLeave(exitPoint), Leave(exitPoint).
/// Además StartSpeakingPulse/StopSpeakingPulse para el pulso de escala mientras se escribe.
/// </summary>
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
    public ProgressBarView progressView;            // referencia (puede ser shared por el manager)
    [Range(0.1f, 120f)] public float progressSecondsToFull = 10f;
    [Range(0.5f, 1f)] public float shakeStartPercent = 0.8f;
    public float shakeMagnitude = 0.08f;
    public float shakeSpeed = 10f;

    [Header("Speaking pulse")]
    [Tooltip("Amplitud de pulso (ej. 0.05 = ±5% en escala)")]
    public float speakPulseAmplitude = 0.05f;
    [Tooltip("Frecuencia del pulso")]
    public float speakPulseFrequency = 4f;

    // internals
    private Transform _target;
    private bool _isMoving = false;
    private bool _isLeaving = false;

    private Vector3 _originPosition;
    private Coroutine _progressCoroutine;
    private Coroutine _shakeCoroutine;
    private Coroutine _speakPulseCoroutine;

    private Vector3 _originalScale;

    public event Action OnReachedDestination;
    public event Action OnLeftScene;

    private const float ArrivalEpsilon = 0.02f;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    #region Initialization & visuals
    public void Initialize(ClienteSO data)
    {
        clienteData = data;
        ApplyVisuals();
        Debug.Log($"[ClienteController] Initialize cliente '{data?.nombre}' ");
    }

    private void ApplyVisuals()
    {
        if (clienteData == null) return;

        if (spriteRenderer != null && clienteData.stageSprites != null && clienteData.stageSprites.Length > 0)
        {
            spriteRenderer.sprite = clienteData.stageSprites[0];
        }

        if (nameTMP != null && !string.IsNullOrEmpty(clienteData.nombre))
        {
            nameTMP.text = clienteData.nombre;
        }
    }
    #endregion

    #region Movement
    public void MoveTo(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("[ClienteController] MoveTo: target null");
            return;
        }

        _target = target;
        _isMoving = true;
        _isLeaving = false;
        StopAllCoroutines();
        StartCoroutine(MoveCoroutine());
        Debug.Log($"[ClienteController] MoveTo -> {target.name}");
    }

    private IEnumerator MoveCoroutine()
    {
        while (_isMoving && _target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, _target.position, moveSpeed * Time.deltaTime);
            float dist = Vector3.Distance(transform.position, _target.position);
            if (dist <= ArrivalEpsilon)
            {
                transform.position = _target.position;
                _isMoving = false;
                Debug.Log("[ClienteController] Reached destination");
                _originPosition = transform.position;
                OnReachedDestination?.Invoke();
                StartProgress();
                yield break;
            }
            yield return null;
        }
    }
    #endregion

    #region Progress / shake
    private void StartProgress()
    {
        // fallback: buscar en escena si no asignado
        if (progressView == null)
        {
            progressView = FindObjectOfType<ProgressBarView>();
            Debug.LogWarning("[ClienteController] progressView null -> FindObjectOfType returned: " + (progressView != null));
        }

        if (progressView != null) progressView.Show(0f);
        else Debug.LogWarning("[ClienteController] No ProgressBarView assigned or found.");

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

            UpdateSpriteByProgress(normalized);

            if (!shakeStarted && normalized >= shakeStartPercent)
            {
                shakeStarted = true;
                SwapSpriteForStage(2); // convention: stageSprites[2] = shake sprite
                _shakeCoroutine = StartCoroutine(ShakeRoutine());
            }

            yield return null;
        }

        if (progressView != null) progressView.SetProgress(1f);

        Debug.Log("[ClienteController] Progress reached 100% -> triggering GameOver");
        var flow = ServiceLocator.Get<IGameFlowService>();
        if (flow != null) flow.TriggerGameOver();
        else Debug.LogWarning("[ClienteController] IGameFlowService not registered.");
    }

    private void UpdateSpriteByProgress(float normalized)
    {
        if (clienteData == null || clienteData.stageSprites == null) return;
        if (clienteData.stageSprites.Length == 0) return;

        if (normalized < 0.5f) SwapSpriteForStage(0);
        else if (normalized < 0.8f) SwapSpriteForStage(1);
        else SwapSpriteForStage(2);
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
        while (true)
        {
            float rx = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f) * 2f * shakeMagnitude;
            float ry = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f) * 2f * shakeMagnitude;
            transform.position = _originPosition + new Vector3(rx, ry, 0f);
            yield return null;
        }
    }
    #endregion

    #region Speaking pulse (scale)
    public void StartSpeakingPulse()
    {
        if (_speakPulseCoroutine != null) return;
        _speakPulseCoroutine = StartCoroutine(SpeakPulseRoutine());
    }

    public void StopSpeakingPulse()
    {
        if (_speakPulseCoroutine != null)
        {
            StopCoroutine(_speakPulseCoroutine);
            _speakPulseCoroutine = null;
            transform.localScale = _originalScale;
        }
    }

    private IEnumerator SpeakPulseRoutine()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * speakPulseFrequency * Mathf.PI * 2f;
            float s = 1f + Mathf.Sin(t) * speakPulseAmplitude;
            transform.localScale = _originalScale * s;
            yield return null;
        }
    }
    #endregion

    #region Leave / cancel
    // Llamado cuando el diálogo termina y queremos que se vaya: cancela progreso y shake, y luego se va
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
            transform.position = _originPosition;
        }

        if (progressView != null) progressView.Hide();

        // detener speaking pulse si estaba activo
        StopSpeakingPulse();

        Leave(exitPoint, onFinish);
    }

    public void Leave(Transform exitPoint, Action onFinish = null)
    {
        if (exitPoint == null)
        {
            Debug.LogWarning("[ClienteController] Leave: exitPoint null");
            onFinish?.Invoke();
            Destroy(gameObject);
            return;
        }

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
        StopSpeakingPulse();
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
    #endregion
}
