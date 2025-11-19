using System.Collections;
using Infrastructure.MVC;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ClienteManagerController : ControllerBase<ClienteManagerModel>
{
    [Header("Configuración")]
    [SerializeField] private ClienteSO[] clientesToSpawn; // asigna los 5 S.O. aquí en el Inspector
    [SerializeField] private GameObject clientePrefab;    // prefab con ClienteController
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform destinationPoint;
    [SerializeField] private Transform exitPoint;

    [Header("Dialog UI")]
    [SerializeField] private DialogController dialogController;

    private ClienteController _activeCliente;
    public ProgressBarView sharedProgressView;


    [SerializeField] private DocumentListView documentListView; // asignar en Inspector (panel)
    [SerializeField] private DocumentViewer documentViewer; // modal viewer
    

    protected override async Task OnStartController()
    {
        // inicializar modelo con la cola
        Model = new ClienteManagerModel();
        Model.SetQueue(clientesToSpawn);
        Model.StartProcessing();

        // subscribir si necesitas reaccionar a cambios
        Model.Subscribe(OnModelChanged);

        // iniciar el primer spawn
        SpawnNextClienteIfAny();

        await Task.CompletedTask;
    }

    protected override void OnModelChange()
    {
        // para este flujo quizá no necesites hacer nada aquí,
        // pero queda listo si quieres enlazar UI a Model.
    }

    protected override void OnDestroyController()
    {
        if (Model != null)
            Model.Unsubscribe(OnModelChanged);
    }

    private void OnModelChanged()
    {
        // placeholder - en caso de que quieras actualizar UI basado en model
    }

    private void SpawnNextClienteIfAny()
    {
        if (Model == null) return;
        if (!Model.HasMore()) return;

        // Avanzar índice y crear el siguiente cliente
        Model.AdvanceIndex();
        ClienteSO? data = Model.GetCurrentCliente();
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

        // iniciar movimiento hacia destinationPoint
        _activeCliente.MoveTo(destinationPoint);
    }

    private void HandleClienteReached()
    {
        if (_activeCliente == null || _activeCliente.clienteData == null) return;

        // Subscribe dialog events first (para typing, etc.)
        dialogController.OnLineAdvance += HandleDialogLineAdvance;
        dialogController.OnDialogFinished += HandleDialogFinishedByEnter;
        dialogController.OnTypingStarted += HandleTypingStarted;
        dialogController.OnTypingEnded += HandleTypingEnded;

        // Mostrar miniaturas en el mostrador (no abre el viewer)
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

        // Por último, mostrar diálogo
        dialogController.ShowDialog(cdata.dialogos, cdata.nombre);
    }
    private void HandleDialogLineAdvance(int newLineIndex)
    {
        // simple: podrías reproducir sonido, animación, etc.
        // no hacemos nada especial aquí
    }

 
    private void HandleDialogFinishedByEnter()
    {
        // quitar suscripciones del dialogController
        dialogController.OnLineAdvance -= HandleDialogLineAdvance;
        dialogController.OnDialogFinished -= HandleDialogFinishedByEnter;
        dialogController.OnTypingStarted -= HandleTypingStarted;
        dialogController.OnTypingEnded -= HandleTypingEnded;
        if (documentListView != null)
        {
            documentListView.OnDocumentSelected -= HandleDocumentSelected;
            documentListView.Hide();
        }

        if (documentViewer != null)
        {
            documentViewer.Close(); // ocultar si está abierto
            documentViewer.OnClosed -= HandleDocumentViewerClosed;
        }

        if (_activeCliente != null)
        {
            _activeCliente.CancelProgressAndLeave(exitPoint, () =>
            {
                SpawnNextClienteIfAny();
            });
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

private void HandleTypingStarted()
    {
        if (_activeCliente != null)
        {
            _activeCliente.StartSpeakingPulse();
        }
    }

    private void HandleTypingEnded()
    {
        if (_activeCliente != null)
        {
            _activeCliente.StopSpeakingPulse();
        }
    }
    private void HandleClienteLeft()
    {
        // limpieza similar por seguridad
        dialogController.OnLineAdvance -= HandleDialogLineAdvance;
        dialogController.OnDialogFinished -= HandleDialogFinishedByEnter;
        dialogController.OnTypingStarted -= HandleTypingStarted;
        dialogController.OnTypingEnded -= HandleTypingEnded;

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
        // si prefieres, también puedes usar este evento para spawnear el siguiente.
        // SpawnNextClienteIfAny();
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
            documentViewer.Show(doc); // SOLO aquí se abre el viewer
                                      // opcional: documentListView.Hide(); // si quieres ocultar miniaturas mientras la imagen está abierta
            documentViewer.OnClosed -= HandleDocumentViewerClosed;
            documentViewer.OnClosed += HandleDocumentViewerClosed;
        }
    }
    private void HandleDocumentViewerClosed()
    {
       documentViewer.OnClosed -= HandleDocumentViewerClosed;
        // opcional: si ocultaste la lista, vuelves a mostrarla aquí
        // if (documentListView != null && _activeCliente != null) documentListView.ShowDocuments(new DocumentSO[]{ _activeCliente.clienteData.dni, _activeCliente.clienteData.reserva });
    }

    private void HandlePuzzleSolved()
    {
        if (_activeCliente == null) return;

        // Mostrar diálogo final
        var cdata = _activeCliente.clienteData;
        dialogController.ShowDialog(cdata.dialogos, cdata.nombre);

        // Esperar que el jugador cierre diálogo (ENTER)
        dialogController.OnDialogFinished += () =>
        {
            dialogController.OnDialogFinished -= HandleDialogFinishedByEnter;
            _activeCliente.CancelProgressAndLeave(exitPoint, () =>
            {
                SpawnNextClienteIfAny();
            });
        };
    }

}
