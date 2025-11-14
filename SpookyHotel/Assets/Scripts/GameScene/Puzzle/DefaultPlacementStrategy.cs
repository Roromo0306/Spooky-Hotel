using System.Text;

public class DefaultPlacementStrategy : IPlacementStrategy
{
    private int[] GetNeighbors(int index)
    {
        int col = index % PuzzleModel.Columns;
        int row = index / PuzzleModel.Columns;
        var list = new System.Collections.Generic.List<int>();
        if (col > 0) list.Add(index - 1);
        if (col < PuzzleModel.Columns - 1) list.Add(index + 1);
        if (row > 0) list.Add(index - PuzzleModel.Columns);
        if (row < PuzzleModel.Rows - 1) list.Add(index + PuzzleModel.Columns);
        return list.ToArray();
    }

    public bool CanPlace(ClienteSO character, int index, PuzzleModel model, out string failReason)
    {
        failReason = string.Empty;
        if (character == null) { failReason = "Character null"; return false; }
        if (model.Cells[index] != null) { failReason = "Celda ocupada"; return false; }

        // Ghost wants to be alone
        if (character.wantsToBeAlone)
        {
            foreach (var n in GetNeighbors(index))
                if (model.Cells[n] != null) { failReason = "El fantasma quiere estar solo"; return false; }
        }

        // adjacency restrictions (both ways)
        foreach (var n in GetNeighbors(index))
        {
            var other = model.Cells[n];
            if (other == null) continue;
            if (character.cannotBeAdjacentTo != null)
            {
                foreach (var bad in character.cannotBeAdjacentTo)
                    if (other.type == bad) { failReason = $"{character.nombre} no puede estar junto a {other.nombre}"; return false; }
            }
            if (other.cannotBeAdjacentTo != null)
            {
                foreach (var bad2 in other.cannotBeAdjacentTo)
                    if (bad2 == character.type) { failReason = $"{other.nombre} no acepta estar junto a {character.nombre}"; return false; }
            }
        }

        // Vampire can't be next to slime specifically (redundant if SO configured but keep extra check)
        if (character.type == CharacterType.V)
        {
            foreach (var n in GetNeighbors(index))
            {
                var o = model.Cells[n];
                if (o != null && o.type == CharacterType.S) { failReason = "El vampiro no soporta pringue de slime cerca"; return false; }
            }
        }

        return true;
    }
}
