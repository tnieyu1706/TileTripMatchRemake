using UnityEngine;

namespace Game.Core.Data
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "TileTrip/Data/Player Data")]
    public class PlayerDataSoap : ScriptableObject
    {
        private const string LevelKey = "Player_CurrentLevel";
        private const int DefaultLevel = 0;

        public int CurrentLevelIndex { get; private set; }

        private void OnEnable()
        {
            Load();
        }

        private void OnDisable()
        {
            Save();
        }

        public void SetLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
            Save(); // Lưu ngay lập tức khi qua màn để đảm bảo không mất dữ liệu nếu crash game
        }

        public void Load()
        {
            CurrentLevelIndex = PlayerPrefs.GetInt(LevelKey, DefaultLevel);
        }

        public void Save()
        {
            PlayerPrefs.SetInt(LevelKey, CurrentLevelIndex);
            PlayerPrefs.Save();
        }
    }
}