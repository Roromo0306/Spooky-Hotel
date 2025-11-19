namespace GameScene.Puzzle
{
    public class PuzzleResult
    {
        public int satisfied;
        public int totalPlaced;
        public string[] details;
        public int bestSolutionIndex; // índice de la mejor solución encontrada (o -1)
    }
}
