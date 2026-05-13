using System;
using UnityEngine;
using UnityEngine.Pool;
using Reflex.Attributes;
using Cysharp.Threading.Tasks;
using Random = UnityEngine.Random;

namespace Game.Core.Gameplay
{
    /// <summary>
    /// Hệ thống quản lý Âm thanh (chỉ dành riêng cho SFX) sử dụng Object Pool và UniTask.
    /// Vòng đời gắn liền với Scene hiện tại.
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        [Header("SFX Pool Settings")] [SerializeField]
        private AudioSource audioSourcePrefab; // [MỚI] Sử dụng Prefab để dễ dàng setup AudioMixer

        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxSize = 30;

        [Header("SFX Tweak Settings")] [SerializeField]
        private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

        private ObjectPool<AudioSource> _pool;

        private void Awake()
        {
            _pool = new ObjectPool<AudioSource>(
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
            // Ưu tiên instantiate từ Prefab đã setup sẵn (AudioMixer, Volume, Spatial Blend...)
            if (audioSourcePrefab != null)
            {
                AudioSource source = Instantiate(audioSourcePrefab, this.transform);
                source.gameObject.SetActive(false);
                return source;
            }
            else
            {
                // Fallback an toàn nếu chưa gán Prefab
                Debug.LogWarning(
                    "Chưa gán AudioSource Prefab vào SfxManager! Hệ thống đang dùng fallback tạo bằng code.");
                GameObject go = new GameObject("SFX_AudioSource_PoolItem");
                go.transform.SetParent(this.transform);

                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D Sound mặc định

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

            AudioSource source = _pool.Get();
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
                    _pool.Release(source);
                }
            }
            catch (OperationCanceledException)
            {
                // Bỏ qua lỗi êm ái khi đổi Scene. ObjectPool sẽ tự động bị huỷ theo Scene.
            }
        }
    }
}