using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal/visor simple que muestra una imagen (document) y un botón cerrar.
/// rootPanel debe ser un panel dentro del Canvas (inicialmente desactivado).
/// </summary>
public class DocumentViewer : MonoBehaviour
{
    [Header("UI References")]
    public GameObject rootPanel;
    public Image documentImage;
    public Button closeButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openClip;
    public AudioClip closeClip;

    public event Action OnClosed;

    private void Awake()
    {
        // Aseguramos que el visor esté cerrado al inicio
        if (rootPanel != null) rootPanel.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    /// <summary>
    /// Muestra la imagen contenida en DocumentSO.
    /// </summary>
    public void Show(DocumentSO doc)
    {
        Debug.Log("[DocumentViewer] Show called for doc: " + (doc != null ? doc.title : "null"));

        if (doc == null)
        {
            Debug.LogWarning("[DocumentViewer] Show called with null doc.");
            return;
        }

        if (documentImage != null)
        {
            if (doc.image != null)
            {
                documentImage.sprite = doc.image;
                documentImage.SetNativeSize();
                documentImage.enabled = true;
            }
            else
            {
                documentImage.sprite = null;
                documentImage.enabled = false;
            }
        }

        if (rootPanel != null) rootPanel.SetActive(true);

        // ✅ reproducimos sonido al abrir
        if (audioSource != null && openClip != null)
            audioSource.PlayOneShot(openClip);
    }

    public void Close()
    {
        Debug.Log("[DocumentViewer] Close called.");

        if (rootPanel != null) rootPanel.SetActive(false);

        // ✅ reproducimos sonido al cerrar
        if (audioSource != null && closeClip != null)
            audioSource.PlayOneShot(closeClip);

        OnClosed?.Invoke();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveAllListeners();
    }
}
