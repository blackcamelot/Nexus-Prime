using UnityEngine;
using System.Collections.Generic;

namespace NexusPrime.Audio
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance;

        [Header("Music Tracks")]
        public List<AudioClip> mainMenuTracks = new List<AudioClip>();
        public List<AudioClip> battleTracks = new List<AudioClip>();

        private AudioSource source;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                source = GetComponent<AudioSource>();
                if (source == null) source = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlayMainMenu()
        {
            if (mainMenuTracks.Count > 0 && AudioManager.Instance != null)
                AudioManager.Instance.PlayMusic(mainMenuTracks[Random.Range(0, mainMenuTracks.Count)]);
        }

        public void PlayBattle()
        {
            if (battleTracks.Count > 0 && AudioManager.Instance != null)
                AudioManager.Instance.PlayMusic(battleTracks[Random.Range(0, battleTracks.Count)]);
        }
    }
}
