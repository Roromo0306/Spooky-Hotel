using System;
using System.Collections;
using UnityEngine;

public class DialogController : MonoBehaviour
{
    [SerializeField] private DialogView dialogView;

    [Header("Typing")]
    [Tooltip("Caracteres por segundo (velocidad de 'typewriter').")]
    public float charsPerSecond = 40f;

    [Header("Typing Sound")]
    public AudioSource typingAudioSource;
    public AudioClip typingClip;


    private string[] _lines = new string[0];
    private int _currentLine = -1;
    private bool _isShowing = false;

    private Coroutine _typingCoroutine;
    private bool _isTyping = false;
    private float _lastTypeSoundTime = 0f;

    // Eventos públicos
    public event Action<int> OnLineAdvance;   // índice de línea actual
    public event Action OnDialogFinished;     // cuando el diálogo termina
    public event Action OnTypingStarted;      // cuando empieza a escribirse una línea
    public event Action OnTypingEnded;        // cuando termina de escribirse una línea

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

        if (dialogView == null)
        {
            dialogView = FindObjectOfType<DialogView>();
            if (dialogView == null)
            {
                Debug.LogError("[DialogController] No DialogView found in scene.");
                return;
            }
        }

        dialogView.Show();
        if (!string.IsNullOrEmpty(speakerName))
            dialogView.SetName(speakerName);

        StartTypingCurrentLine();
    }

    private void Update()
    {
        if (!_isShowing) return;

        // ENTER: completar typing o avanzar / cerrar
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (_isTyping)
            {
                CompleteTypingImmediate();
            }
            else
            {
                if (_currentLine + 1 < _lines.Length)
                {
                    AdvanceLine();
                }
                else
                {
                    FinishDialog();
                }
            }
        }

        // ESC: cerrar todo el diálogo (skip)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            FinishDialog();
        }
    }

    private void StartTypingCurrentLine()
    {
        if (_currentLine < 0 || _currentLine >= _lines.Length) return;

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
            _isTyping = false;
        }

        // ✅ REPRODUCIR SONIDO SOLO UNA VEZ POR LÍNEA
        if (typingAudioSource != null && typingClip != null)
        {
            typingAudioSource.PlayOneShot(typingClip);
        }

        _typingCoroutine = StartCoroutine(TypeLineRoutine(_lines[_currentLine]));
    }

    private IEnumerator TypeLineRoutine(string fullLine)
    {
        _isTyping = true;
        OnTypingStarted?.Invoke();

        dialogView?.SetContent("");

        float delayPerChar = 1f / Mathf.Max(charsPerSecond, 1f);
        for (int i = 0; i < fullLine.Length; i++)
        {
            dialogView?.SetContent(fullLine.Substring(0, i + 1));
            yield return new WaitForSeconds(delayPerChar);
        }

        _isTyping = false;
        _typingCoroutine = null;
        OnTypingEnded?.Invoke();
    }

    private void CompleteTypingImmediate()
    {
        if (!_isTyping) return;

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _isTyping = false;
        if (_currentLine >= 0 && _currentLine < _lines.Length)
        {
            dialogView?.SetContent(_lines[_currentLine]);
        }
        OnTypingEnded?.Invoke();
    }

    private void AdvanceLine()
    {
        if (_currentLine < 0) return;

        if (_currentLine + 1 < _lines.Length)
        {
            _currentLine++;
            OnLineAdvance?.Invoke(_currentLine);
            StartTypingCurrentLine();
        }
        else
        {
            Debug.Log("[DialogController] No hay más líneas.");
        }
    }

    private void FinishDialog()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _isTyping = false;
        _isShowing = false;
        _currentLine = -1;
        _lines = new string[0];

        dialogView?.Hide();
        OnDialogFinished?.Invoke();
    }

    public void ForceCloseDialog()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _isTyping = false;
        _isShowing = false;
        _currentLine = -1;
        _lines = new string[0];

        dialogView?.Hide();
        OnDialogFinished?.Invoke();
    }
}
