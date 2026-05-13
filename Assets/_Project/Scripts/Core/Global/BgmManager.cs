using UnityEngine;
using UnityEngine.Audio;

namespace _Project.Scripts.Core.Global
{
    [RequireComponent(typeof(AudioSource))]
    public class BgmManager : MonoBehaviour
    {
        private static BgmManager instance;
        public static BgmManager Instance => instance;

        public AudioSource audioSource;

        [SerializeField] private AudioResource audioResource;
        [SerializeField] private bool playOnStart;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            if (playOnStart)
            {
                PlayBgm(audioResource);
            }
        }

        public void PlayBgm(AudioResource audioResourceSource)
        {
            audioSource.resource = audioResourceSource;
            audioSource.Play();
        }
    }
}