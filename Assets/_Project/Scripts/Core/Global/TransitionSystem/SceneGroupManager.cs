using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZLinq;

namespace SceneManagement
{
    [Serializable]
    public class SceneGroupManager
    {
        private const int PROGRESS_DELAY = 100;

        public SceneGroup currentActiveSceneGroup;

        public event Action OnPreAllLoad = delegate { };
        public event Action<string> OnOldUnloaded = delegate { };
        public event Action OnOldsCompletedLoad = delegate { };
        public event Action<string> OnNewLoaded = delegate { };
        public event Action OnAllCompletedLoad = delegate { };
        public event Action<string> OnActiveLoaded = delegate { };

        public async UniTaskVoid LoadSceneAsync(SceneGroup sceneGroup, IProgress<float> progress)
        {
            OnPreAllLoad?.Invoke();
            await UnLoadScenesAsync(sceneGroup);

            currentActiveSceneGroup = sceneGroup;

            //prepare: next loading scenes
            List<string> maintainingScenes = SceneManagementUtils.GetLoadingScenesName();
            List<SceneData> loadingScenes = new();
            foreach (var scene in sceneGroup.scenes)
            {
                if (maintainingScenes.Contains(scene.Name)) continue;

                loadingScenes.Add(scene);
            }

            //LoadingScene
            var operationGroup = new AsyncOperationGroup(loadingScenes.Count);
            foreach (var scene in loadingScenes)
            {
                var operation = SceneManager.LoadSceneAsync(scene.Name, LoadSceneMode.Additive);
                if (operation == null) continue;
                operation.completed += _ => OnNewLoaded?.Invoke(scene.Name);

                operationGroup.Operations.Add(operation);
            }

            //Waiting
            while (!operationGroup.IsDone)
            {
                progress?.Report(operationGroup.Progress);
                await UniTask.Delay(PROGRESS_DELAY);
            }

            var sceneActiveName = sceneGroup.FindSceneNameByType(SceneType.Active);
            var sceneNeedActive = SceneManager.GetSceneByName(sceneActiveName);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (sceneNeedActive != null)
            {
                SceneManager.SetActiveScene(sceneNeedActive);
                OnActiveLoaded?.Invoke(sceneActiveName);
            }


            OnAllCompletedLoad?.Invoke();
        }

        private async UniTask UnLoadScenesAsync(SceneGroup sceneGroup)
        {
            var oldScenesName = SceneManagementUtils.GetLoadingScenesName();

            //prepare: unload necessary scenes
            var unloadSceneNames = new List<string>();
            foreach (var scene in oldScenesName)
            {
                if (scene == Bootstrapper.BOOTSTRAPPER_SCENE_NAME) continue;
                if (sceneGroup.scenes
                    .AsValueEnumerable()
                    .Any(s => s.Name == scene && !s.alwaysReload)) continue;

                unloadSceneNames.Add(scene);
            }

            //Handle UnloadScenes
            var operationGroup = new AsyncOperationGroup(unloadSceneNames.Count);
            foreach (var s in unloadSceneNames)
            {
                var operation = SceneManager.UnloadSceneAsync(s);
                operationGroup.Operations.Add(operation);

                OnOldUnloaded?.Invoke(s);
            }

            //Waiting
            while (!operationGroup.IsDone)
            {
                await UniTask.Delay(PROGRESS_DELAY); // tight loop
            }

            // Optional: UnloadUnusedAssets - unload all unused asset from memory 
            // await Resources.UnloadUnusedAssets();

            OnOldsCompletedLoad?.Invoke();
        }
    }

    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> Operations;

        public float Progress =>
            Operations.Count == 0
                ? 0
                : Operations.AsValueEnumerable().Average(o => o.progress);

        public bool IsDone =>
            Operations.Count == 0 || Operations.AsValueEnumerable().All(o => o.isDone);

        public AsyncOperationGroup(int capacity)
        {
            Operations = new List<AsyncOperation>(capacity);
        }
    }
}