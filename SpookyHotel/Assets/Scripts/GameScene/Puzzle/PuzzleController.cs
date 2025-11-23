using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using GameScene.Puzzle;

public class PuzzleController : MonoBehaviour
{
    [Header("View")]
    public PuzzleView view;

    [Header("Dependencies")]
    public MonoBehaviour puzzleServiceBehaviour;
    private IPuzzleService _puzzleService;

    [Header("Prefabs (UI)")]
    public GameObject draggablePrefab;

    [Header("Config")]
    public ClienteSO[] spawnOrder;
    private int _spawnIndex = 0;

    [Header("Solutions")]
    public PuzzleSolutionsSO solutionsSO;

    [Header("Destino de clientes (UI Panel en Canvas)")]
    public RectTransform counterArea; // panel donde aparecen los clientes (UI)

    [Header("Punto donde aparece el draggable (UI)")]
    [Tooltip("Empty (RectTransform) en el Canvas donde quieres que aparezca el icono draggable")]
    public RectTransform draggableSpawnPoint;

    [Header("Mundo - cliente")]
    public ClienteController clienteWorldPrefab;   // prefab del cliente en el mundo (con ClienteController)
    public Transform worldSpawnPoint;             // donde instanciar nuevos clientes en el mundo
    public Transform worldCounterTransform;       // target en mundo donde moverse (mostrador)

    [Header("Salida del cliente (mundo)")]
    public Transform clientExitPoint; // punto en mundo 2D donde el cliente se marcha

    [Header("Behaviour")]
    [Tooltip("Si está activo, al asignar también se hará SpawnNextCharacter() del UI")]
    public bool autoSpawnNextUI = true;

    [Tooltip("Si está activo, PuzzleController intentará spawnear el primer cliente del mundo automáticamente usando spawnOrder.")]
    public bool autoSpawnFirstWorldClient = false;

    [Header("Eventos")]
    public UnityEvent onClientLeft; // conecta aquí tu sistema que 'spawnea' el siguiente cliente del mundo (opcional)

    [Header("Fin de partida")]
    public ResultScreenView resultScreenView; // pantalla de resultados (asignar en el Inspector)

    [Header("Audio")]
    [Tooltip("AudioSource que reproducirá los sonidos del puzzle (asignar/cerrar)")]
    public AudioSource audioSource;
    [Tooltip("Clip que sonará cuando se asigne correctamente un cliente")]
    public AudioClip assignClip;
    [Tooltip("Clip que sonará al cerrar el puzzle")]
    public AudioClip closeClip;

    private PuzzleModel _model;
    private List<DraggableCharacterView> _activeDraggables = new List<DraggableCharacterView>();
    private CellView[] _cells;

    // UI draggable/icon que representa al cliente actual
    private DraggableCharacterView _currentDraggable = null;
    private int? _pendingPlacementIndex = null;
    private bool[] _currentAllowedIndices = null;

    // Reference to the in-world client (behaviour)
    private ClienteController _currentClient = null;

    private void Awake()
    {
        if (puzzleServiceBehaviour != null && puzzleServiceBehaviour is IPuzzleService)
            _puzzleService = puzzleServiceBehaviour as IPuzzleService;
        else
            _puzzleService = new PuzzleService(new DefaultPlacementStrategy());

        _model = new PuzzleModel();

        if (view == null || view.gridView == null)
        {
            Debug.LogError("[PuzzleController] view o gridView no asignado.");
            return;
        }

        // Inicializar grid
        view.gridView.BuildGrid();
        _cells = view.gridView.Cells;
        foreach (var c in _cells) c.OnDropped += HandleDrop;

        // Botones
        view.assignButton.onClick.RemoveAllListeners();
        view.assignButton.onClick.AddListener(OnAssignClicked);

        view.closeButton.onClick.RemoveAllListeners();
        view.closeButton.onClick.AddListener(OnCloseClicked);

        // El puzzle no se muestra hasta que llamemos a OpenPuzzle()
        // view.Show();

        // UI: crear primera ficha (consume spawnOrder[0])
        SpawnNextCharacter();

        // Opcional: auto spawnear primer cliente mundo usando spawnOrder
        if (autoSpawnFirstWorldClient && clienteWorldPrefab != null && worldSpawnPoint != null && worldCounterTransform != null)
        {
            var so = PeekNextClienteSO(); // NO consume
            if (so != null)
            {
                TrySpawnNextWorldClientFromCurrentUI(so);
                _spawnIndex = Mathf.Min(_spawnIndex + 1, spawnOrder.Length);
            }
        }
    }

