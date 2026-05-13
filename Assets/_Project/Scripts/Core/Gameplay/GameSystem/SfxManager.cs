using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Game.Core.Gameplay
{
    public class SfxManager : MonoBehaviour
    {
        [Header("SFX Pool Settings")] [SerializeField]
        private AudioSource audioSourcePrefab;

        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxSize = 30;

        [Header("SFX Tweak Settings")] [SerializeField]
        private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

        private ObjectPool<AudioSource> pool;

        private void Awake()
        {
            pool = new ObjectPool<AudioSource>(
                createFunc: CreateAudioSource,
                actionOnGet: OnTakeFromPool,
                actionOnRelease: OnReturnedToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        private AudioSource CreateAudioSource()
        {
            if (audioSourcePrefab != null)
            {
                AudioSource source = Instantiate(audioSourcePrefab, this.transform);
                source.gameObject.SetActive(false);
                return source;
            }
            else
            {
                // Ensure fallback when prefab not exists
                Debug.LogWarning(
                    "Chưa gán AudioSource Prefab vào SfxManager! Hệ thống đang dùng fallback tạo bằng code.");
                GameObject go = new GameObject("SFX_AudioSource_PoolItem");
                go.transform.SetParent(this.transform);

                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;

                return source;
            }
        }

        private void OnTakeFromPool(AudioSource source) => source.gameObject.SetActive(true);

        private void OnReturnedToPool(AudioSource source)
        {
            if (source != null)
            {
                source.Stop();
                source.clip = null;
                source.gameObject.SetActive(false);
            }
        }

        private void OnDestroyPoolObject(AudioSource source)
        {
            if (source != null && source.gameObject != null) Destroy(source.gameObject);
        }

        public void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource source = pool.Get();
            source.clip = clip;
            source.volume = volume;
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.Play();

            ReleaseAfterPlayAsync(source, clip.length).Forget();
        }

        private async UniTaskVoid ReleaseAfterPlayAsync(AudioSource source, float duration)
        {
            if (source == null) return;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration),
                    cancellationToken: source.GetCancellationTokenOnDestroy());

                if (source != null && source.gameObject != null && source.gameObject.activeInHierarchy)
                {
                    pool.Release(source);
                }
            }
            catch (OperationCanceledException)
            {
                // Skip gracefully when Scene changes. ObjectPool will be automatically destroyed with the Scene.
            }
        }
    }
}