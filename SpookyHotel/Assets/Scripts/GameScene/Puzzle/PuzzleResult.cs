using System.Linq;
using UnityEngine;

public class PuzzleResult
{
    public int satisfied;
    public int totalPlaced;
    public string[] details;
    public int bestSolutionIndex; // which of the provided solutions matched best (or -1)
}

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

    // EvaluateFinal: compares model against provided solutions if any (PuzzleSolutionsSO)
    public PuzzleResult EvaluateFinal(PuzzleModel model, PuzzleSolutionsSO solutionsSo = null)
    {
        var res = new PuzzleResult();
        // count total placed characters
        var placedCount = model.Cells.Count(c => c != null);
        res.totalPlaced = placedCount;

        // If no solutions provided, fallback: count all placed as 'satisfied' (or run strategy)
        var solutions = solutionsSo != null ? solutionsSo.ParsedSolutions : new string[0];
        if (solutions == null || solutions.Length == 0)
        {
            // Fallback simple: all placed are considered satisfied
            res.satisfied = placedCount;
            res.details = model.Cells.Select((c, i) => c == null ? $"Pos {i}: vacío" : $"Pos {i}: {c.nombre}").ToArray();
            res.bestSolutionIndex = -1;
            return res;
        }

        // Convert model placement to string of 12 chars ('.' for empty)
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
                    detailsList.Add($"Pos {i}: {TypeNameFromChar(placedChar)} OK");
                }
                else if (placedChar == '.')
                {
                    detailsList.Add($"Pos {i}: vacío (esperaba {TypeNameFromChar(solChar)})");
                }
                else
                {
                    detailsList.Add($"Pos {i}: {TypeNameFromChar(placedChar)} NO (esperaba {TypeNameFromChar(solChar)})");
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

    private char CharFromType(CharacterType t)
    {
        switch (t)
        {
            case CharacterType.V: return 'V';
            case CharacterType.W: return 'W';
            case CharacterType.S: return 'S';
            case CharacterType.Z: return 'Z';
            case CharacterType.G: return 'G';
        }
        return '.';
    }

    private string TypeNameFromChar(char c)
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
