using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapperController : MonoBehaviour
{
    public BootConfigSO bootConfig;
    public LoadingView loadingView;

    private void Start()
    {
        string target = (bootConfig != null && !string.IsNullOrEmpty(bootConfig.nextScene))
            ? bootConfig.nextScene
            : "GameScene";

        StartCoroutine(LoadSceneAsync(target));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            if (loadingView != null) loadingView.SetProgress(progress);
            yield return null;
        }
    }
}

