using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class ServiceBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        // Evitar registrar doble si ya estaba
        if (ServiceLocator.Get<ISceneService>() != null && ServiceLocator.Get<IAppService>() != null)
        {
            DontDestroyOnLoad(gameObject);
            return;
        }

        // Registrar implementaciones
        ServiceLocator.Register<ISceneService>(new SceneService());
        ServiceLocator.Register<IAppService>(new AppService());

        DontDestroyOnLoad(gameObject);
    }
}

