using UnityEngine.TextCore.Text;

public interface IPuzzleService
{
    bool ValidatePlacement(ClienteSO character, int index, PuzzleModel model, out string reason);
    PuzzleResult EvaluateFinal(PuzzleModel model, PuzzleSolutionsSO solutionsSo = null);

    /// <summary>
    /// Devuelve un array booleano de length PuzzleModel.CellCount indicando
    /// qué índices son válidos para colocar `character` dado el estado actual del model.
    /// </summary>
    bool[] GetAllowedIndices(ClienteSO character, PuzzleModel model);
}
