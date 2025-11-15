// Assets/Scripts/Services/PuzzleService.cs
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// Resultado de la evaluación final del puzzle.
/// </summary>
public class PuzzleResult
{
    public int satisfied;
    public int totalPlaced;
    public string[] details;
    public int bestSolutionIndex; // índice de la mejor solución encontrada (o -1)
}

/// <summary>
/// Servicio que encapsula la lógica del puzzle (validaciones por estrategia,
/// cálculo de índices permitidos y evaluación final contra soluciones).
/// </summary>
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

        for (int i = 0; i < PuzzleModel.CellCount; i++)
        {
            // Only allow placing on empty cells
            if (model.Cells[i] != null) { allowed[i] = false; continue; }

            string dummyReason;
            allowed[i] = _strategy.CanPlace(character, i, model, out dummyReason);
        }

        return allowed;
    }

    /// <summary>
    /// Compara la disposición final del modelo con las soluciones dadas (PuzzleSolutionsSO).
    /// Si no se proporcionan soluciones, devuelve todos los colocados como satisfechos.
    /// </summary>
    public PuzzleResult EvaluateFinal(PuzzleModel model, PuzzleSolutionsSO solutionsSo = null)
    {
        var res = new PuzzleResult();

        // total de piezas colocadas en el tablero
        var placedCount = model.Cells.Count(c => c != null);
        res.totalPlaced = placedCount;

        // parse solutions
        var solutions = solutionsSo != null ? solutionsSo.ParsedSolutions : new string[0];
        if (solutions == null || solutions.Length == 0)
        {
            // fallback: considerar todas las piezas como satisfechas (sin soluciones de referencia)
            res.satisfied = placedCount;
            res.details = model.Cells.Select((c, i) => c == null ? $"Pos {i}: vacío" : $"Pos {i}: {c.characterName}").ToArray();
            res.bestSolutionIndex = -1;
            return res;
        }

        // Convertimos el estado actual del modelo a una cadena de 12 chars ('.' si vacío)
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

    // -----------------------
    // Helpers
    // -----------------------
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

    /// <summary>
    /// Devuelve nombre legible a partir de un char ('V','W','S','Z','G' o '.').
    /// Renombrado a GetTypeNameFromChar para evitar conflictos/ambigüedades.
    /// </summary>
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
