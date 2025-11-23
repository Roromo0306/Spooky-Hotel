using UnityEngine;

/// <summary>
/// Componente para un objeto del mundo que abre el puzzle al hacer click.
/// Requiere un Collider2D y una referencia al PuzzleController.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PuzzleWorldClick : MonoBehaviour
{
    [Header("Referencia al controlador del puzzle")]
    public PuzzleController puzzleController;

    private void OnMouseDown()
    {
        if (puzzleController == null)
        {
            Debug.LogWarning("[PuzzleWorldClick] puzzleController no asignado en " + name);
            return;
        }

        puzzleController.OpenPuzzle();
    }
}
