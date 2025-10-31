using UnityEngine;
using UnityEngine.UI;
using System;

public class MainMenuView : MonoBehaviour
{
    [Header("Botones")]
    public Button playButton;
    public Button creditsButton;
    public Button quitButton;

    public void Bind(Action onPlay, Action onCredits, Action onQuit)
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() => onPlay?.Invoke());
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(() => onCredits?.Invoke());
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() => onQuit?.Invoke());
        }
    }

    public void SetInteractable(bool state)
    {
        if (playButton) playButton.interactable = state;
        if (creditsButton) creditsButton.interactable = state;
        if (quitButton) quitButton.interactable = state;
    }
}

