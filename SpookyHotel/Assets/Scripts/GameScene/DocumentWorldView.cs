using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class DocumentWorldView : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer previewRenderer;

    [Header("Default Scale (fallback)")]
    public Vector2 defaultScale = Vector2.one;

    private DocumentSO _document;

    /// <summary>
    /// Doc asociado a este objeto del mundo.
    /// </summary>
    public DocumentSO Document => _document;

    /// <summary>
    /// Se dispara cuando el jugador hace clic en este documento del mundo.
    /// </summary>
    public event Action<DocumentSO> OnClicked;

    private void Reset()
    {
        if (previewRenderer == null)
            previewRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Inicializa el objeto del mundo con el DocumentSO correspondiente.
    /// </summary>
    public void Initialize(DocumentSO doc)
    {
        _document = doc;

        Debug.Log($"[DocumentWorldView] Initialize {name} con doc: {(doc != null ? doc.title : "null")}");

        // 1) Sprite de preview sobre la mesa
        if (previewRenderer != null)
        {
            Sprite spriteToUse = null;

            if (doc != null)
            {
                if (doc.previewSprite != null)
                    spriteToUse = doc.previewSprite;  // sprite específico de preview
                else
                    spriteToUse = doc.image;          // fallback: imagen de detalle
            }

            if (spriteToUse != null)
            {
                previewRenderer.sprite = spriteToUse;
                previewRenderer.enabled = true;
            }
            else
            {
                previewRenderer.sprite = null;
                previewRenderer.enabled = false;
            }
        }
        else
        {
            Debug.LogWarning("[DocumentWorldView] previewRenderer no asignado en " + name);
        }

        // 2) Escala en mesa
        Vector2 scaleToUse = defaultScale;
        if (doc != null && doc.previewScale != Vector2.zero)
            scaleToUse = doc.previewScale;

        transform.localScale = new Vector3(scaleToUse.x, scaleToUse.y, 1f);
    }

    /// <summary>
    /// Llamado desde el sistema de clicks 2D cuando se detecta un raycast sobre este objeto.
    /// </summary>
    public void InvokeClick()
    {
        Debug.Log($"[DocumentWorldView] InvokeClick en {name}. Doc: {(_document != null ? _document.title : "null")}");

        if (OnClicked != null)
            OnClicked.Invoke(_document);
        else
            Debug.LogWarning("[DocumentWorldView] InvokeClick llamado pero no hay listeners en OnClicked.");
    }

    private void OnDestroy()
    {
        OnClicked = null;
    }
}
