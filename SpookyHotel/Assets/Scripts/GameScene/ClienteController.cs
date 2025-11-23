using System;
using System.Collections;
using UnityEngine;
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
    public ProgressBarView progressView;
    [SerializeField][Range(0.1f, 120f)] private float progressSecondsToFull = 30f;
    [Range(0.5f, 1f)] public float shakeStartPercent = 0.8f;
    public float shakeMagnitude = 0.08f;
    public float shakeSpeed = 10f;

    [Header("Speaking pulse")]
    public float speakPulseAmplitude = 0.05f;
    public float speakPulseFrequency = 4f;

    private Transform _target;
    private bool _isMoving = false;

    private Vector3 _originPosition;
    private Coroutine _progressCoroutine;
    private Coroutine _shakeCoroutine;
    private Coroutine _speakPulseCoroutine;
    private Vector3 _originalScale;

    public event Action OnReachedDestination;
    public event Action OnLeftScene;

    private const float ArrivalEpsilon = 0.02f;

    private DialogController _dialogController; // cache para desuscribir

    private void Awake()
    {
        _originalScale = transform.localScale;
        Reset();
    }

    private void Reset()
    {
        progressSecondsToFull = 30f;
    }

    public void Initialize(ClienteSO data)
    {
        clienteData = data;
        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (clienteData == null) return;

        if (spriteRenderer != null && clienteData.stageSprites.Length > 0)
            spriteRenderer.sprite = clienteData.stageSprites[0];

        if (nameTMP != null)
            nameTMP.text = clienteData.nombre;
    }

    public void MoveTo(Transform target)
    {
        if (target == null) return;

        _target = target;
        _isMoving = true;

        // Solo detenemos corutinas de ESTE cliente
        StopAllCoroutines();
        StartCoroutine(MoveCoroutine());
    }

    private IEnumerator MoveCoroutine()
    {
        while (_isMoving && _target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, _target.position, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _target.position) <= ArrivalEpsilon)
            {
                transform.position = _target.position;
                _isMoving = false;
                _originPosition = transform.position;

                Debug.Log("[ClienteController] Cliente llegó a DESTINO -> " +
                          (clienteData != null ? clienteData.nombre : "sin nombre"));

                // 1) Evento normal
                OnReachedDestination?.Invoke();

                // 2) Avisar explícitamente al manager de que este cliente ha llegado
                var manager = FindObjectOfType<ClienteManagerController>();
                if (manager != null)
                {
                    manager.NotifyClienteReachedFromClient(this);
                }
                else
                {
                    Debug.LogWarning("[ClienteController] No se encontró ClienteManagerController al llegar al destino.");
                }

                // Auto-registrar al PuzzleController
                var pc = FindObjectOfType<PuzzleController>();
                if (pc != null) pc.RegisterCurrentClient(this);

                // Empezar barra de progreso
                StartProgress();

                // MOSTRAR DIÁLOGO
                TryShowDialogForThisClient();

                yield break;
            }

            yield return null;
        }
    }

    private void TryShowDialogForThisClient()
    {
        if (clienteData == null || clienteData.dialogos == null || clienteData.dialogos.Length == 0)
        {
            Debug.LogWarning("[ClienteController] Cliente " +
                             (clienteData != null ? clienteData.nombre : "null") +
                             " no tiene diálogos definidos.");
            return;
        }

        _dialogController = FindObjectOfType<DialogController>();
        if (_dialogController == null)
        {
            Debug.LogError("[ClienteController] No se encontró DialogController en la escena.");
            return;
        }

        _dialogController.OnTypingStarted += HandleTypingStartedLocal;
        _dialogController.OnTypingEnded += HandleTypingEndedLocal;
        _dialogController.OnDialogFinished += HandleDialogFinishedLocal;

        Debug.Log("[ClienteController] Mostrando diálogo de " + clienteData.nombre +
                  " con " + clienteData.dialogos.Length + " líneas.");

        _dialogController.ShowDialog(clienteData.dialogos, clienteData.nombre);
    }

    private void StartProgress()
    {
        if (progressView == null) progressView = FindObjectOfType<ProgressBarView>();
        if (progressView != null) progressView.Show(0f);
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

            if (!shakeStarted && normalized >= shakeStartPercent)
            {
                shakeStarted = true;
                _shakeCoroutine = StartCoroutine(ShakeRoutine());
            }

            yield return null;
        }

        if (progressView != null) progressView.SetProgress(1f);

        var flow = ServiceLocator.Get<IGameFlowService>();
        if (flow != null) flow.TriggerGameOver();
    }

    private IEnumerator ShakeRoutine()
    {
        while (true)
        {
            float rx = (UnityEngine.Random.value - 0.5f) * 2f * shakeMagnitude;
            float ry = (UnityEngine.Random.value - 0.5f) * 2f * shakeMagnitude;
            transform.position = _originPosition + new Vector3(rx, ry, 0f);
            yield return null;
        }
    }

    public void CancelProgressAndLeave(Transform exitPoint, Action onFinish = null)
    {
        if (_progressCoroutine != null) StopCoroutine(_progressCoroutine);
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        StopSpeakingPulse();
        if (progressView != null) progressView.Hide();
        Leave(exitPoint, onFinish);
    }

    public void Leave(Transform exitPoint, Action onFinish = null)
    {
        StartCoroutine(LeaveCoroutine(exitPoint, onFinish));
    }

    private IEnumerator LeaveCoroutine(Transform exitPoint, Action onFinish)
    {
        while (true)
        {
            transform.position = Vector3.MoveTowards(transform.position, exitPoint.position, moveSpeed * 1.2f * Time.deltaTime);
            if (Vector3.Distance(transform.position, exitPoint.position) <= 0.02f)
            {
                transform.position = exitPoint.position;

                Debug.Log("[ClienteController] Cliente llegó al EXIT -> " +
                          (clienteData != null ? clienteData.nombre : "sin nombre"));

                // 1) evento normal
                OnLeftScene?.Invoke();

                // 2) avisar al manager para que limpie documentos SÍ O SÍ
                var manager = FindObjectOfType<ClienteManagerController>();
                if (manager != null)
                {
                    manager.OnClienteReallyLeft(this);
                }
                else
                {
                    Debug.LogWarning("[ClienteController] No se encontró ClienteManagerController al salir.");
                }

                // callback del manager (para spawnear siguiente)
                onFinish?.Invoke();

                CleanupDialogSubscriptions();
                Destroy(gameObject);
                yield break;
            }
            yield return null;
        }
    }

    // -------- SPEAKING PULSE --------

    public void StartSpeakingPulse()
    {
        if (_speakPulseCoroutine != null) return;
        _speakPulseCoroutine = StartCoroutine(SpeakPulseRoutine());
    }

    public void StopSpeakingPulse()
    {
        if (_speakPulseCoroutine != null) StopCoroutine(_speakPulseCoroutine);
        _speakPulseCoroutine = null;
        transform.localScale = _originalScale;
    }

    private IEnumerator SpeakPulseRoutine()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * speakPulseFrequency * Mathf.PI * 2f;
            transform.localScale = _originalScale * (1f + Mathf.Sin(t) * speakPulseAmplitude);
            yield return null;
        }
    }

    // -------- HANDLERS DE EVENTOS DE DIÁLOGO (por cliente) --------

    private void HandleTypingStartedLocal()
    {
        StartSpeakingPulse();
    }

    private void HandleTypingEndedLocal()
    {
        StopSpeakingPulse();
    }

    private void HandleDialogFinishedLocal()
    {
        StopSpeakingPulse();
        CleanupDialogSubscriptions();
    }

    private void CleanupDialogSubscriptions()
    {
        if (_dialogController != null)
        {
            _dialogController.OnTypingStarted -= HandleTypingStartedLocal;
            _dialogController.OnTypingEnded -= HandleTypingEndedLocal;
            _dialogController.OnDialogFinished -= HandleDialogFinishedLocal;
        }
    }

    private void OnDestroy()
    {
        CleanupDialogSubscriptions();
    }
}
