using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.Events;

namespace SceneManagement
{
    [DefaultExecutionOrder(-500)]
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader _instance;
        public static SceneLoader Instance => _instance;

        public SceneGroupManager manager;
        private bool isLoading;

        #region PROPERTIES

        [SerializeField] private Camera loadingCamera;
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private CanvasGroup loadingCanvasGroup; // Thêm CanvasGroup để Fade mượt mà

        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private UnityEvent<float> onLoadingProgressUpdate;

        #endregion

        protected void Awake()
        {
            _instance = this;

            manager = new SceneGroupManager();
            // Đảm bảo UI Loading ẩn đi lúc đầu
            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.alpha = 0f;
                loadingCanvasGroup.blocksRaycasts = false;
            }

            loadingCanvas.enabled = false;
            loadingCamera.enabled = false;
        }

        private void OnEnable()
        {
            // Bỏ manager.OnPreAllLoad vì ta sẽ tự Fade In trước rồi mới gọi Manager Load
            manager.OnAllCompletedLoad += CompleteSceneGroupLoad;
        }

        private void OnDisable()
        {
            manager.OnAllCompletedLoad -= CompleteSceneGroupLoad;
        }

        #region LOADING EVENTS

        private void CompleteSceneGroupLoad()
        {
            // Khi Load xong mọi thứ, bắt đầu Fade Out
            FadeOutAndCompleteAsync().Forget();
        }

        private async UniTaskVoid FadeOutAndCompleteAsync()
        {
            await LMotion.Create(1f, 0f, fadeDuration)
                .WithEase(Ease.InQuad)
                .Bind(alpha => loadingCanvasGroup.alpha = alpha)
                .ToUniTask();

            loadingCanvasGroup.blocksRaycasts = false;
            loadingCanvas.enabled = false;
            loadingCamera.enabled = false;
            isLoading = false;
        }

        #endregion

        public void Load(SceneGroup sceneGroup)
        {
            if (isLoading)
            {
                Debug.LogWarning("Loading already in progress");
                return;
            }

            // Gọi luồng Load có tích hợp hiệu ứng
            LoadWithFadeAsync(sceneGroup).Forget();
        }

        private async UniTaskVoid LoadWithFadeAsync(SceneGroup sceneGroup)
        {
            isLoading = true;

            // 1. Chuẩn bị UI
            loadingCamera.enabled = true;
            loadingCanvas.enabled = true;
            loadingCanvasGroup.blocksRaycasts = true;

            // 2. Fade In màn hình Loading để che toàn bộ game lại
            await LMotion.Create(0f, 1f, fadeDuration)
                .WithEase(Ease.OutQuad)
                .Bind(alpha => loadingCanvasGroup.alpha = alpha)
                .ToUniTask();

            // Nghỉ 1 nhịp nhỏ để đảm bảo Animation chạy xong mượt mà trước khi CPU bắt tay vào xử lý nặng
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));

            // 3. Tiến hành gọi lõi Load của bạn
            IProgress<float> loadingProgress =
                new Progress<float>(p => onLoadingProgressUpdate?.Invoke(p));

            manager.LoadSceneAsync(sceneGroup, loadingProgress).Forget();
        }
    }
}