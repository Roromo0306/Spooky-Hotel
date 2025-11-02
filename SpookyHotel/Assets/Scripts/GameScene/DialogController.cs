using System;
using UnityEngine;

public class DialogController : MonoBehaviour
{
    [SerializeField] private DialogView dialogView;

    private string[] _lines = new string[0];
    private int _currentLine = -1;
    private bool _isShowing = false;

    public event Action<int> OnLineAdvance;      // envia índice de línea actual
    public event Action OnDialogFinished;        // cuando usuario pulsa ENTER

    public void ShowDialog(string[] lines)
    {
        _lines = lines ?? new string[0];
        if (_lines.Length == 0)
        {
            // nada que mostrar
            return;
        }
        _currentLine = 0;
        _isShowing = true;
        if (dialogView != null)
        {
            dialogView.Show();
            dialogView.SetContent(_lines[_currentLine]);
            dialogView.SetName(""); // si quieres, rellénalo con nombre desde manager
            dialogView.hintText.text = "ESC = siguiente | ENTER = irse";
        }
    }

    private void Update()
    {
        if (!_isShowing) return;

        // ESC avanza
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AdvanceLine();
        }

        // ENTER termina el diálogo y hace irse al personaje
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            FinishDialog();
        }
    }

    private void AdvanceLine()
    {
        if (_lines == null || _lines.Length == 0) return;
        _currentLine++;
        if (_currentLine >= _lines.Length)
        {
            // si te pasas del final, mantén en última o auto-llama FinishDialog
            _currentLine = _lines.Length - 1;
            // opcional: FinishDialog();
        }

        if (dialogView != null)
            dialogView.SetContent(_lines[_currentLine]);

        OnLineAdvance?.Invoke(_currentLine);
    }

    private void FinishDialog()
    {
        _isShowing = false;
        _currentLine = -1;
        dialogView.Hide();
        OnDialogFinished?.Invoke();
    }
}
