using Infrastructure.MVC;
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
    [SerializeField] private ButtonActionSO returnAction; // ✅ NUEVO

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip clip;

    protected override async Task OnStartController()
    {
        Model = new MainMenuModel();

        if (view != null)
        {
            view.Bind(
                onPlay: async () =>
                {
                    PlayClickSound();
                    await Task.Delay(400);
                    Model.Select(MainMenuModel.MenuSelection.Play);
                    playAction?.Execute();
                },
                onCredits: async () =>
                {
                    PlayClickSound();
                    await Task.Delay(400);
                    Model.Select(MainMenuModel.MenuSelection.Credits);
                    creditsAction?.Execute();
                },
                onQuit: async () =>
                {
                    PlayClickSound();
                    await Task.Delay(400);
                    Model.Select(MainMenuModel.MenuSelection.Quit);
                    quitAction?.Execute();
                },
                onReturn: async () => // ✅ AHORA SE PASA EL 4º ARGUMENTO
                {
                    PlayClickSound();
                    await Task.Delay(400);
                    Model.Select(MainMenuModel.MenuSelection.None);
                    returnAction?.Execute();
                }
            );
        }

        await Task.CompletedTask;
    }

    private void PlayClickSound()
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    protected override void OnModelChange()
    {
        if (Model == null || view == null) return;
        view.SetInteractable(Model.IsInteractable);
    }

    protected override void OnDestroyController() { }
}
