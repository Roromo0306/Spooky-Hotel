using UnityEngine;

public enum DocumentType
{
    DNI,
    Reservation
}

[CreateAssetMenu(menuName = "Game/Document", fileName = "DocumentSO")]
public class DocumentSO : ScriptableObject
{
    [Header("Detalle (Canvas / Viewer)")]
    public string title;
    [TextArea(2, 4)]
    public string description;
    public DocumentType type;

    /// <summary>
    /// Imagen en detalle que se mostrará en el visor (Canvas).
    /// </summary>
    public Sprite image;

    [Header("Preview en mesa")]
    /// <summary>
    /// Sprite que se verá sobre la mesa (puede ser distinto del detalle).
    /// </summary>
    public Sprite previewSprite;

    /// <summary>
    /// Escala del objeto sobre la mesa (X,Y).
    /// </summary>
    public Vector2 previewScale = Vector2.one;
}
