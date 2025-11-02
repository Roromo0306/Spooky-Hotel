using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogView : MonoBehaviour
{
    public GameObject panel;    // panel que contiene UI (activable)
    public TextMeshProUGUI nameText;       // si quieres mostrar nombre
    public TextMeshProUGUI contentText;    // línea actual
    public TextMeshProUGUI hintText;       // "ESC para siguiente, ENTER para irse" opcional

    public void Show()
    {
        if (panel) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
    }

    public void SetContent(string content)
    {
        if (contentText) contentText.text = content;
    }

    public void SetName(string name)
    {
        if (nameText) nameText.text = name;
    }
}

