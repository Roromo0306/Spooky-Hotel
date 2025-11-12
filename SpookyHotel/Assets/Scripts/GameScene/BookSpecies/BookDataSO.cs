using UnityEngine;

[CreateAssetMenu(fileName = "BookData", menuName = "Game/BookData")]
public class BookDataSO : ScriptableObject
{
    [Header("Metadata")]
    public string title;
    public Sprite cover;

    [Header("Pages (order matters)")]
    public Sprite[] pages;
}
