using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameOverController : MonoBehaviour
{
    [Header("UI")]
    public Image blackPanel; // full-screen black image (alpha 0 -> animate to 1)
    public TextMeshProUGUI gameOverText;
    public Button backToMenuButton;

    [Header("Audio")]
    public AudioClip bulbsBreakClip;
    public float blackDelay = 0.2f; // small fade delay
    public float showDelaySeconds = 2.0f; // seconds after sound to show "GAME OVER" and button

    private IAudioService _audio;

    private void Awake()
    {
        // find audio service
        _audio = ServiceLocator.Get<IAudioService>();
        // initial state
        if (blackPanel != null) { var c = blackPanel.color; c.a = 0f; blackPanel.color = c; }
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // fade to black quickly
        float t = 0f;
        float fadeTime = 0.5f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / fadeTime);
            if (blackPanel != null) { var c = blackPanel.color; c.a = a; blackPanel.color = c; }
            yield return null;
        }

        // play bulbs breaking sound
        if (_audio != null && bulbsBreakClip != null) _audio.PlayOneShot(bulbsBreakClip, 1f);

        // wait a bit
        yield return new WaitForSeconds(showDelaySeconds);

        if (gameOverText != null) gameOverText.gameObject.SetActive(true);
        if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(true);

        // wire button
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(() =>
            {
                var flow = ServiceLocator.Get<IGameFlowService>();
                if (flow != null) flow.ShowMainMenu();
            });
        }
    }
}
