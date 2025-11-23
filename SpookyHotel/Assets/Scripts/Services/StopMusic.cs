using UnityEngine;

public class StopMusicOnThisScene : MonoBehaviour
{
    private void Start()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.StopMusic();
        }
    }
}
