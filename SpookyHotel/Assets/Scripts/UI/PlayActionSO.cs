using UnityEngine;

[CreateAssetMenu(menuName = "Actions/PlayAction", fileName = "PlayAction")]
public class PlayActionSO : ButtonActionSO
{
    public BootConfigSO bootConfig;
    public string targetSceneName;

    public override void Execute()
    {
        var sceneSvc = ServiceLocator.Get<ISceneService>();
        if (sceneSvc == null)
        {
            Debug.LogError("[PlayActionSO] ISceneService no registrado.");
            return;
        }

        sceneSvc.LoadBootstrapperWithConfig(bootConfig, targetSceneName);
    }
}

