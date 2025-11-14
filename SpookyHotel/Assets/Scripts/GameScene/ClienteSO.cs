using UnityEngine;

[CreateAssetMenu(menuName = "Game/Cliente", fileName = "ClienteSO")]
public class ClienteSO : ScriptableObject
{
    public string nombre;
    public CharacterType type;
    [TextArea(2, 6)] public string[] dialogos;

    [Header("Visual")]
    public Sprite[] stageSprites;
    public Sprite icon;

    [Header("Documents (per-client)")]
    public DocumentSO dni;
    public DocumentSO reserva;

    // Habilidad placeholder
    public ScriptableObject habilidad;

    [Header("Rules")]
    public bool cannotBeInSun = false;
    public bool wantsToBeAlone = false;
    public CharacterType[] cannotBeAdjacentTo;
}

