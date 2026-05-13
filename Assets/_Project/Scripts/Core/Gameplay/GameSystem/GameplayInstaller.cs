using Reflex.Core;
using UnityEngine;

namespace Game.Core.Gameplay
{
    public class GameplayInstaller : MonoBehaviour, IInstaller
    {
        [Header("Controllers")] [SerializeField]
        private BoardController boardController;

        [SerializeField] private RackController rackController;
        [SerializeField] private GameManager gameManager;

        [Header("Managers")] 
        [SerializeField] private SfxManager sfxManager;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterValue(boardController);
            builder.RegisterValue(rackController);
            builder.RegisterValue(gameManager);

            builder.RegisterValue(sfxManager);
        }
    }
}