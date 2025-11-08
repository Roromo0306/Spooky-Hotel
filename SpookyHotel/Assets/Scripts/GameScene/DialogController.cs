using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class DialogController : MonoBehaviour
{
    [SerializeField] private DialogView dialogView;

    [Header("Typing")]
    [Tooltip("Caracteres por segundo (velocidad de 'typewriter').")]
    public float charsPerSecond = 40f;

    private string[] _lines = new string[0];
    private int _currentLine = -1;
    private bool _isShowing = false;

    private Coroutine _typingCoroutine;
    private bool _isTyping = false;

    public event Action<int> OnLineAdvance;         // índice de línea actual (cuando cambia línea completa)
    public event Action OnDialogFinished;           // cuando el usuario presiona ENTER para terminar todo el diálogo
    public event Action OnTypingStarted;            // se dispara al empezar a tipearse una línea
    public event Action OnTypingEnded;              // se dispara cuando termina de tipearse la línea (completada)

    public void ShowDialog(string[] lines, string speakerName = null)
    {
        _lines = lines ?? new string[0];
        if (_lines.Length == 0)
        {
            Debug.Log("[DialogController] No lines to show.");
            return;
        }

        _currentLine = 0;
        _isShowing = true;

        if (dialogView != null)
        {
            dialogView.Show();
            if (!string.IsNullOrEmpty(speakerName)) dialogView.SetName(speakerName);
        }

        StartTypingCurrentLine();
    }

    private void Update()
    {
        if (!_isShowing) return;

        // ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isTyping)
            {
                // completar inmediatamente la línea actual
                CompleteTypingImmediate();
            }
            else
            {
                AdvanceLine();
            }
        }

        // ENTER
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            FinishDialog();
        }
    }

    private void StartTypingCurrentLine()
    {
        if (_currentLine < 0 || _currentLine >= _lines.Length) return;

        // si ya hay typing coroutine, pararla
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
            _isTyping = false;
        }

        _typingCoroutine = StartCoroutine(TypeLineRoutine(_lines[_currentLine]));
    }

    private IEnumerator TypeLineRoutine(string fullLine)
    {
        _isTyping = true;
        OnTypingStarted?.Invoke();

        if (dialogView != null) dialogView.SetContent(string.Empty);

        if (charsPerSecond <= 0f) charsPerSecond = 40f;
        float delayPerChar = 1f / charsPerSecond;
        int len = fullLine.Length;
        int i = 0;

        while (i < len)
        {
            // Añadir siguiente carácter
            string sub = fullLine.Substring(0, i + 1);
            if (dialogView != null) dialogView.SetContent(sub);
            i++;
            yield return new WaitForSeconds(delayPerChar);
        }

        // terminado
        _isTyping = false;
        _typingCoroutine = null;
        OnTypingEnded?.Invoke();
    }

    private void CompleteTypingImmediate()
    {
        if (!_isTyping) return;

        // Detener coroutine y escribir la línea completa
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _isTyping = false;
        if (_currentLine >= 0 && _currentLine < _lines.Length)
        {
            if (dialogView != null) dialogView.SetContent(_lines[_currentLine]);
        }
        OnTypingEnded?.Invoke();
    }

    private void AdvanceLine()
    {
        // Si está en última línea, no hace nada (el ENTER se encarga de terminar)
        if (_currentLine < 0) return;
        if (_currentLine + 1 < _lines.Length)
        {
            _currentLine++;
            OnLineAdvance?.Invoke(_currentLine);
            StartTypingCurrentLine();
        }
        else
        {
            // no hay siguiente línea; dejar la última (o podríamos auto-finish)
            Debug.Log("[DialogController] No hay más líneas que avanzar.");
        }
    }

    private void FinishDialog()
    {
        // terminar el diálogo completamente (se usará para que el cliente se vaya)
        // detener typing
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
        _isTyping = false;

        // ocultar UI
        if (dialogView != null) dialogView.Hide();

        // notificar
        OnDialogFinished?.Invoke();
    }
}
