using UnityEngine;

namespace Game.Core.Gameplay
{
    [CreateAssetMenu(fileName = "LevelDataSo", menuName = "TileTrip/Level Data (Auto Gen)")]
    public class LevelDataSo : ScriptableObject
    {
        [Header("Generation Rules")]
        [Tooltip("Tổng số viên gạch trên bàn (Sẽ tự động làm tròn xuống để chia hết cho 3)")]
        public int tilesNumber = 30; 
        
        [Tooltip("Số tầng (Layer) tối đa")]
        public int layersNumber = 3;

        [Header("Rack Settings")]
        public int rackSlots = 7;
    }
}