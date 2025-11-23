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
        Model.Subscribe(OnModelChange);

        _currentClienteIndex = -1;
        SpawnNextClienteIfAny();

        await Task.CompletedTask;
    }

    protected override void OnModelChange() { }

    protected override void OnDestroyController()
    {
        if (Model != null)
            Model.Unsubscribe(OnModelChange);

        ClearSpawnedWorldDocs();
    }

    private void SpawnNextClienteIfAny()
    {
        ClearSpawnedWorldDocs();

        _currentClienteIndex++;

        if (_currentClienteIndex >= clientesToSpawn.Length)
        {
            ShowEndGameResults();
            return;
        }

        ClienteSO data = clientesToSpawn[_currentClienteIndex];

        GameObject go = Instantiate(clientePrefab, spawnPoint.position, Quaternion.identity);
        _activeCliente = go.GetComponent<ClienteController>();

        if (_activeCliente.progressView == null && sharedProgressView != null)
            _activeCliente.progressView = sharedProgressView;

        _activeCliente.Initialize(data);
        _activeCliente.OnLeftScene += HandleClienteLeft;

        _activeCliente.MoveTo(destinationPoint);
    }

    private void ShowEndGameResults()
    {
        if (resultScreenView == null) return;
        resultScreenView.ShowResults("Resultados", defaultSummaryText);
    }

    public void NotifyClienteReachedFromClient(ClienteController controller)
    {
        _activeCliente = controller;
        var cdata = controller.clienteData;

        if (dialogController == null)
            dialogController = FindObjectOfType<DialogController>();

        dialogController.OnDialogFinished += HandleDialogFinishedByEnter;

        ClearSpawnedWorldDocs();
        SpawnWorldDocumentsForCliente(cdata.dni, cdata.reserva);
    }

    private void SpawnWorldDocumentsForCliente(DocumentSO dni, DocumentSO reserva)
    {
        if (dni != null)
        {
            var dniInstance = Instantiate(documentWorldPrefab, dniSpawnPoint.position, dniSpawnPoint.rotation);
            dniInstance.Initialize(dni);
            dniInstance.OnClicked += HandleDocumentClickedFromWorld;
            _spawnedWorldDocs.Add(dniInstance);
        }

        if (reserva != null)
        {
            var reservaInstance = Instantiate(documentWorldPrefab, reservaSpawnPoint.position, reservaSpawnPoint.rotation);
            reservaInstance.Initialize(reserva);
            reservaInstance.OnClicked += HandleDocumentClickedFromWorld;
            _spawnedWorldDocs.Add(reservaInstance);
        }
    }

    private void HandleDocumentClickedFromWorld(DocumentSO doc)
    {
        if (documentViewer == null) return;
        documentViewer.Show(doc);
    }

    private void HandleDialogFinishedByEnter()
    {
        if (dialogController != null)
            dialogController.OnDialogFinished -= HandleDialogFinishedByEnter;

        if (documentViewer != null)
            documentViewer.Close();
    }

    private void HandleClienteLeft()
    {
        if (dialogController != null)
            dialogController.ForceCloseDialog();

        if (documentViewer != null)
            documentViewer.Close();

        ClearSpawnedWorldDocs();
    }

    public void OnClienteReallyLeft(ClienteController controller)
    {
        if (dialogController != null)
            dialogController.ForceCloseDialog();

        ClearSpawnedWorldDocs();
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
