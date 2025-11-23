using UnityEngine;

[CreateAssetMenu(menuName = "Game/Cliente", fileName = "ClienteSO")]
public class ClienteSO : ScriptableObject
{
    public string nombre;
    public CharacterType type;

    [TextArea(2, 6)]
    public string[] dialogos;   // diálogos normales (por ejemplo al llegar)

    [Header("Diálogos tras asignar bien en el puzzle")]
    [TextArea(2, 6)]
    public string[] dialogosPuzzleExito;

    [Header("Diálogos tras asignar mal en el puzzle")]
    [TextArea(2, 6)]
    public string[] dialogosPuzzleFallo;

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

    [Header("Optional: designer-specified CORRECT cells (indices 0..11)")]
    [Tooltip("Casillas correctas para este cliente. Las usaremos para saber si está bien colocado.")]
    public int[] allowedCellIndices;

    [Header("Optional: designer-specified WRONG cells (indices 0..11)")]
    [Tooltip("Opcional: casillas marcadamente incorrectas (solo referencia de diseño, no bloquea nada).")]
    public int[] wrongCellIndices;

    public bool HasAllowedCells => allowedCellIndices != null && allowedCellIndices.Length > 0;

    public bool IsCorrectCell(int index)
    {
        if (!HasAllowedCells) return false;
        return System.Array.IndexOf(allowedCellIndices, index) >= 0;
    }

    public bool IsExplicitWrongCell(int index)
    {
        if (wrongCellIndices == null || wrongCellIndices.Length == 0) return false;
        return System.Array.IndexOf(wrongCellIndices, index) >= 0;
    }
}
