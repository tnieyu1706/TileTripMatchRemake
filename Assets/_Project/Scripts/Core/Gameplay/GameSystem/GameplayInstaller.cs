using UnityEngine;
using Reflex.Core;
using Game.Core.Data;

namespace Game.Core.Gameplay
{
    public class GameplayInstaller : MonoBehaviour, IInstaller
    {
        [Header("Controllers")] [SerializeField]
        private BoardController boardController;

        [SerializeField] private RackController rackController;
        [SerializeField] private GameManager gameManager;

        [Header("Managers")] [SerializeField] private SfxManager sfxManager; // Khai báo SfxManager

        [Header("Data (SOAP)")] [SerializeField]
        private PlayerDataSoap playerData;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterValue(boardController);
            builder.RegisterValue(rackController);
            builder.RegisterValue(gameManager);

            // Đăng ký SfxManager vào DI Container
            if (sfxManager != null)
            {
                builder.RegisterValue(sfxManager);
            }
            else
            {
                Debug.LogWarning("Chưa gán SfxManager vào GameplayInstaller!");
            }

            if (playerData != null)
            {
                builder.RegisterValue(playerData);
            }
            else
            {
                Debug.LogError("Chưa gán PlayerDataSoap vào GameplayInstaller!");
            }
        }
    }
}