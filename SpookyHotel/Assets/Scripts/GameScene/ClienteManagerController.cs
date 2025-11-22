using Infrastructure.MVC;
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

    [SerializeField] private DocumentListView documentListView;
    [SerializeField] private DocumentViewer documentViewer;

    [Header("End Game UI")]
    [SerializeField] private ResultScreenView resultScreenView;

    [TextArea(3, 6)]
    [SerializeField]
    private string defaultSummaryText =
        "¡Has terminado la jornada!\n\n(Después puedes sustituir este texto por estadísticas de la partida).";

    // 👉 índice del cliente actual en el array
    private int _currentClienteIndex = -1;

    protected override async Task OnStartController()
    {
        // Si sigues usando el modelo para otras cosas, lo dejamos inicializado
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
        // No usamos por ahora
    }

    protected override void OnDestroyController()
    {
        if (Model != null)
            Model.Unsubscribe(OnModelChanged);
    }

    private void OnModelChanged()
    {
        // Placeholder
    }

    private void SpawnNextClienteIfAny()
    {
        _currentClienteIndex++;

        // 👉 Si ya nos hemos pasado del último índice, mostramos resultados
        if (_currentClienteIndex >= clientesToSpawn.Length)
        {
            Debug.Log("[ClienteManagerController] No quedan más clientes. Mostrando pantalla de resultados.");
            ShowEndGameResults();
            return;
        }

        ClienteSO data = clientesToSpawn[_currentClienteIndex];
        if (data == null)
        {
            Debug.LogError("[ClienteManagerController] ClienteSO en índice " + _currentClienteIndex + " es null.");
            // Aun así intentamos seguir al siguiente
            ShowEndGameResults();
            return;
        }

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
            Debug.Log("[Manager] Asigné sharedProgressView al cliente instanciado.");
        }

        _activeCliente.Initialize(data);
        _activeCliente.OnReachedDestination += HandleClienteReached;
        _activeCliente.OnLeftScene += HandleClienteLeft;

        _activeCliente.MoveTo(destinationPoint);
    }

    private void ShowEndGameResults()
    {
        if (resultScreenView == null)
        {
            Debug.LogError("[ClienteManagerController] resultScreenView no asignado en el inspector.");
            return;
        }

        string title = "Resultados de la partida";
        string summary = BuildResultsSummary();

        Debug.Log("[ClienteManagerController] Llamando a resultScreenView.ShowResults");
        resultScreenView.ShowResults(title, summary);
    }

    private string BuildResultsSummary()
    {
        // Aquí puedes usar info real del modelo si quieres.
        // De momento devolvemos defaultSummaryText:
        return defaultSummaryText;
    }

    private void HandleClienteReached()
    {
        if (_activeCliente == null || _activeCliente.clienteData == null) return;

        // Asegurar DialogController
        if (dialogController == null)
        {
            dialogController = FindObjectOfType<DialogController>();
            if (dialogController == null)
            {
                Debug.LogError("[Manager] No se encontró DialogController en la escena.");
                return;
            }
        }

        // Suscribir eventos necesarios (liberarse cuando termine)
        dialogController.OnLineAdvance += HandleDialogLineAdvance;
        dialogController.OnDialogFinished += HandleDialogFinishedByEnter;

        // Mostrar documentos del cliente
        var cdata = _activeCliente.clienteData;
        var docs = new DocumentSO[] { cdata.dni, cdata.reserva };

        if (documentListView == null)
        {
            documentListView = FindObjectOfType<DocumentListView>();
            Debug.LogWarning("[Manager] documentListView was null. FindObjectOfType -> " + (documentListView != null));
        }

        if (documentListView != null)
        {
            documentListView.ShowDocuments(docs);
            documentListView.OnDocumentSelected += HandleDocumentSelected;
        }
        else
        {
            Debug.LogWarning("[Manager] documentListView is null, cannot show documents.");
        }

        // El diálogo lo lanza el ClienteController al llegar
    }

    private void HandleDialogLineAdvance(int newLineIndex)
    {
        // Para sonidos, animaciones, etc.
    }

    private void HandleDialogFinishedByEnter()
    {
        if (dialogController != null)
        {
            dialogController.OnLineAdvance -= HandleDialogLineAdvance;
            dialogController.OnDialogFinished -= HandleDialogFinishedByEnter;
        }

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
                // Cuando este cliente se va, intentamos spawnear el siguiente.
                // Si no hay más, se mostrará la pantalla de resultados.
                SpawnNextClienteIfAny();
            });
        }
        else
        {
            // Por si acaso
            SpawnNextClienteIfAny();
        }
    }

    private void HandleClienteLeft()
    {
        if (dialogController != null)
        {
            dialogController.OnLineAdvance -= HandleDialogLineAdvance;
            dialogController.OnDialogFinished -= HandleDialogFinishedByEnter;
        }

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

    private void HandleDocumentSelected(DocumentSO doc)
    {
        Debug.Log("[Manager] Document selected -> " + (doc != null ? doc.title : "null"));
        if (doc == null) return;

        if (documentViewer == null)
        {
            documentViewer = FindObjectOfType<DocumentViewer>();
            Debug.LogWarning("[Manager] documentViewer was null. FindObjectOfType -> " + (documentViewer != null));
        }

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

    private void HandlePuzzleSolved()
    {
        if (_activeCliente == null) return;

        var cdata = _activeCliente.clienteData;

        if (dialogController == null)
        {
            dialogController = FindObjectOfType<DialogController>();
            if (dialogController == null)
            {
                Debug.LogError("[Manager] No se encontró DialogController para puzzle.");
                return;
            }
        }

        dialogController.ShowDialog(cdata.dialogos, cdata.nombre);

        void OnFinalDialogFinished()
        {
            dialogController.OnDialogFinished -= OnFinalDialogFinished;

            if (_activeCliente != null)
            {
                _activeCliente.CancelProgressAndLeave(exitPoint, () =>
                {
                    SpawnNextClienteIfAny();
                });
            }
        }

        dialogController.OnDialogFinished += OnFinalDialogFinished;
    }
}
