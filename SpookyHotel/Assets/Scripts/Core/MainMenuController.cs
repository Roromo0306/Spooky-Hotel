using System.Threading.Tasks;
using UnityEngine;

public class MainMenuController : ControllerBase<MainMenuModel>
{
    [Header("View")]
    [SerializeField] private MainMenuView view;

    [Header("Actions (ScriptableObjects - Strategy)")]
    [SerializeField] private ButtonActionSO playAction;
    [SerializeField] private ButtonActionSO creditsAction;
    [SerializeField] private ButtonActionSO quitAction;

    protected override async Task OnStartController()
    {
        Model = new MainMenuModel();

        if (view != null)
        {
            view.Bind(
                onPlay: () =>
                {
                    Model.Select(MainMenuModel.MenuSelection.Play);
                    playAction?.Execute();
                },
                onCredits: () =>
                {
                    Model.Select(MainMenuModel.MenuSelection.Credits);
                    creditsAction?.Execute();
                },
                onQuit: () =>
                {
                    Model.Select(MainMenuModel.MenuSelection.Quit);
                    quitAction?.Execute();
                }
            );
        }

        await Task.CompletedTask;
    }

    protected override void OnModelChange()
    {
        if (Model == null || view == null) return;
        view.SetInteractable(Model.IsInteractable);
        // se puede usar Model.Selected para efectos visuales, etc.
    }

    protected override void OnDestroyController() { }
}

