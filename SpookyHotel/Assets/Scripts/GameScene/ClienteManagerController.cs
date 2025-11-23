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
    [SerializeField] private DocumentListView documentListView; // opcional

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

    // --------------------- CICLO DE VIDA ---------------------

    protected override async Task OnStartController()
    {
        Model = new ClienteManagerModel();
        Model.SetQueue(clientesToSpawn);
        Model.StartProcessing();
        Model.Subscribe(OnModelChanged);

        _currentClienteIndex = -1;
        Debug.Log("[ClienteManagerController] Start. Total clientes: " + clientesToSpawn.Length);
        SpawnNextClienteIfAny();

        await Task.CompletedTask;
    }

    protected override void OnModelChange()
    {
        // No usado por ahora
    }

    protected override void OnDestroyController()
    {
        if (Model != null)
            Model.Unsubscribe(OnModelChanged);

        ClearSpawnedWorldDocs();
    }

    private void OnModelChanged()
    {
        // Placeholder
    }

    // --------------------- SPAWN DE CLIENTES ---------------------

    private void SpawnNextClienteIfAny()
    {
        // Limpieza extra por si algo raro quedó
        ClearSpawnedWorldDocs();

        _currentClienteIndex++;
        Debug.Log("[ClienteManagerController] SpawnNextClienteIfAny -> índice: " + _currentClienteIndex);

        if (_currentClienteIndex >= clientesToSpawn.Length)
        {
            Debug.Log("[ClienteManagerController] No quedan más clientes.");
            ShowEndGameResults();
            return;
        }

        ClienteSO data = clientesToSpawn[_currentClienteIndex];
        if (data == null)
        {
            Debug.LogError("[ClienteManagerController] ClienteSO null en índice " + _currentClienteIndex);
            ShowEndGameResults();
            return;
        }

        Debug.Log("[ClienteManagerController] Instanciando cliente: " + data.nombre);

        GameObject go = Instantiate(clientePrefab, spawnPoint.position, Quaternion.identity);
        _activeCliente = go.GetComponent<ClienteController>();
        if (_activeCliente == null)
        {
            Debug.LogError("[ClienteManagerController] clientePrefab no contiene ClienteController.");
            Destroy(go);
            return;
        }

        if (_activeCliente.progressView == null && sharedProgressView != null)
        {
            _activeCliente.progressView = sharedProgressView;
            Debug.Log("[ClienteManagerController] Asigné sharedProgressView al cliente instanciado.");
        }

        _activeCliente.Initialize(data);

        // OnLeftScene lo seguimos usando por si quieres lógica extra
        _activeCliente.OnLeftScene += HandleClienteLeft;

        _activeCliente.MoveTo(destinationPoint);
    }

    private void ShowEndGameResults()
    {
        if (resultScreenView == null)
        {
            Debug.LogError("[ClienteManagerController] resultScreenView no asignado.");
            return;
        }

        string title = "Resultados de la partida";
        string summary = defaultSummaryText;

        Debug.Log("[ClienteManagerController] Mostrando pantalla de resultados.");
        resultScreenView.ShowResults(title, summary);
    }

    // --------------------- LLAMADO DESDE ClienteController AL LLEGAR ---------------------

    public void NotifyClienteReachedFromClient(ClienteController controller)
    {
        if (controller == null || controller.clienteData == null)
        {
            Debug.LogWarning("[ClienteManagerController] NotifyClienteReachedFromClient con controller o clienteData null.");
            return;
        }

        _activeCliente = controller;
        var cdata = controller.clienteData;

        Debug.Log($"[ClienteManagerController] NotifyClienteReachedFromClient -> {cdata.nombre}" +
                  $"\n   DNI: {(cdata.dni != null ? cdata.dni.title : "NULL")}" +
                  $"\n   RESERVA: {(cdata.reserva != null ? cdata.reserva.title : "NULL")}");

        // Asegurar DialogController
        if (dialogController == null)
        {
            dialogController = FindObjectOfType<DialogController>();
            if (dialogController == null)
            {
                Debug.LogError("[ClienteManagerController] No se encontró DialogController en la escena.");
                return;
            }
        }

        dialogController.OnLineAdvance += HandleDialogLineAdvance;
        dialogController.OnDialogFinished += HandleDialogFinishedByEnter;

        // APARECEN DOCUMENTOS DEL CLIENTE
        ClearSpawnedWorldDocs();
        SpawnWorldDocumentsForCliente(cdata.dni, cdata.reserva);
    }

    // Método de compatibilidad
    private void HandleClienteReached()
    {
        if (_activeCliente == null || _activeCliente.clienteData == null) return;
        NotifyClienteReachedFromClient(_activeCliente);
    }

    private void SpawnWorldDocumentsForCliente(DocumentSO dni, DocumentSO reserva)
    {
        if (documentWorldPrefab == null)
        {
            Debug.LogError("[ClienteManagerController] documentWorldPrefab no asignado.");
            return;
        }

        Debug.Log("[ClienteManagerController] SpawnWorldDocumentsForCliente()" +
                  $" dni={(dni != null ? dni.title : "NULL")}, reserva={(reserva != null ? reserva.title : "NULL")}");

        if (dni != null && dniSpawnPoint != null)
        {
            var dniInstance = Instantiate(documentWorldPrefab, dniSpawnPoint.position, dniSpawnPoint.rotation);
            dniInstance.name = $"DNI_{dni.title}";
            dniInstance.Initialize(dni);
            dniInstance.OnClicked += HandleDocumentClickedFromWorld;
            _spawnedWorldDocs.Add(dniInstance);
            Debug.Log("[ClienteManagerController] DNI instanciado en mesa -> " + dni.title);
        }

        if (reserva != null && reservaSpawnPoint != null)
        {
            var reservaInstance = Instantiate(documentWorldPrefab, reservaSpawnPoint.position, reservaSpawnPoint.rotation);
            reservaInstance.name = $"RESERVA_{reserva.title}";
            reservaInstance.Initialize(reserva);
            reservaInstance.OnClicked += HandleDocumentClickedFromWorld;
            _spawnedWorldDocs.Add(reservaInstance);
            Debug.Log("[ClienteManagerController] RESERVA instanciada en mesa -> " + reserva.title);
        }
    }

    // 🔴 LLAMADO DIRECTAMENTE POR ClienteController AL SALIR POR LA PUERTA
    public void OnClienteReallyLeft(ClienteController controller)
    {
        Debug.Log("[ClienteManagerController] OnClienteReallyLeft -> " +
                  (controller != null && controller.clienteData != null ? controller.clienteData.nombre : "null"));

        // solo limpiamos si es el cliente activo
        if (controller == _activeCliente)
        {
            ClearSpawnedWorldDocs();
        }
    }

    // --------------------- CLICK EN DOCUMENTOS ---------------------

    private void HandleDocumentClickedFromWorld(DocumentSO doc)
    {
        Debug.Log("[ClienteManagerController] HandleDocumentClickedFromWorld -> " +
                  (doc != null ? doc.title : "null"));
        HandleDocumentSelected(doc);
    }

    private void HandleDocumentSelected(DocumentSO doc)
    {
        Debug.Log("[ClienteManagerController] HandleDocumentSelected -> " +
                  (doc != null ? doc.title : "null"));

        if (doc == null) return;

        if (documentViewer == null)
        {
            Debug.LogError("[ClienteManagerController] documentViewer no asignado en el inspector.");
            return;
        }

        documentViewer.Show(doc);
        documentViewer.OnClosed -= HandleDocumentViewerClosed;
        documentViewer.OnClosed += HandleDocumentViewerClosed;
    }

    private void HandleDocumentViewerClosed()
    {
        if (documentViewer != null)
            documentViewer.OnClosed -= HandleDocumentViewerClosed;
    }

    // --------------------- DIÁLOGO ---------------------

    private void HandleDialogLineAdvance(int newLineIndex)
    {
        // sonidos, animaciones, etc
    }

    private void HandleDialogFinishedByEnter()
    {
        Debug.Log("[ClienteManagerController] Diálogo terminado por Enter.");

        if (dialogController != null)
        {
            dialogController.OnLineAdvance -= HandleDialogLineAdvance;
            dialogController.OnDialogFinished -= HandleDialogFinishedByEnter;
        }

        // limpiar documentos cuando ya no necesito verlos
        ClearSpawnedWorldDocs();

        if (documentListView != null)
        {
            documentListView.OnDocumentSelected -= HandleDocumentSelected;
            documentListView.Hide();
        }

        if (documentViewer != null)
        {
            documentViewer.Close();
            documentViewer.OnClosed -= HandleDocumentViewerClosed;
        }

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

    // --------------------- CLIENTE SE VA (evento OnLeftScene, por si quieres usarlo) ---------------------

    private void HandleClienteLeft()
    {
        Debug.Log("[ClienteManagerController] Cliente salió de escena (OnLeftScene).");

        if (dialogController != null)
        {
            dialogController.OnLineAdvance -= HandleDialogLineAdvance;
            dialogController.OnDialogFinished -= HandleDialogFinishedByEnter;
        }

        // aquí NO hacemos ClearSpawnedWorldDocs, porque ya lo hacemos en OnClienteReallyLeft
        // para que esté completamente sincronizado con la llegada al exitPoint

        if (documentListView != null)
        {
            documentListView.OnDocumentSelected -= HandleDocumentSelected;
            documentListView.Hide();
        }

        if (documentViewer != null)
        {
            documentViewer.Close();
            documentViewer.OnClosed -= HandleDocumentViewerClosed;
        }
    }

    // --------------------- LIMPIEZA DOCUMENTOS ---------------------

    private void ClearSpawnedWorldDocs()
    {
        Debug.Log("[ClienteManagerController] ClearSpawnedWorldDocs. Count = " + _spawnedWorldDocs.Count);

        foreach (var wv in _spawnedWorldDocs)
        {
            if (wv == null) continue;
            wv.OnClicked -= HandleDocumentClickedFromWorld;
            Destroy(wv.gameObject);
        }
        _spawnedWorldDocs.Clear();
    }
}
