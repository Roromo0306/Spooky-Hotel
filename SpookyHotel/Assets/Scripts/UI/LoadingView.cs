using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingView : MonoBehaviour
{
    public Slider progressSlider;
    public TextMeshProUGUI percentText;

    public void SetProgress(float normalized)
    {
        if (progressSlider) progressSlider.value = normalized;
        if (percentText) percentText.text = Mathf.RoundToInt(normalized * 100f) + "%";
    }
}

