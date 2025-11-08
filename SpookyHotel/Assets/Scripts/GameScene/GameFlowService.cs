using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowService : IGameFlowService
{
    private GameObject _gameOverPrefab; // set by bootstrapper or via setter
    private BootConfigSO _bootConfig; // optional

    // audio and scene services through ServiceLocator when needed

    public GameFlowService(GameObject gameOverPrefab, BootConfigSO bootConfig = null)
    {
        _gameOverPrefab = gameOverPrefab;
        _bootConfig = bootConfig;
    }

    public void TriggerGameOver()
    {
        // instantiate prefab (handles black screen, sound & UI)
        if (_gameOverPrefab != null)
        {
            GameObject.Instantiate(_gameOverPrefab);
        }
        else
        {
            Debug.LogWarning("[GameFlowService] No GameOver prefab assigned.");
        }
    }

    public void ShowMainMenu()
    {
        var sceneSvc = ServiceLocator.Get<ISceneService>();
        if (sceneSvc != null)
        {
            sceneSvc.LoadScene("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}