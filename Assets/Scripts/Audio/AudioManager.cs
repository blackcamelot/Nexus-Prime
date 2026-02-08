using UnityEngine;

namespace NexusPrime.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        [Header("Audio Sources")]
        public AudioSource musicSource;
        public AudioSource sfxSource;
        public AudioSource uiSource;

        [Header("Volumes")]
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] public float uiVolume = 1f;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
                if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
                if (uiSource == null) uiSource = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlayUISound(string soundId)
        {
            if (uiSource != null)
            {
                var clip = Resources.Load<AudioClip>($"Audio/SFX/UI/{soundId}");
                if (clip != null)
                {
                    uiSource.PlayOneShot(clip, uiVolume);
                }
            }
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (sfxSource != null && clip != null)
                sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicSource != null && clip != null)
            {
                musicSource.clip = clip;
                musicSource.volume = musicVolume;
                musicSource.loop = loop;
                musicSource.Play();
            }
        }
    }
}
