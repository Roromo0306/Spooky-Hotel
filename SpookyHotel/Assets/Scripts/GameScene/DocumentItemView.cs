using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab item que representa una miniatura clicable de documento (UI).
/// Debe colocarse dentro de un Canvas. El prefab raíz debe tener este componente,
/// un Image (thumbnail) y un Button. No abre el viewer por sí mismo.
/// </summary>
public class DocumentItemView : MonoBehaviour
{
    [Header("References")]
    public Image thumbnail;
    public Button button;

    private DocumentSO _document;

    public event Action<DocumentSO> OnClicked;

    /// <summary>
    /// Inicializa la vista con el DocumentSO correspondiente.
    /// No llama al viewer — solo configura la miniatura y el listener del botón.
    /// </summary>
    public void Initialize(DocumentSO document)
    {
        _document = document;
        Debug.Log("[DocumentItemView] Initialize called for doc: " + (document != null ? document.title : "null"));

        if (thumbnail != null)
        {
            if (document != null && document.image != null)
            {
                thumbnail.sprite = document.image;
                thumbnail.enabled = true;
            }
            else
            {
                thumbnail.sprite = null;
                thumbnail.enabled = false;
            }
        }
        else
        {
            Debug.LogWarning("[DocumentItemView] thumbnail reference is null on " + gameObject.name);
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                Debug.Log("[DocumentItemView] Button clicked for doc: " + (_document != null ? _document.title : "null"));
                OnClicked?.Invoke(_document);
            });
        }
        else
        {
            Debug.LogWarning("[DocumentItemView] button reference is null on " + gameObject.name);
        }
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveAllListeners();
    }
}
