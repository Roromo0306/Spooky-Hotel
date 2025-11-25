using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Music Source")]
    public AudioSource audioSource;

    [Header("Scene where music should auto-resume")]
    public string sceneToResumeMusic = "MainScene"; // <-- pon aquí el nombre de la escena

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource != null)
        {
            audioSource.loop = true;

            if (!audioSource.isPlaying)
                audioSource.Play();
        }

        // Suscribirse al cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Evitar que queden suscripciones huérfanas
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si la escena cargada coincide con la definida y la música no está sonando → reanudar
        if (scene.name == sceneToResumeMusic)
        {
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log("[MusicManager] Música reanudada en escena: " + scene.name);
            }
        }
    }

    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    public void PlayMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }
}
