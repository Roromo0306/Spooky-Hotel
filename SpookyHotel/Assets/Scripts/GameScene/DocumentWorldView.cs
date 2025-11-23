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

    public DocumentSO Document => _document;

    public event Action<DocumentSO> OnClicked;

    private void Reset()
    {
        if (previewRenderer == null)
            previewRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(DocumentSO doc)
    {
        _document = doc;

        Debug.Log($"[DocumentWorldView] Initialize {name} con doc: {(doc != null ? doc.title : "null")}");

        // Sprite de preview
        if (previewRenderer != null)
        {
            Sprite spriteToUse = null;

            if (doc != null)
            {
                if (doc.previewSprite != null)
                    spriteToUse = doc.previewSprite;
                else
                    spriteToUse = doc.image;
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

        // Escala
        Vector2 scaleToUse = defaultScale;
        if (doc != null && doc.previewScale != Vector2.zero)
            scaleToUse = doc.previewScale;

        transform.localScale = new Vector3(scaleToUse.x, scaleToUse.y, 1f);
    }

    // Si usas raycast externo (WorldClick2DHandler), llama a esto:
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
