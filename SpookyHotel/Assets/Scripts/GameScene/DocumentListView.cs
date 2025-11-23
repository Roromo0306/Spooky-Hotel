using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Panel que muestra un conjunto de DocumentItemView instanciados dinámicamente.
/// rootPanel: panel raíz (activar/desactivar).
/// container: transform donde instanciar items.
/// itemPrefab: prefab de DocumentItemView (arrastrar prefab en el inspector).
/// </summary>
public class DocumentListView : MonoBehaviour
{
    [Header("References")]
    public GameObject rootPanel;          // panel que se activa/desactiva
    public Transform container;           // contenedor donde instanciar items
    public DocumentItemView itemPrefab;   // prefab (DocumentItemView)

    private readonly List<DocumentItemView> _spawned = new List<DocumentItemView>();

    /// <summary>
    /// Evento que notifica al seleccionar un documento.
    /// </summary>
    public event Action<DocumentSO> OnDocumentSelected;

    private void Awake()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    /// <summary>
    /// Muestra miniaturas para cada DocumentSO (ignora nulos).
    /// </summary>
    public void ShowDocuments(DocumentSO[] docs)
    {
        Debug.Log("[DocumentListView] ShowDocuments called. docs == null? " + (docs == null));
        Clear();

        if (docs == null || docs.Length == 0)
        {
            Debug.Log("[DocumentListView] docs null/empty -> hiding panel.");
            if (rootPanel != null) rootPanel.SetActive(false);
            return;
        }

        bool allNull = true;
        foreach (var d in docs) if (d != null) { allNull = false; break; }
        Debug.Log("[DocumentListView] docs allNull? " + allNull);
        if (allNull)
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            return;
        }

        if (rootPanel != null) rootPanel.SetActive(true);

        if (itemPrefab == null)
        {
            Debug.LogError("[DocumentListView] itemPrefab is null — assign the DocumentItemView prefab in the inspector.");
            return;
        }
        if (container == null)
        {
            Debug.LogError("[DocumentListView] container is null — assign a container Transform in the inspector.");
            return;
        }

        foreach (var doc in docs)
        {
            if (doc == null) continue;

            Debug.Log("[DocumentListView] Instantiating item for doc: " + doc.title);
            var item = Instantiate(itemPrefab, container);
            if (item == null)
            {
                Debug.LogError("[DocumentListView] Failed to instantiate itemPrefab.");
                continue;
            }

            item.Initialize(doc);
            item.OnClicked += HandleItemClicked;
            _spawned.Add(item);
        }
    }

    private void HandleItemClicked(DocumentSO doc)
    {
        Debug.Log("[DocumentListView] HandleItemClicked -> doc: " + (doc != null ? doc.title : "null"));
        OnDocumentSelected?.Invoke(doc);
    }

    /// <summary>
    /// Oculta el panel y destruye los items spawneds.
    /// </summary>
    public void Hide()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        Clear();
    }

    /// <summary>
    /// Destruye items y limpia listeners.
    /// </summary>
    public void Clear()
    {
        foreach (var it in _spawned)
        {
            if (it == null) continue;
            it.OnClicked -= HandleItemClicked;
            try { Destroy(it.gameObject); } catch { }
        }
        _spawned.Clear();
    }

    private void OnDestroy()
    {
        Clear();
    }
}
