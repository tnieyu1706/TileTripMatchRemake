using UnityEngine;
using Reflex.Core;

namespace Game.Core.Gameplay
{
    public class GameplayInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private BoardController boardController;
        [SerializeField] private RackController rackController;
        [SerializeField] private GameManager gameManager;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterValue(boardController);
            builder.RegisterValue(rackController);
            builder.RegisterValue(gameManager);
        }
    }
}