using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core.Global
{
    public static class Bootstrapper
    {
        public const string BOOTSTRAPPER_SCENE_NAME = "Bootstrapper";

        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        // static async void Init()
        // {
        //     Debug.Log("Bootstrapper Initialized...");
        //     await SceneManager.LoadSceneAsync(BOOTSTRAPPER_SCENE_NAME, LoadSceneMode.Single);
        // }
    }
}