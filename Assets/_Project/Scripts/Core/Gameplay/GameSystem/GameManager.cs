using System;
using System.Collections.Generic;
using UnityEngine;
using Reflex.Attributes;
using Game.Core.Data;
using Game.Core.Global;

namespace Game.Core.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        [Header("Level Progression")] [SerializeField]
        private List<LevelDataSo> levelList;

        [Header("Scene Management")] [SerializeField]
        private SceneGroup homeSceneGroup;

        private BoardController boardController;
        private RackController rackController;
        private PlayerDataSoap playerData;

        private int currentLevelIndex = 0;

        public event Action OnLevelWon;
        public event Action OnLevelLost;

        [Inject]
        private void Construct(
            BoardController boardControllerSource,
            RackController rackControllerSource,
            PlayerDataSoap playerDataSource
        )
        {
            this.boardController = boardControllerSource;
            this.rackController = rackControllerSource;
            this.playerData = playerDataSource;
        }

        private void Start()
        {
            boardController.OnBoardCleared += HandleWinCondition;
            rackController.OnRackFull += HandleLoseCondition;

            if (levelList is { Count: > 0 })
            {
                currentLevelIndex = Mathf.Clamp(playerData.CurrentLevelIndex, 0, levelList.Count - 1);
                LoadLevelByIndex(currentLevelIndex);
            }
            else
            {
                Debug.LogError("[GameManager] levelList is empty!");
            }
        }

        private void OnDestroy()
        {
            if (boardController != null)
                boardController.OnBoardCleared -= HandleWinCondition;

            if (rackController != null)
                rackController.OnRackFull -= HandleLoseCondition;
        }

        public void LoadLevelByIndex(int index)
        {
            if (index < 0 || index >= levelList.Count) return;

            currentLevelIndex = index;
            boardController.InitializeBoard(levelList[currentLevelIndex]);
        }

        public void RestartCurrentLevel()
        {
            // Fast restart without reloading the entire scene
            LoadLevelByIndex(currentLevelIndex);
        }

        public void LoadNextLevel()
        {
            currentLevelIndex++;

            if (currentLevelIndex < levelList.Count)
            {
                playerData.SetLevel(currentLevelIndex);
                LoadLevelByIndex(currentLevelIndex);
            }
            else
            {
                Debug.Log("[GameManager] All levels completed!");
                ReturnToHomeScene();
            }
        }

        public void ReturnToHomeScene()
        {
            if (homeSceneGroup != null)
            {
                SceneLoader.Instance.Load(homeSceneGroup);
            }
            else
            {
                Debug.LogError("[GameManager] homeSceneGroup is missing. Please assign it in the Inspector.");
            }
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