using UnityEngine;

[CreateAssetMenu(menuName = "Game/Cliente", fileName = "ClienteSO")]
public class ClienteSO : ScriptableObject
{
    public string nombre;
    public int id;
    public int numeroNoches;
    [TextArea(2, 6)] public string[] dialogos;

    [Header("Visual")]
    public Sprite[] stageSprites;

    [Header("Documents (per-client)")]
    public DocumentSO dni;
    public DocumentSO reserva;

    // Habilidad placeholder
    public ScriptableObject habilidad;
}

