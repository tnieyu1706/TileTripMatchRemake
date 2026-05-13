using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Reflex.Attributes;
using Game.Core.Data;

namespace Game.Core.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        [Header("Level Progression")] [SerializeField]
        private List<LevelDataSo> levelList;

        private int _currentLevelIndex = 0;
        private BoardController _boardController;
        private RackController _rackController;
        private PlayerDataSoap _playerData;

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
            _boardController.OnBoardCleared += HandleWinCondition;
            _rackController.OnRackFull += HandleLoseCondition;

            // Logic tự động load level tương ứng hiện tại ngay khi Scene bắt đầu
            if (levelList != null && levelList.Count > 0)
            {
                // Đồng bộ biến nội bộ với dữ liệu từ SOAP (Giới hạn kịch trần để tránh lỗi Out of Range)
                _currentLevelIndex = Mathf.Clamp(_playerData.CurrentLevelIndex, 0, levelList.Count - 1);
                LoadLevelByIndex(_currentLevelIndex);
            }
            else
            {
                Debug.LogWarning("Chưa có LevelDataSo nào được thêm vào danh sách levelList của GameManager!");
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
            if (index < 0 || index >= levelList.Count)
            {
                Debug.LogWarning("Đã hoàn thành toàn bộ Level hoặc Level không tồn tại!");
                return;
            }

            _currentLevelIndex = index;
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
                // Cập nhật và lưu lại level mới qua SOAP
                _playerData.SetLevel(_currentLevelIndex);
                LoadLevelByIndex(_currentLevelIndex);
            }
            else
            {
                Debug.Log("Chúc mừng! Bạn đã phá đảo game.");
                ReturnToHomeScene();
            }
        }

        public void ReturnToHomeScene()
        {
            SceneManager.LoadScene("HomeScene");
        }

        private void HandleWinCondition()
        {
            OnLevelWon?.Invoke();
        }

        private void HandleLoseCondition()
        {
            OnLevelLost?.Invoke();
        }
    }
}