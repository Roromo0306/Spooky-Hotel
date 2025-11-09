using UnityEngine;
using Infrastructure.MVC;

public class CameraController : ControllerBase<CameraModel>
{
    [SerializeField] private CameraSettingsSO settingsSO; // Arrastrar el ScriptableObject en el Inspector
    private Camera _camera;

    protected override void OnDestroyController() { }

    protected override void OnModelChange() { }

    protected override async System.Threading.Tasks.Task OnStartController()
    {
        _camera = Camera.main;
        if (_camera == null)
        {
            Debug.LogError("[CameraController] No main camera found in the scene.");
        }

        // Inicializamos el Model con el ScriptableObject y posición inicial
        Model = new CameraModel(settingsSO, _camera.transform.position);

        await System.Threading.Tasks.Task.CompletedTask;
    }

    private void Update()
    {
        if (!IsStarted || Model == null) return;

        HandleMouseMovement();
    }

    private void HandleMouseMovement()
    {
        Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        Vector3 move = new Vector3(mouseDelta.x, mouseDelta.y, mouseDelta.y) * Model.settings.mouseSensitivity;

        Vector3 newTarget = Model.TargetPosition + move;

        // Aplicamos límites X, Y, Z
        newTarget.x = Mathf.Clamp(newTarget.x, Model.settings.minLimits.x, Model.settings.maxLimits.x);
        newTarget.y = Mathf.Clamp(newTarget.y, Model.settings.minLimits.y, Model.settings.maxLimits.y);
        newTarget.z = Mathf.Clamp(newTarget.z, Model.settings.minLimits.z, Model.settings.maxLimits.z);

        Model.TargetPosition = newTarget;

        // Movemos suavemente la cámara
        _camera.transform.position = Vector3.Lerp(
            _camera.transform.position,
            Model.TargetPosition,
            Time.deltaTime * Model.settings.moveSpeed
        );
    }
}
