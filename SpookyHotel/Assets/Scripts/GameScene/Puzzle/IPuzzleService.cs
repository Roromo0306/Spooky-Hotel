public interface IPuzzleService
{
    bool ValidatePlacement(ClienteSO character, int index, PuzzleModel model, out string reason);
    PuzzleResult EvaluateFinal(PuzzleModel model, PuzzleSolutionsSO solutionsSo = null);
}
