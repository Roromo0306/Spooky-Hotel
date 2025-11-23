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

    [Header("Mensajes de resultado")]
    [TextArea(1, 3)] public string msg0Aciertos = "Lo has hecho muy mal.";
    [TextArea(1, 3)] public string msg1a2Aciertos = "Los clientes están enfadados.";
    [TextArea(1, 3)] public string msg3a4Aciertos = "Cuida la atención al cliente.";
    [TextArea(1, 3)] public string msg5plusAciertos = "¡Enhorabuena!";

    [Header("Audio")]
    [Tooltip("AudioSource que reproducirá los sonidos del puzzle (asignar/cerrar)")]
    public AudioSource audioSource;
    [Tooltip("Clip que sonará cuando se asigne correctamente un cliente")]
    public AudioClip assignClip;
    [Tooltip("Clip que sonará al cerrar el puzzle")]
    public AudioClip closeClip;

    [Header("Dialogo")]
    public DialogController dialogController;   // para mostrar diálogos tras asignar

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

        // Intentar autoconfigurar dialogController si no está asignado
        if (dialogController == null)
        {
            dialogController = FindObjectOfType<DialogController>();
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

        // allowedIndices ahora solo se usan para highlight (no para bloquear)
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

        // Solo para feedback visual
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

        // Permitir soltar en cualquier celda vacía dentro de rango
        if (index < 0 || index >= PuzzleModel.CellCount)
        {
            dragged.Revert();
            _pendingPlacementIndex = null;
            return;
        }

        if (_model.Cells[index] != null)
        {
            // Ya hay alguien asignado a esa celda → no permitimos sobrescribir
            Debug.LogWarning($"HandleDrop: celda {index} ya ocupada.");
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
        var clienteData = _currentDraggable.data;

        // Determinar si la casilla es correcta según el ScriptableObject del cliente
        bool isCorrectPlacement = clienteData != null && clienteData.IsCorrectCell(placedIndex);

        // Feedback de diálogos
        if (isCorrectPlacement)
        {
            // sonido + diálogo de éxito
            PlayAssignSound();
            ShowSuccessDialogForCurrentClient();
        }
        else
        {
            // diálogo de fallo
            ShowFailDialogForCurrentClient();
        }

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

        // 8) CERRAR PANEL TRAS ASIGNAR
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

    // ------------------- CÁLCULO DE PUNTOS -------------------

    private void ComputeScore(out int satisfied, out int totalPlaced)
    {
        satisfied = 0;
        totalPlaced = 0;

        for (int i = 0; i < PuzzleModel.CellCount; i++)
        {
            var c = _model.Cells[i];
            if (c == null) continue;

            totalPlaced++;

            // suma punto solo si está en una de sus casillas correctas
            if (c.IsCorrectCell(i))
                satisfied++;
        }
    }

    private IEnumerator EndPuzzleRoutine()
    {
        var fade = FindObjectOfType<FadeController>();

        // Fade out inicial (si tienes FadeController)
        if (fade != null)
            yield return fade.FadeOutCoroutine();

        // ✅ calculamos puntos en función de las casillas correctas de cada cliente
        int satisfied;
        int totalPlaced;
        ComputeScore(out satisfied, out totalPlaced);

        Debug.Log($"[PuzzleController] FIN PUZZLE -> Clientes satisfechos {satisfied} / {totalPlaced}");

        // ⏳ Esperamos 5 segundos antes de mostrar la pantalla de resultados
        yield return new WaitForSeconds(5f);

        // Opcional: fade in del resto de la escena si usas FadeController
        if (fade != null)
            yield return fade.FadeInCoroutine();

        // ✅ Mensaje según el número de aciertos (editable en el inspector)
        string moodText;
        if (satisfied <= 0)
        {
            moodText = msg0Aciertos;
        }
        else if (satisfied <= 2)
        {
            moodText = msg1a2Aciertos;
        }
        else if (satisfied <= 4)
        {
            moodText = msg3a4Aciertos;
        }
        else // 5 o más
        {
            moodText = msg5plusAciertos;
        }

        if (resultScreenView != null)
        {
            string title = "Resultados de la partida";
            string summary =
                $"Clientes satisfechos: {satisfied} / {totalPlaced}\n\n" +
                moodText;

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

    // ------------ DIÁLOGO TRAS ASIGNAR BIEN ------------

    private void ShowSuccessDialogForCurrentClient()
    {
        if (dialogController == null)
        {
            dialogController = FindObjectOfType<DialogController>();
            if (dialogController == null)
            {
                Debug.LogWarning("[PuzzleController] No DialogController found to show success dialog.");
                return;
            }
        }

        // Intentamos sacar el ClienteSO desde el cliente del mundo, y si no desde el draggable
        ClienteSO data = null;

        if (_currentClient != null && _currentClient.clienteData != null)
        {
            data = _currentClient.clienteData;
        }
        else if (_currentDraggable != null && _currentDraggable.data != null)
        {
            data = _currentDraggable.data;
        }

        if (data == null)
        {
            Debug.LogWarning("[PuzzleController] ShowSuccessDialogForCurrentClient: ClienteSO es null.");
            return;
        }

        if (data.dialogosPuzzleExito == null || data.dialogosPuzzleExito.Length == 0)
        {
            // No tiene diálogos configurados, no pasa nada
            return;
        }

        dialogController.ShowDialog(data.dialogosPuzzleExito, data.nombre);
    }

    // ------------ DIÁLOGO TRAS ASIGNAR MAL ------------

    private void ShowFailDialogForCurrentClient()
    {
        if (dialogController == null)
        {
            dialogController = FindObjectOfType<DialogController>();
            if (dialogController == null)
            {
                Debug.LogWarning("[PuzzleController] No DialogController found to show fail dialog.");
                return;
            }
        }

        ClienteSO data = null;

        if (_currentClient != null && _currentClient.clienteData != null)
        {
            data = _currentClient.clienteData;
        }
        else if (_currentDraggable != null && _currentDraggable.data != null)
        {
            data = _currentDraggable.data;
        }

        if (data == null)
        {
            Debug.LogWarning("[PuzzleController] ShowFailDialogForCurrentClient: ClienteSO es null.");
            return;
        }

        if (data.dialogosPuzzleFallo == null || data.dialogosPuzzleFallo.Length == 0)
        {
            // No tiene diálogos de fallo configurados, no pasa nada
            return;
        }

        dialogController.ShowDialog(data.dialogosPuzzleFallo, data.nombre);
    }
}
