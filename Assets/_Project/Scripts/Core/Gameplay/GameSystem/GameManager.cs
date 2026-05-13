using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Reflex.Attributes;
using Game.Core.Data; // Nơi chứa PlayerDataSoap

namespace Game.Core.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        [Header("Level Progression")] [SerializeField]
        private List<LevelDataSo> levelList; // Bỏ 10 levels của bạn vào đây

        private BoardController _boardController;
        private RackController _rackController;
        private PlayerDataSoap _playerData; // Data lưu Level hiện tại

        private int _currentLevelIndex = 0;

        // Events cho UI (GameplayMenuGUI) lắng nghe
        public event Action OnLevelWon;
        public event Action OnLevelLost;

        [Inject]
        private void Construct(BoardController boardController, RackController rackController,
            PlayerDataSoap playerData)
        {
            _boardController = boardController;
            _rackController = rackController;
            _playerData = playerData;
        }

        private void Start()
        {
            // Đăng ký sự kiện từ các Core Systems
            _boardController.OnBoardCleared += HandleWinCondition;
            _rackController.OnRackFull += HandleLoseCondition;

            // Tự động load level hiện tại khi vừa vào Gameplay Scene
            if (levelList != null && levelList.Count > 0)
            {
                // Lấy level từ Data (bảo vệ tránh out of range nếu xoá bớt level)
                _currentLevelIndex = Mathf.Clamp(_playerData.CurrentLevelIndex, 0, levelList.Count - 1);
                LoadLevelByIndex(_currentLevelIndex);
            }
            else
            {
                Debug.LogError("GameManager: Chưa có LevelDataSo nào trong levelList!");
            }
        }

        private void OnDestroy()
        {
            if (_boardController != null)
                _boardController.OnBoardCleared -= HandleWinCondition;

            if (_rackController != null)
                _rackController.OnRackFull -= HandleLoseCondition;
        }

        public void LoadLevelByIndex(int index)
        {
            if (index < 0 || index >= levelList.Count) return;

            _currentLevelIndex = index;
            // Chỉ cần quăng Data cho BoardController, phần còn lại Board tự lo
            _boardController.InitializeBoard(levelList[_currentLevelIndex]);
        }

        public void RestartCurrentLevel()
        {
            LoadLevelByIndex(_currentLevelIndex);
        }

        public void LoadNextLevel()
        {
            _currentLevelIndex++;

            if (_currentLevelIndex < levelList.Count)
            {
                // Lưu lại mốc level mới vào PlayerPrefs (thông qua SOAP)
                _playerData.SetLevel(_currentLevelIndex);
                LoadLevelByIndex(_currentLevelIndex);
            }
            else
            {
                Debug.Log("Chúc mừng! Bạn đã hoàn thành toàn bộ 10 Levels.");
                ReturnToHomeScene();
            }
        }

        public void ReturnToHomeScene()
        {
            SceneManager.LoadScene("HomeScene");
        }

        private void HandleWinCondition()
        {
            OnLevelWon?.Invoke(); // Bắn event để WinPanel hiện lên
        }

        private void HandleLoseCondition()
        {
            OnLevelLost?.Invoke(); // Bắn event để LosePanel hiện lên
        }
    }
}