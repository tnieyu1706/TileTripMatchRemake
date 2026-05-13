using UnityEngine;

namespace Game.Core.Gameplay
{
    [CreateAssetMenu(fileName = "LevelDataSo", menuName = "TileTrip/Level Data")]
    public class LevelDataSo : ScriptableObject
    {
        [Header("Generation Rules")] 
        public int tilesNumber = 30;
        public int layersNumber = 3;
        public int rackSlots = 7;
    }
}