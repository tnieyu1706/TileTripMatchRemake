using UnityEngine;

namespace SceneManagement
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