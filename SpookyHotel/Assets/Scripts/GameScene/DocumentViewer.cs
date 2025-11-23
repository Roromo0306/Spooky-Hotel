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

    public void Show(DocumentSO doc)
    {
        string caller = "unknown";
        try
        {
            var st = new System.Diagnostics.StackTrace();
            var f = st.GetFrame(1);
            caller = f != null ? f.GetMethod().Name : "null";
        }
        catch { }

        Debug.Log("[DocumentViewer] Show called for doc: " + (doc != null ? doc.title : "null") + " calledFrom: " + caller);

        if (doc == null)
        {
            Debug.LogWarning("[DocumentViewer] Show called with null doc.");
            return;
        }

        if (documentImage != null)
        {
            if (doc.image != null)
            {
                documentImage.sprite = doc.image;  // 👈 imagen de detalle
                documentImage.SetNativeSize();
                documentImage.enabled = true;
            }
            else
            {
                documentImage.sprite = null;
                documentImage.enabled = false;
            }
        }
        else
        {
            Debug.LogWarning("[DocumentViewer] documentImage reference is null.");
        }

        if (rootPanel != null) rootPanel.SetActive(true);
    }

    public void Close()
    {
        Debug.Log("[DocumentViewer] Close called.");
        if (rootPanel != null) rootPanel.SetActive(false);
        OnClosed?.Invoke();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveAllListeners();
    }
}
