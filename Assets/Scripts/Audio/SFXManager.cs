using UnityEngine;
using System.Collections.Generic;

namespace NexusPrime.Audio
{
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance;

        [Header("SFX Clips")]
        public List<AudioClip> uiClickClips = new List<AudioClip>();
        public List<AudioClip> uiHoverClips = new List<AudioClip>();

        public void PlayUIClick()
        {
            if (AudioManager.Instance != null && uiClickClips.Count > 0)
                AudioManager.Instance.PlaySFX(uiClickClips[Random.Range(0, uiClickClips.Count)]);
        }

        public void PlayUIHover()
        {
            if (AudioManager.Instance != null && uiHoverClips.Count > 0)
                AudioManager.Instance.PlaySFX(uiHoverClips[Random.Range(0, uiHoverClips.Count)]);
        }
    }
}
