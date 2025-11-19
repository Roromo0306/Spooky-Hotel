using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameScene.Puzzle;

public class PuzzleController : MonoBehaviour
{
    [Header("View")]
    public PuzzleView view;

    [Header("Dependencies")]
    public MonoBehaviour puzzleServiceBehaviour;
    private IPuzzleService _puzzleService;

    [Header("Prefabs")]
    public GameObject draggablePrefab;

    [Header("Config")]
    public ClienteSO[] spawnOrder;
    private int _spawnIndex = 0;

    [Header("Solutions")]
    public PuzzleSolutionsSO solutionsSO;

    [Header("Destino de clientes (UI Panel en Canvas)")]
    public RectTransform counterArea; // panel donde aparecen los clientes

    private PuzzleModel _model;
    private List<DraggableCharacterView> _activeDraggables = new List<DraggableCharacterView>();
    private CellView[] _cells;

    private DraggableCharacterView _currentDraggable = null;
    private int? _pendingPlacementIndex = null;
    private bool[] _currentAllowedIndices = null;

    private void Awake()
    {
        // Configurar servicio de puzzle
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
        view.closeButton.onClick.AddListener(() => view.Hide());

        view.Show();

        SpawnNextCharacter();
    }

    private void OnDestroy()
    {
        foreach (var c in _cells) c.OnDropped -= HandleDrop;
        view.assignButton.onClick.RemoveAllListeners();
        view.closeButton.onClick.RemoveAllListeners();
    }

    private void Update()
    {
        // Mostrar draggable cuando "llega" al mostrador (simulado)
        if (_currentDraggable != null && !_currentDraggable.gameObject.activeSelf)
        {
            ShowDraggableAtCounter();
        }
    }

    private void SpawnNextCharacter()
    {
        if (_spawnIndex >= spawnOrder.Length)
        {
            Debug.Log("[PuzzleController] No more characters to spawn.");
            _currentDraggable = null;
            _currentAllowedIndices = null;
            view.gridView.ClearAllowedMarks();
            return;
        }

        var so = spawnOrder[_spawnIndex++];

        // Instanciar prefab dentro del spawnArea en Canvas
        var go = Instantiate(draggablePrefab, view.spawnArea);
        go.transform.localScale = Vector3.one;
        go.SetActive(false); // oculto hasta que llegue al mostrador

        var drag = go.GetComponent<DraggableCharacterView>();
        drag.data = so;

        // Asignar sprite e icono 120x120
        var img = go.GetComponentInChildren<Image>();
        if (img != null && so.icon != null)
        {
            img.sprite = so.icon;
            img.rectTransform.sizeDelta = new Vector2(120, 120);
        }

        _activeDraggables.Add(drag);
        _currentDraggable = drag;
        _pendingPlacementIndex = null;
        _currentAllowedIndices = _puzzleService.GetAllowedIndices(so, _model);
    }

    private void ShowDraggableAtCounter()
    {
        if (_currentDraggable == null) return;

        _currentDraggable.gameObject.SetActive(true);
        _currentDraggable.transform.SetParent(counterArea, false);
        _currentDraggable.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

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

    private void OnAssignClicked()
    {
        if (_currentDraggable == null || _pendingPlacementIndex == null) return;

        int placedIndex = _pendingPlacementIndex.Value;
        string reason;

        if (_puzzleService.ValidatePlacement(_currentDraggable.data, placedIndex, _model, out reason))
        {
            _model.PlaceAt(placedIndex, _currentDraggable.data);

            // Cliente se va
            Destroy(_currentDraggable.gameObject);
            _activeDraggables.Remove(_currentDraggable);

            _pendingPlacementIndex = null;
            _currentDraggable = null;
            _currentAllowedIndices = null;
            view.gridView.ClearAllowedMarks();

            SpawnNextCharacter();

            if (CountAssigned() >= 5) StartCoroutine(EndPuzzleRoutine());
        }
        else
        {
            Debug.LogWarning($"Placement rejected for {_currentDraggable.data.nombre} at {placedIndex}: {reason}");
            _currentDraggable.Revert();
            _pendingPlacementIndex = null;
        }
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
        if (fade != null) yield return fade.FadeOutCoroutine();

        var result = _puzzleService.EvaluateFinal(_model, solutionsSO);
        Debug.Log($"Clientes satisfechos {result.satisfied}/{result.totalPlaced}");

        if (fade != null) yield return fade.FadeInCoroutine();
    }
}




