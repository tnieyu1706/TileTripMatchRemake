using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Global
{
    [DefaultExecutionOrder(-500)]
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader instance;
        public static SceneLoader Instance => instance;

        public SceneGroupManager manager;
        private bool isLoading;

        #region PROPERTIES

        [SerializeField] private Camera loadingCamera;
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private CanvasGroup loadingCanvasGroup;

        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private UnityEvent<float> onLoadingProgressUpdate;

        #endregion

        protected void Awake()
        {
            instance = this;

            manager = new SceneGroupManager();
            // Hide CanvasGroup at first
            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.alpha = 0f;
                loadingCanvasGroup.blocksRaycasts = false;
            }

            loadingCanvas.enabled = false;
            loadingCamera.enabled = false;
        }

        #region LOADING EVENTS

        private void OnEnable()
        {
            manager.OnAllCompletedLoad += CompleteSceneGroupLoad;
        }

        private void OnDisable()
        {
            manager.OnAllCompletedLoad -= CompleteSceneGroupLoad;
        }

        private void CompleteSceneGroupLoad()
        {
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

            LoadWithFadeAsync(sceneGroup).Forget();
        }

        private async UniTaskVoid LoadWithFadeAsync(SceneGroup sceneGroup)
        {
            isLoading = true;

            // 1. Prepare UI
            loadingCamera.enabled = true;
            loadingCanvas.enabled = true;
            loadingCanvasGroup.blocksRaycasts = true;

            // 2. Fade-in SceneLoading
            await LMotion.Create(0f, 1f, fadeDuration)
                .WithEase(Ease.OutQuad)
                .Bind(alpha => loadingCanvasGroup.alpha = alpha)
                .ToUniTask();

            // Temporary delay for ensure UI update before start loading, can be removed if your loading process is heavy enough to cause visible frame drop at the beginning
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));

            // 3. Load SceneGroup
            IProgress<float> loadingProgress =
                new Progress<float>(p => onLoadingProgressUpdate?.Invoke(p));

            manager.LoadSceneAsync(sceneGroup, loadingProgress).Forget();
        }
    }
}