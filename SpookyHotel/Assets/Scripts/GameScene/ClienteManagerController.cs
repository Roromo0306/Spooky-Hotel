using System.Collections;
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

        // Suscribir primero a los eventos del dialogController (para no perder el OnTypingStarted)
        dialogController.OnLineAdvance += HandleDialogLineAdvance;
        dialogController.OnDialogFinished += HandleDialogFinishedByEnter;
        dialogController.OnTypingStarted += HandleTypingStarted;
        dialogController.OnTypingEnded += HandleTypingEnded;

        // Ahora mostrar el diálogo (esto arrancará la primera línea y disparará OnTypingStarted,
        // que ya está suscrito)
        dialogController.ShowDialog(_activeCliente.clienteData.dialogos, _activeCliente.clienteData.nombre);
    }

    private void HandleDialogLineAdvance(int newLineIndex)
    {
        // simple: podrías reproducir sonido, animación, etc.
        // no hacemos nada especial aquí
    }

    private void HandleDialogFinishedByEnter()
    {
        // Desuscribir handlers principales
        dialogController.OnLineAdvance -= HandleDialogLineAdvance;
        dialogController.OnDialogFinished -= HandleDialogFinishedByEnter;

        // Desuscribir handlers de typing
        dialogController.OnTypingStarted -= HandleTypingStarted;
        dialogController.OnTypingEnded -= HandleTypingEnded;

        if (_activeCliente != null)
        {
            // Llamamos a CancelProgressAndLeave para detener progreso/shake y hacer salir al cliente
            _activeCliente.CancelProgressAndLeave(exitPoint, () =>
            {
                SpawnNextClienteIfAny();
            });
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
        // si prefieres, también puedes usar este evento para spawnear el siguiente.
        // SpawnNextClienteIfAny();
    }
}
