using UnityEngine;
using Reflex.Core;
using Game.Core.Data; // Import namespace cho PlayerDataSoap

namespace Game.Core.Gameplay
{
    public class GameplayInstaller : MonoBehaviour, IInstaller
    {
        [Header("Controllers")] [SerializeField]
        private BoardController boardController;

        [SerializeField] private RackController rackController;
        [SerializeField] private GameManager gameManager;

        [Header("Data (SOAP)")] [SerializeField]
        private PlayerDataSoap playerData;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterValue(boardController);
            builder.RegisterValue(rackController);
            builder.RegisterValue(gameManager);

            // Bind SOAP Data vào DI Container
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