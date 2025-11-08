using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressBarView : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;     // panel raíz que se activa/desactiva
    public Slider slider;
    public TextMeshProUGUI percentText;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Show(float startValue = 0f)
    {
        if (panel != null) panel.SetActive(true);
        SetProgress(startValue);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void SetProgress(float normalized) // 0..1
    {
        if (slider != null) slider.value = Mathf.Clamp01(normalized);
        if (percentText != null) percentText.text = Mathf.RoundToInt(normalized * 100f) + "%";
    }
}

