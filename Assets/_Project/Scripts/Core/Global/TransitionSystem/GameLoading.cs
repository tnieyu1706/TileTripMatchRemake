using UnityEngine;

namespace Game.Core.Global
{
    public class GameLoading : MonoBehaviour
    {
        [SerializeField] private SceneGroup homeSceneGroup;

        void Start()
        {
            SceneLoader.Instance.Load(homeSceneGroup);
        }
    }
}