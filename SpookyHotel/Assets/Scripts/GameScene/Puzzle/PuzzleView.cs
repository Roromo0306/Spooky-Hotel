using UnityEngine;
using UnityEngine.UI;

public class PuzzleView : MonoBehaviour
{
    public GameObject rootPanel;
    public GridView gridView;
    public Transform spawnArea;
    public Button closeButton;
    public Button assignButton;

    private void Awake() { if (rootPanel != null) rootPanel.SetActive(false); }
    public void Show() => rootPanel.SetActive(true);
    public void Hide() => rootPanel.SetActive(false);
}