    private void OnDestroy()
    {
        if (_cells != null)
        {
            foreach (var c in _cells) c.OnDropped -= HandleDrop;
        }
        view.assignButton.onClick.RemoveAllListeners();
        view.closeButton.onClick.RemoveAllListeners();
    }

    private void Update()
    {
        // Mostrar draggable cuando "llega" al mostrador (si tu flujo lo requiere)
        if (_currentDraggable != null && !_currentDraggable.gameObject.activeSelf)
        {
            ShowDraggableAtCounter();
        }
    }

    /// <summary>
    /// Registra el ClienteController del mundo como "cliente actual".
    /// Llama a este método desde tu spawner o cuando crees/asignes el cliente.
    /// </summary>
    public void RegisterCurrentClient(ClienteController client)
    {
        _currentClient = client;
        if (_currentClient != null)
        {
            Debug.Log("[PuzzleController] Registered current client: " + client.name + " pos=" + client.transform.position);
            _currentClient.OnLeftScene += HandleClientLeftScene;
        }
    }

    private void HandleClientLeftScene()
    {
        if (_currentClient != null)
            _currentClient.OnLeftScene -= HandleClientLeftScene;
        _currentClient = null;
    }

    private void SpawnNextCharacter()
    {
        if (spawnOrder == null)
        {
            Debug.LogWarning("[PuzzleController] SpawnNextCharacter: spawnOrder es null.");
            return;
        }

        if (_spawnIndex >= spawnOrder.Length)
        {
            Debug.Log("[PuzzleController] No more characters to spawn.");
            _currentDraggable = null;
            _currentAllowedIndices = null;
            view.gridView.ClearAllowedMarks();
            return;
        }

        var so = spawnOrder[_spawnIndex++];

        // Instanciar prefab dentro del spawnArea (luego lo moveremos en ShowDraggableAtCounter)
        var go = Instantiate(draggablePrefab, view.spawnArea);
        go.transform.localScale = Vector3.one;
        go.SetActive(false); // oculto hasta que lo mostremos en el punto deseado

        var drag = go.GetComponent<DraggableCharacterView>();
        if (drag == null)
        {
            Debug.LogWarning("[PuzzleController] SpawnNextCharacter: DraggableCharacterView no encontrado en prefab.");
            drag = go.AddComponent<DraggableCharacterView>();
        }
        drag.data = so;

        // Asignar sprite e icono 120x120
        var img = go.GetComponentInChildren<Image>(true);
        if (img != null && so.icon != null)
        {
            img.sprite = so.icon;
            img.rectTransform.sizeDelta = new Vector2(120, 120);
            img.preserveAspect = true;
            img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
        }
        else if (img == null)
        {
            Debug.LogWarning("[PuzzleController] SpawnNextCharacter: no Image encontrado en prefab.");
        }
        else
        {
            Debug.LogWarning($"[PuzzleController] SpawnNextCharacter: ClienteSO.icon es NULL para {so?.nombre}");
        }

        _activeDraggables.Add(drag);
        _currentDraggable = drag;
        _pendingPlacementIndex = null;
        _currentAllowedIndices = _puzzleService.GetAllowedIndices(so, _model);
    }

    /// <summary>
    /// Muestra el draggable en el punto específico del Canvas (empty RectTransform).
    /// </summary>
    private void ShowDraggableAtCounter()
    {
        if (_currentDraggable == null) return;

        _currentDraggable.gameObject.SetActive(true);

        // 👉 Si se ha asignado draggableSpawnPoint, usamos ese.
        // Si no, usamos counterArea como fallback.
        RectTransform parent = draggableSpawnPoint != null ? draggableSpawnPoint : counterArea;

        if (parent != null)
        {
            _currentDraggable.transform.SetParent(parent, false);
            var rt = _currentDraggable.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = Vector2.zero;
        }
        else
        {
            Debug.LogWarning("[PuzzleController] ShowDraggableAtCounter: no hay parent (draggableSpawnPoint ni counterArea).");
        }

        view.gridView.SetAllowedIndices(_currentAllowedIndices);
    }

