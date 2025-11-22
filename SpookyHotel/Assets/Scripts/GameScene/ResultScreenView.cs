using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultScreenView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;        // Panel negro a pantalla completa
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button mainMenuButton;

    [Header("Config")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // Cambia al nombre de tu escena de menú

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        // Si no se asigna panel, asumimos que es el mismo GameObject
        if (panel == null)
            panel = this.gameObject;

        // Aseguramos CanvasGroup para controlar alpha / input
        _canvasGroup = panel.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = panel.AddComponent<CanvasGroup>();

        // Estado inicial: invisible y sin interacción, pero ACTIVO (para que funcionen coroutines)
        panel.SetActive(true);
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuButtonPressed);
        }
    }

    /// <summary>
    /// Muestra la pantalla de resultados sin fade (visible al instante).
    /// </summary>
    public void ShowResults(string title, string summary)
    {
        if (panel == null) return;

        panel.SetActive(true);

        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = summary;

        if (_canvasGroup == null)
            _canvasGroup = panel.GetComponent<CanvasGroup>();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// Muestra la pantalla de resultados con un fade in suave.
    /// </summary>
    public void ShowResultsWithFade(string title, string summary, float duration = 1f)
    {
        if (panel == null) return;

        // Por seguridad, paramos cualquier fade anterior
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine(title, summary, duration));
    }

    private IEnumerator FadeInRoutine(string title, string summary, float duration)
    {
        panel.SetActive(true);

        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = summary;

        if (_canvasGroup == null)
            _canvasGroup = panel.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = panel.AddComponent<CanvasGroup>();

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            _canvasGroup.alpha = normalized;
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    private void OnMainMenuButtonPressed()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("[ResultScreenView] mainMenuSceneName no está configurado.");
        }
    }
}


