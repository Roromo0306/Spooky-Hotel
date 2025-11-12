using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View encapsula referencias UI. No contiene lógica de negocio.
/// </summary>
public class BookView : MonoBehaviour
{
    [Header("Root")]
    public GameObject rootPanel;        // panel modal (desactivado por defecto)

    [Header("UI Elements")]
    public Image pageImage;             // image where page sprite is shown
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;

    private void Awake()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    public void Show()
    {
        if (rootPanel != null) rootPanel.SetActive(true);
    }

    public void Hide()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    public void SetPage(Sprite sprite)
    {
        if (pageImage != null)
        {
            pageImage.sprite = sprite;
            // No usar SetNativeSize si quieres mantener tamaño; si quieres usarlo, comprueba layout.
            // pageImage.SetNativeSize();
        }
    }

    public void SetNavEnabled(bool prevEnabled, bool nextEnabled)
    {
        if (prevButton != null) prevButton.interactable = prevEnabled;
        if (nextButton != null) nextButton.interactable = nextEnabled;
    }
}
