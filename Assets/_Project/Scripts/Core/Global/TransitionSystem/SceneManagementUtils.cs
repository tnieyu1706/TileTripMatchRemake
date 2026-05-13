using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SceneManagement
{
    public static class SceneManagementUtils
    {
        public static List<string> GetLoadingScenesName()
        {
            List<string> scenes = new List<string>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    scenes.Add(scene.name);
            }

            return scenes;
        }

        public static List<Scene> GetLoadingScenes()
        {
            List<Scene> scenes = new();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    scenes.Add(scene);
            }

            return scenes;
        }
    }
}