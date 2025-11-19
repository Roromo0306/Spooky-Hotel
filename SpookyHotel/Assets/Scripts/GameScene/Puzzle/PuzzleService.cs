using System.Linq;
using UnityEngine;

namespace GameScene.Puzzle
{
    public class PuzzleService : IPuzzleService
    {
        private IPlacementStrategy _strategy;

        public PuzzleService(IPlacementStrategy strategy)
        {
            _strategy = strategy;
        }

        public bool ValidatePlacement(ClienteSO character, int index, PuzzleModel model, out string reason)
        {
            return _strategy.CanPlace(character, index, model, out reason);
        }
        public bool[] GetAllowedIndices(ClienteSO character, PuzzleModel model)
        {
            var allowed = new bool[PuzzleModel.CellCount];
            if (character == null || model == null) return allowed;

            // Precompute set if character defines allowed cells
            System.Collections.Generic.HashSet<int> allowedSet = null;
            if (character.HasAllowedCells)
            {
                allowedSet = new System.Collections.Generic.HashSet<int>(character.allowedCellIndices);
            }

            for (int i = 0; i < PuzzleModel.CellCount; i++)
            {
                // If designer specified allowed cells and this index isn't one of them, skip
                if (allowedSet != null && !allowedSet.Contains(i))
                {
                    allowed[i] = false;
                    continue;
                }

                // Only allow placing on empty cells (or allow if it's the same instance already there)
                if (model.Cells[i] != null)
                {
                    allowed[i] = false;
                    continue;
                }

                string dummyReason;
                bool strategyOk = _strategy.CanPlace(character, i, model, out dummyReason);
                allowed[i] = strategyOk;
            }

            return allowed;
        }

        public PuzzleResult EvaluateFinal(PuzzleModel model, PuzzleSolutionsSO solutionsSo = null)
        {
            var res = new PuzzleResult();

            var placedCount = model.Cells.Count(c => c != null);
            res.totalPlaced = placedCount;

            var solutions = solutionsSo != null ? solutionsSo.ParsedSolutions : new string[0];
            if (solutions == null || solutions.Length == 0)
            {
                res.satisfied = placedCount;
                res.details = model.Cells.Select((c, i) => c == null ? $"Pos {i}: vacío" : $"Pos {i}: {c.nombre}").ToArray();
                res.bestSolutionIndex = -1;
                return res;
            }

            string modelStr = "";
            for (int i = 0; i < PuzzleModel.CellCount; i++)
            {
                var c = model.Cells[i];
                modelStr += (c == null) ? '.' : CharFromType(c.type);
            }

            int bestSat = -1;
            int bestIdx = -1;
            string[] bestDetails = null;

            for (int s = 0; s < solutions.Length; s++)
            {
                var sol = solutions[s];
                if (string.IsNullOrEmpty(sol) || sol.Length < PuzzleModel.CellCount) continue;

                int sat = 0;
                var detailsList = new System.Collections.Generic.List<string>();

                for (int i = 0; i < PuzzleModel.CellCount; i++)
                {
                    char solChar = sol[i];
                    char placedChar = modelStr[i];

                    if (placedChar != '.' && placedChar == solChar)
                    {
                        sat++;
                        detailsList.Add($"Pos {i}: {GetTypeNameFromChar(placedChar)} OK");
                    }
                    else if (placedChar == '.')
                    {
                        detailsList.Add($"Pos {i}: vacío (esperaba {GetTypeNameFromChar(solChar)})");
                    }
                    else
                    {
                        detailsList.Add($"Pos {i}: {GetTypeNameFromChar(placedChar)} NO (esperaba {GetTypeNameFromChar(solChar)})");
                    }
                }

                if (sat > bestSat)
                {
                    bestSat = sat;
                    bestIdx = s;
                    bestDetails = detailsList.ToArray();
                }
            }

            res.satisfied = bestSat >= 0 ? bestSat : 0;
            res.details = bestDetails ?? new string[0];
            res.bestSolutionIndex = bestIdx;
            return res;
        }

        // Helpers
        private char CharFromType(CharacterType t)
        {
            switch (t)
            {
                case CharacterType.V: return 'V';
                case CharacterType.W: return 'W';
                case CharacterType.S: return 'S';
                case CharacterType.Z: return 'Z';
                case CharacterType.G: return 'G';
                default: return '.';
            }
        }

        private string GetTypeNameFromChar(char c)
        {
            switch (c)
            {
                case 'V': return "Vampiro";
                case 'W': return "Hombre Lobo";
                case 'S': return "Slime";
                case 'Z': return "Zombie";
                case 'G': return "Fantasma";
                default: return ".";
            }
        }
    }
}
