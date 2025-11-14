using UnityEngine;

[CreateAssetMenu(fileName = "PuzzleSolutions", menuName = "Game/PuzzleSolutions")]
public class PuzzleSolutionsSO : ScriptableObject
{
    [Tooltip("Introduce cada solución como 12 caracteres (row-major): 'V.WZS...G..' OR como 4 filas separadas usando '\\n'.")]
    [TextArea(4, 8)]
    public string solutionsText; // paste the 12 solutions text here (human-readable)

    // Internally parsed solutions as array of normalized 12-char strings
    public string[] ParsedSolutions
    {
        get
        {
            if (string.IsNullOrEmpty(solutionsText)) return new string[0];
            // parse lines, split on known separators (empty lines between solutions)
            var lines = solutionsText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            var sols = new System.Collections.Generic.List<string>();
            var buffer = new System.Text.StringBuilder();
            foreach (var l in lines)
            {
                var trimmed = l.Trim();
                if (trimmed.ToLower().StartsWith("solución") || trimmed.ToLower().StartsWith("solucion"))
                {
                    // skip header lines
                    if (buffer.Length == 12) { sols.Add(buffer.ToString()); buffer.Clear(); }
                    continue;
                }

                // a solution row may look like "V.W" or "ZS." etc. append
                buffer.Append(trimmed);
                // when we have 12 chars (3*4) we accept solution OR if we detect 3-char rows collection (4 rows)
                if (buffer.Length >= 12)
                {
                    // take exactly first 12 characters
                    var sol = buffer.ToString().Substring(0, 12);
                    sols.Add(sol);
                    buffer.Clear();
                }
            }

            // Fallback: if buffer has content but less than 12, ignore.
            return sols.ToArray();
        }
    }
}


