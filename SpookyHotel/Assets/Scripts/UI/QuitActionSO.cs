using UnityEngine;

[CreateAssetMenu(menuName = "Actions/QuitAction", fileName = "QuitAction")]
public class QuitActionSO : ButtonActionSO
{
    public override void Execute()
    {
        var appSvc = ServiceLocator.Get<IAppService>();
        if (appSvc == null)
        {
            Debug.LogError("[QuitActionSO] IAppService no registrado.");
            return;
        }

        appSvc.Quit();
    }
}

