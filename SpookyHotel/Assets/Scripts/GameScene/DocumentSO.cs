using UnityEngine;

public enum DocumentType
{
    DNI,
    Reservation
}

[CreateAssetMenu(menuName = "Game/Document", fileName = "DocumentSO")]
public class DocumentSO : ScriptableObject
{
    public string title;
    [TextArea(2, 4)]
    public string description;
    public DocumentType type;
    public Sprite image; // imagen que se mostrará en el visor
}
