namespace GameScene.Puzzle
{
    public interface IPuzzleService
    {
        bool ValidatePlacement(ClienteSO character, int index, PuzzleModel model, out string reason);
        bool[] GetAllowedIndices(ClienteSO character, PuzzleModel model);
        PuzzleResult EvaluateFinal(PuzzleModel model, PuzzleSolutionsSO solutionsSo = null);
    }
}
