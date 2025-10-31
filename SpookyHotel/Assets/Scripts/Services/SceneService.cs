using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneService : ISceneService
{
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneService] sceneName vacío.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    public void LoadBootstrapperWithConfig(BootConfigSO bootConfig, string targetScene)
    {
        if (bootConfig == null)
        {
            Debug.LogError("[SceneService] bootConfig null.");
            return;
        }

        bootConfig.nextScene = targetScene;
        SceneManager.LoadScene("Bootstrapper");
    }
}

