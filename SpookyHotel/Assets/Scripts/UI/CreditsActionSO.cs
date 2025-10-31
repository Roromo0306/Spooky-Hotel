using UnityEngine;

[CreateAssetMenu(menuName = "Actions/CreditsAction", fileName = "CreditsAction")]
public class CreditsActionSO : ButtonActionSO
{
    public string creditsSceneName = "Credits";

    public override void Execute()
    {
        var sceneSvc = ServiceLocator.Get<ISceneService>();
        if (sceneSvc == null)
        {
            Debug.LogError("[CreditsActionSO] ISceneService no registrado.");
            return;
        }

        sceneSvc.LoadScene(creditsSceneName);
    }
}

