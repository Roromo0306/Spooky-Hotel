using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AppService : IAppService
{
    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

