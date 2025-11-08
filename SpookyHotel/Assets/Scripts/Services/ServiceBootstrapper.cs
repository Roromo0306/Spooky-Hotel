using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class ServiceBootstrapper : MonoBehaviour
{
    [Header("References")]
    public GameObject gameOverPrefab; // asignar prefab en inspector
    public BootConfigSO bootConfig;   // opcional

    private void Awake()
    {
        // Evitar duplicados si ya existe
        if (ServiceLocator.Get<ISceneService>() != null && ServiceLocator.Get<IAppService>() != null)
        {
            DontDestroyOnLoad(gameObject);
            return;
        }

        // Registrar implementaciones existentes
        ServiceLocator.Register<ISceneService>(new SceneService());
        ServiceLocator.Register<IAppService>(new AppService());

        // nuevo audio service
        ServiceLocator.Register<IAudioService>(new AudioService());

        // GameFlowService con prefab
        ServiceLocator.Register<IGameFlowService>(new GameFlowService(gameOverPrefab, bootConfig));

        DontDestroyOnLoad(gameObject);
    }
}
