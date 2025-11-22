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

    protected override async Task OnStartController()
    {
        Model = new ClienteManagerModel();
        Model.SetQueue(clientesToSpawn);
        Model.StartProcessing();
        Model.Subscribe(OnModelChanged);

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
        // Placeholder para UI ligada al modelo
    }

    private void SpawnNextClienteIfAny()
    {
        if (Model == null) return;
        if (!Model.HasMore()) return;

        Model.AdvanceIndex();
        ClienteSO data = Model.GetCurrentCliente();
        if (data == null) return;

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

        // OJO: el diálogo lo muestra el ClienteController,
        // aquí solo preparamos documentos y eventos.
    }

    private void HandleDialogLineAdvance(int newLineIndex)
    {
        // Aquí podrías meter sonidos, animaciones, etc.
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
                SpawnNextClienteIfAny();
            });
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
