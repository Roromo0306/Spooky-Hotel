using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class PuzzleController : MonoBehaviour
{
    [Header("View")]
    public PuzzleView view;

    [Header("Dependencies")]
    public MonoBehaviour puzzleServiceBehaviour; // optional; if null we create one
    private IPuzzleService _puzzleService;

    [Header("Prefabs")]
    public GameObject draggablePrefab;

    [Header("Config")]
    public ClienteSO[] spawnOrder; // queue
    private int _spawnIndex = 0;

    [Header("Solutions (drag PuzzleSolutionsSO here)")]
    public PuzzleSolutionsSO solutionsSO;

    private PuzzleModel _model;
    private List<DraggableCharacterView> _activeDraggables = new List<DraggableCharacterView>();
    private CellView[] _cells;

    // Keep last spawned draggable reference to compute allowed indices for it
    private DraggableCharacterView _currentDraggable = null;
    private bool[] _currentAllowedIndices = null;

    private void Awake()
    {
        if (puzzleServiceBehaviour != null && puzzleServiceBehaviour is IPuzzleService)
            _puzzleService = puzzleServiceBehaviour as IPuzzleService;
        else
            _puzzleService = new PuzzleService(new DefaultPlacementStrategy());

        _model = new PuzzleModel();
        if (view == null)
        {
            Debug.LogError("[PuzzleController] view not assigned.");
            return;
        }
        if (view.gridView == null)
        {
            Debug.LogError("[PuzzleController] view.gridView not assigned.");
            return;
        }

        view.gridView.BuildGrid();
        _cells = view.gridView.Cells;

        foreach (var c in _cells) c.OnDropped += HandleDrop;

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
        var go = Instantiate(draggablePrefab, view.spawnArea);
        var drag = go.GetComponent<DraggableCharacterView>();
        drag.data = so;
        var img = go.GetComponent<UnityEngine.UI.Image>();
        if (img != null && so.icon != null) img.sprite = so.icon;
        _activeDraggables.Add(drag);

        // compute allowed indices for this spawned character
        _currentDraggable = drag;
        _currentAllowedIndices = _puzzleService.GetAllowedIndices(so, _model);
        view.gridView.SetAllowedIndices(_currentAllowedIndices);
    }

    private void HandleDrop(int index, DraggableCharacterView dragged)
    {
        Debug.Log($"Dropped attempt {dragged.data.nombre} into cell {index}");

        // If the cell isn't allowed for the current draggable, reject: model doesn't change
        if (dragged != _currentDraggable)
        {
            // not the current spawned draggable? still ignore and revert
            Debug.Log("[PuzzleController] Dropped an older draggable or unknown item; rejecting.");
            // we will let the draggable revert in OnEndDrag (it will detect parent unchanged)
            return;
        }

        if (_currentAllowedIndices == null || index < 0 || index >= _currentAllowedIndices.Length || !_currentAllowedIndices[index])
        {
            Debug.LogWarning($"Placement not allowed for {dragged.data.nombre} at {index}");
            // Optional: show a UI message to the player with reason. Let's compute reason:
            string reason;
            _puzzleService.ValidatePlacement(dragged.data, index, _model, out reason); // will fill reason (even if false)
            Debug.Log("[PuzzleController] Rejected placement reason: " + reason);
            // revert draggable to spawn area (we move it back)
            dragged.transform.SetParent(view.spawnArea, false);
            return;
        }

        // if allowed: place into model (permanent until assignment confirmed)
        _model.PlaceAt(index, dragged.data);

        // move object into this cell visually (CellView already did SetParent)
        // Note: OnAssignClicked will finalize the assignment (destroy draggable and spawn next)
    }

    private void OnAssignClicked()
    {
        // Assign the last placed current draggable (if any)
        // We search if any cell currently hosts _currentDraggable.data
        if (_currentDraggable == null)
        {
            Debug.Log("[PuzzleController] No current draggable to assign.");
            return;
        }

        // find index where model has that data
        int placedIndex = _model.IndexOf(_currentDraggable.data);
        if (placedIndex < 0)
        {
            Debug.Log("[PuzzleController] Current draggable not placed in any cell yet.");
            return;
        }

        // validate again (should be allowed)
        string reason;
        if (_puzzleService.ValidatePlacement(_currentDraggable.data, placedIndex, _model, out reason))
        {
            // finalize: destroy UI draggable and remove from active list
            Destroy(_currentDraggable.gameObject);
            _activeDraggables.Remove(_currentDraggable);

            // clear allowed marks and spawn next
            view.gridView.ClearAllowedMarks();
            _currentDraggable = null;
            _currentAllowedIndices = null;

            SpawnNextCharacter();

            int assignedCount = CountAssigned();
            if (assignedCount >= 5)
            {
                StartCoroutine(EndPuzzleRoutine());
            }
        }
        else
        {
            Debug.LogWarning($"(assign) Placement rejected for {_currentDraggable.data.nombre} at {placedIndex}: {reason}");
            // revert placement
            _model.RemoveAt(placedIndex);
            _currentDraggable.transform.SetParent(view.spawnArea, false);
        }
    }

    private int CountAssigned()
    {
        int count = 0;
        for (int i = 0; i < PuzzleModel.CellCount; i++)
            if (_model.Cells[i] != null) count++;
        return count;
    }

    private System.Collections.IEnumerator EndPuzzleRoutine()
    {
        var fade = FindObjectOfType<FadeController>();
        if (fade != null) yield return fade.FadeOutCoroutine();

        var result = _puzzleService.EvaluateFinal(_model, solutionsSO);
        Debug.Log($"Clientes satisfechos {result.satisfied}/{result.totalPlaced}");
        // TODO: show result UI

        if (fade != null) yield return fade.FadeInCoroutine();
    }
}
