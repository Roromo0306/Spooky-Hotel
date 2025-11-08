
using UnityEngine;

public class AudioService : IAudioService
{
    private GameObject _audioHolder;
    private AudioSource _source;

    public AudioService()
    {
        _audioHolder = new GameObject("[AudioService]");
        Object.DontDestroyOnLoad(_audioHolder);
        _source = _audioHolder.AddComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        _source.PlayOneShot(clip, volume);
    }
}

