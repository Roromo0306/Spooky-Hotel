public interface ISceneService : IService
{
    void LoadScene(string sceneName);
    void LoadBootstrapperWithConfig(BootConfigSO bootConfig, string targetScene);
}

