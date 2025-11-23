using Infrastructure.MVC;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ClienteManagerController : ControllerBase<ClienteManagerModel>
{
    [Header("Configuración")]
    [SerializeField] private ClienteSO[] clientesToSpawn;
    [SerializeField] private GameObject clientePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform destinationPoint;
    [SerializeField] private Transform exitPoint;

    [Header("Dialog UI")]
    [SerializeField] private DialogController dialogController;

    private ClienteController _activeCliente;
    public ProgressBarView sharedProgressView;

    [Header("Documentos")]
    [SerializeField] private DocumentViewer documentViewer;

    // (solo si usas la UI lista en otras partes)
    [SerializeField] private DocumentListView documentListView;

    [Header("World Documents (sobre la mesa)")]
    [SerializeField] private DocumentWorldView documentWorldPrefab;
    [SerializeField] private Transform dniSpawnPoint;
    [SerializeField] private Transform reservaSpawnPoint;

    private readonly List<DocumentWorldView> _spawnedWorldDocs = new List<DocumentWorldView>();

    [Header("End Game UI")]
    [SerializeField] private ResultScreenView resultScreenView;

    [TextArea(3, 6)]
    [SerializeField]
    private string defaultSummaryText =
        "¡Has terminado la jornada!\n\n(Después puedes sustituir este texto por estadísticas de la partida).";

    private int _currentClienteIndex = -1;

    protected override async Task OnStartController()
    {
        Model = new ClienteManagerModel();
        Model.SetQueue(clientesToSpawn);
        Model.StartProcessing();
        Model.Subscribe(OnModelChanged);

        _currentClienteIndex = -1;
        SpawnNextClienteIfAny();

        await Task.CompletedTask;
    }

    protected override void OnModelChange()
    {
        // No usado aún
    }

    protected override void OnDestroyController()
    {
        if (Model != null)
            Model.Unsubscribe(OnModelChanged);

        ClearSpawnedWorldDocs();
    }

    private void OnModelChanged()
    {
        // Placeholder para expansión futura
    }

    private void SpawnNextClienteIfAny()
    {
        _currentClienteIndex++;

        if (_currentClienteIndex >= clientesToSpawn.Length)
        {
            ShowEndGameResults();
            return;
        }

        ClienteSO data = clientesToSpawn[_currentClienteIndex];
        if (data == null)
        {
            ShowEndGameResults();
            return;
        }

        GameObject go = Instantiate(clientePrefab, spawnPoint.position, Quaternion.identity);
        _activeCliente = go.GetComponent<ClienteController>();
        if (_activeCliente == null)
        {
            Destroy(go);
            return;
        }

        if (_activeCliente.progressView == null && sharedProgressView != null)
            _activeCliente.progressView = sharedProgressView;

        _activeCliente.Initialize(data);
        _activeCliente.OnReachedDestination += HandleClienteReached;
        _activeCliente.OnLeftScene += HandleClienteLeft;

        _activeCliente.MoveTo(destinationPoint);
    }

    private void ShowEndGameResults()
    {
        if (resultScreenView == null) return;

        string title = "Resultados de la partida";
        string summary = defaultSummaryText;

        resultScreenView.ShowResults(title, summary);
    }

    private void HandleClienteReached()
    {
        if (_activeCliente == null || _activeCliente.clienteData == null) return;

        if (dialogController == null)
        {
            dialogController = FindObjectOfType<DialogController>();
            if (dialogController == null) return;
        }

        dialogController.OnLineAdvance += HandleDialogLineAdvance;
        dialogController.OnDialogFinished += HandleDialogFinishedByEnter;

        var cdata = _activeCliente.clienteData;

        // ✅ Spawnear DNI y Reserva sobre la mesa
        SpawnWorldDocumentsForCliente(cdata.dni, cdata.reserva);
    }

    private void SpawnWorldDocumentsForCliente(DocumentSO dni, DocumentSO reserva)
    {
        ClearSpawnedWorldDocs();

        if (documentWorldPrefab == null) return;

        // ✅ DNI
        if (dni != null && dniSpawnPoint != null)
        {
            var dniInstance = Instantiate(documentWorldPrefab, dniSpawnPoint.position, dniSpawnPoint.rotation);
            dniInstance.name = $"DNI_{dni.title}";
            dniInstance.Initialize(dni);
            dniInstance.OnClicked += HandleDocumentClickedFromWorld;
            _spawnedWorldDocs.Add(dniInstance);
        }

        // ✅ Reserva
        if (reserva != null && reservaSpawnPoint != null)
        {
            var reservaInstance = Instantiate(documentWorldPrefab, reservaSpawnPoint.position, reservaSpawnPoint.rotation);
            reservaInstance.name = $"RESERVA_{reserva.title}";
            reservaInstance.Initialize(reserva);
            reservaInstance.OnClicked += HandleDocumentClickedFromWorld;
            _spawnedWorldDocs.Add(reservaInstance);
        }
    }

    private void HandleDocumentClickedFromWorld(DocumentSO doc)
    {
        HandleDocumentSelected(doc);
    }

    private void HandleDocumentSelected(DocumentSO doc)
    {
        if (doc == null) return;

        if (documentViewer == null)
            documentViewer = FindObjectOfType<DocumentViewer>();

        if (documentViewer != null)
        {
            documentViewer.Show(doc);
            documentViewer.OnClosed -= HandleDocumentViewerClosed;
            documentViewer.OnClosed += HandleDocumentViewerClosed;
        }
    }

    private void HandleDocumentViewerClosed()
    {
        if (documentViewer != null)
            documentViewer.OnClosed -= HandleDocumentViewerClosed;
    }

    private void HandleDialogLineAdvance(int newLineIndex)
    {
        // para sonido o animaciones si quieres
    }

    private void HandleDialogFinishedByEnter()
    {
        if (dialogController != null)
        {
            dialogController.OnLineAdvance -= HandleDialogLineAdvance;
            dialogController.OnDialogFinished -= HandleDialogFinishedByEnter;
        }

        ClearSpawnedWorldDocs();
        if (documentListView != null) documentListView.Hide();
        if (documentViewer != null) documentViewer.Close();

        if (_activeCliente != null)
        {
            _activeCliente.CancelProgressAndLeave(exitPoint, () =>
            {
                SpawnNextClienteIfAny();
            });
        }
        else
        {
            SpawnNextClienteIfAny();
        }
    }

    private void HandleClienteLeft()
    {
        ClearSpawnedWorldDocs();
        if (documentViewer != null) documentViewer.Close();
        if (documentListView != null) documentListView.Hide();
    }

    private void ClearSpawnedWorldDocs()
    {
        foreach (var wv in _spawnedWorldDocs)
        {
            if (wv == null) continue;
            wv.OnClicked -= HandleDocumentClickedFromWorld;
            Destroy(wv.gameObject);
        }
        _spawnedWorldDocs.Clear();
    }
}
