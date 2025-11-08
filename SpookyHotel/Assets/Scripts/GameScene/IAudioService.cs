public interface IAudioService : IService
{
    void PlayOneShot(UnityEngine.AudioClip clip, float volume = 1f);
}