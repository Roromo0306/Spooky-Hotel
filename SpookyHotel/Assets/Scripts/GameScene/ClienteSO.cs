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

    [Header("Rules")]
    public bool cannotBeInSun = false;
    public bool wantsToBeAlone = false;
    public CharacterType[] cannotBeAdjacentTo;

    [Header("Optional: designer-specified allowed cells (indices 0..11)")]
    [Tooltip("Si no se especifica (array vacío), se usará la estrategia dinámica. Si se especifican índices, solo esas celdas podrán usarse.")]
    public int[] allowedCellIndices;

    // Helper: facilidad para comprobar si tiene configuración explícita
    public bool HasAllowedCells => allowedCellIndices != null && allowedCellIndices.Length > 0;
}