    private void HandleDrop(int index, DraggableCharacterView dragged)
    {
        if (dragged != _currentDraggable)
        {
            dragged.Revert();
            _pendingPlacementIndex = null;
            view.gridView.ClearAllowedMarks();
            return;
        }

        if (_currentAllowedIndices == null || index < 0 || index >= _currentAllowedIndices.Length || !_currentAllowedIndices[index])
        {
            string reason;
            _puzzleService.ValidatePlacement(dragged.data, index, _model, out reason);
            Debug.LogWarning($"Placement not allowed for {dragged.data.nombre} at {index}: {reason}");
            dragged.Revert();
            _pendingPlacementIndex = null;
            return;
        }

        _pendingPlacementIndex = index;
        var cell = _cells[index];
        dragged.transform.SetParent(cell.contentParent, false);
        dragged.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Helper: devuelve el siguiente ClienteSO en spawnOrder sin incrementar el índice.
    /// </summary>
    private ClienteSO PeekNextClienteSO()
    {
        if (spawnOrder == null) return null;
        if (_spawnIndex >= spawnOrder.Length) return null;
        return spawnOrder[_spawnIndex]; // NO incrementa
    }

    // ---------------- ASIGNAR ----------------

    public void OnAssignClicked()
    {
        // quick auto-register fallback: si no hay cliente registrado intentamos encontrar uno en el counter
        if (_currentClient == null)
        {
            if (worldCounterTransform != null)
            {
                var clients = FindObjectsOfType<ClienteController>();
                ClienteController closest = null;
                float bestDist = float.MaxValue;
                foreach (var c in clients)
                {
                    float d = Vector3.Distance(c.transform.position, worldCounterTransform.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        closest = c;
                    }
                }
                if (closest != null && bestDist <= 0.6f)
                {
                    RegisterCurrentClient(closest);
                    Debug.Log($"[PuzzleController] Auto-registered closest client at start of Assign: {closest.name} (dist {bestDist:F2})");
                }
            }
        }

        // guards
        if (_currentDraggable == null || _pendingPlacementIndex == null)
        {
            Debug.Log("[PuzzleController] OnAssignClicked: nothing to assign.");
            return;
        }

        int placedIndex = _pendingPlacementIndex.Value;
        string reason;

        if (!_puzzleService.ValidatePlacement(_currentDraggable.data, placedIndex, _model, out reason))
        {
            Debug.LogWarning($"(assign) Placement rejected for {_currentDraggable.data.nombre} at {placedIndex}: {reason}");
            _currentDraggable.Revert();
            _pendingPlacementIndex = null;
            return;
        }

        // ✅ Asignación válida → reproducir sonido de asignar
        PlayAssignSound();

        // 1) registrar en el modelo (el icono UI ya está en la celda)
        _model.PlaceAt(placedIndex, _currentDraggable.data);

        // 2) limpieza UI inmediata (la ficha queda visible en la celda)
        _activeDraggables.Remove(_currentDraggable);
        view.gridView.ClearAllowedMarks();

        // 3) PREPARAR datos para spawn del siguiente cliente en mundo SIN depender de _currentDraggable.
        ClienteSO nextSo = PeekNextClienteSO();
        Debug.Log($"[PuzzleController] Next ClienteSO peek = {(nextSo != null ? nextSo.nombre : "NULL (end of list)")}");

        // 4) reset pending & allowed (la UI actual ya está colocada)
        _pendingPlacementIndex = null;
        _currentAllowedIndices = null;

        // 5) Guardamos localmente el cliente que tiene que irse para no perder la referencia
        var clientToLeave = _currentClient;

        if (clientToLeave != null)
            Debug.Log($"[PuzzleController] Will ask client '{clientToLeave.name}' to leave to exitPoint={(clientExitPoint != null ? clientExitPoint.name : "NULL")}");
        else
            Debug.Log("[PuzzleController] No world client to ask to leave.");

        // 6) Si hay cliente en mundo, pedir que se vaya. Usamos Leave() y callback.
        if (clientToLeave != null)
        {
            clientToLeave.Leave(clientExitPoint, () =>
            {
                Debug.Log("[PuzzleController] clientToLeave callback: finished leaving.");

                onClientLeft?.Invoke();

                if (nextSo != null)
                {
                    TrySpawnNextWorldClientFromCurrentUI(nextSo);
                }
                else
                {
                    TrySpawnNextWorldClientFromCurrentUI(null);
                }

                if (autoSpawnNextUI)
                {
                    SpawnNextCharacter();
                }

                if (_currentClient == clientToLeave)
                {
                    _currentClient.OnLeftScene -= HandleClientLeftScene;
                    _currentClient = null;
                }
            });
        }
        else
        {
            onClientLeft?.Invoke();

            if (nextSo != null)
            {
                TrySpawnNextWorldClientFromCurrentUI(nextSo);
            }
            else
            {
                TrySpawnNextWorldClientFromCurrentUI(null);
            }

            if (autoSpawnNextUI)
            {
                SpawnNextCharacter();
            }
        }

        // 7) comprobar fin de puzzle (ajusta 5 si tienes otro número de clientes)
        if (CountAssigned() >= 5)
            StartCoroutine(EndPuzzleRoutine());

        // 8) 🔴 CERRAR PANEL TRAS ASIGNAR
        if (view != null)
        {
            PlayCloseSound();   // reutilizamos el mismo sonido que cerrar
            view.Hide();
        }
    }

    // ---------------- SPAWN CLIENTE MUNDO ----------------

    private void TrySpawnNextWorldClientFromCurrentUI(ClienteSO so)
    {
        if (clienteWorldPrefab == null || worldSpawnPoint == null || worldCounterTransform == null)
        {
            Debug.Log("[PuzzleController] TrySpawnNextWorldClientFromCurrentUI: falta prefab o puntos, no se creará cliente mundo.");
            return;
        }

        ClienteSO useSo = so;
        if (useSo == null)
        {
            if (_currentDraggable != null)
            {
                useSo = _currentDraggable.data;
            }
            else
            {
                Debug.Log("[PuzzleController] TrySpawnNextWorldClientFromCurrentUI: no hay draggable UI actual ni ClienteSO pasado.");
                return;
            }
        }

        if (useSo == null)
        {
            Debug.LogWarning("[PuzzleController] TrySpawnNextWorldClientFromCurrentUI: ClienteSO resultó null.");
            return;
        }

        var go = Instantiate(clienteWorldPrefab.gameObject, worldSpawnPoint.position, Quaternion.identity);
        var cliente = go.GetComponent<ClienteController>();
        if (cliente == null)
        {
            Debug.LogError("[PuzzleController] TrySpawnNextWorldClientFromCurrentUI: prefab no contiene ClienteController.");
            Destroy(go);
            return;
        }

        cliente.Initialize(useSo);
        cliente.MoveTo(worldCounterTransform);
        RegisterCurrentClient(cliente);

        Debug.Log($"[PuzzleController] Spawned next world client for {useSo.nombre}");
    }

    private int CountAssigned()
    {
        int count = 0;
        for (int i = 0; i < PuzzleModel.CellCount; i++)
            if (_model.Cells[i] != null) count++;
        return count;
    }

    private IEnumerator EndPuzzleRoutine()
    {
        var fade = FindObjectOfType<FadeController>();

        // Fade out inicial (si tienes FadeController)
        if (fade != null)
            yield return fade.FadeOutCoroutine();

        var result = _puzzleService.EvaluateFinal(_model, solutionsSO);
        Debug.Log($"Clientes satisfechos {result.satisfied}/{result.totalPlaced}");

        // ⏳ Esperamos 5 segundos antes de mostrar la pantalla de resultados
        yield return new WaitForSeconds(5f);

        // Opcional: fade in del resto de la escena si usas FadeController
        if (fade != null)
            yield return fade.FadeInCoroutine();

        // Mostrar pantalla de resultados con fade in propio
        if (resultScreenView != null)
        {
            string title = "Resultados de la partida";
            string summary =
                $"Clientes satisfechos: {result.satisfied} / {result.totalPlaced}\n\n" +
                "¡Gracias por jugar!";

            resultScreenView.ShowResultsWithFade(title, summary, 1f);
        }
        else
        {
            Debug.LogWarning("[PuzzleController] resultScreenView no asignado en el inspector, no puedo mostrar resultados.");
        }
    }

    // ------------ NUEVO: abrir puzzle desde fuera ------------

    /// <summary>
    /// Muestra el panel del puzzle. Llamar desde un objeto del mundo (por ejemplo, al hacer click).
    /// </summary>
    public void OpenPuzzle()
    {
        if (view == null)
        {
            Debug.LogError("[PuzzleController] OpenPuzzle llamado pero view es null.");
            return;
        }

        view.Show();
    }

    // ------------ CERRAR + AUDIO ------------

    private void OnCloseClicked()
    {
        PlayCloseSound();
        if (view != null)
            view.Hide();
    }

    private void PlayAssignSound()
    {
        if (audioSource != null && assignClip != null)
        {
            audioSource.PlayOneShot(assignClip);
        }
    }

    private void PlayCloseSound()
    {
        if (audioSource != null && closeClip != null)
        {
            audioSource.PlayOneShot(closeClip);
        }
    }
}
