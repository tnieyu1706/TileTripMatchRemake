using Game.Core.Data;
using Reflex.Core;
using UnityEngine;

namespace Game.Core.Global
{
    public class GlobalInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private PlayerDataSoap playerData;

        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterValue(playerData);
        }
    }
}