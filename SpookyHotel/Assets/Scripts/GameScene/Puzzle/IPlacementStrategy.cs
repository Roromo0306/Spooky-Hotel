public interface IPlacementStrategy
{
    bool CanPlace(ClienteSO character, int index, PuzzleModel model, out string failReason);
}