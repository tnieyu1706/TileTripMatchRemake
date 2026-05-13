using UnityEngine;

namespace Game.Core.Data
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "TileTrip/Data/Player Data")]
    public class PlayerDataSoap : ScriptableObject
    {
        private const string LEVEL_KEY = "Player_CurrentLevel";
        private const int DEFAULT_LEVEL = 0;

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
            Save();
        }

        public void Load()
        {
            CurrentLevelIndex = PlayerPrefs.GetInt(LEVEL_KEY, DEFAULT_LEVEL);
        }

        public void Save()
        {
            PlayerPrefs.SetInt(LEVEL_KEY, CurrentLevelIndex);
            PlayerPrefs.Save();
        }
    }
}