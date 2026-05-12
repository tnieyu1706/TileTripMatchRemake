using UnityEngine;
using Reflex.Attributes;

namespace Game.Core.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        private BoardController _boardController;
        private RackController _rackController;

        [SerializeField] private LevelDataSo levelData;

        [Inject]
        private void Construct(BoardController boardController, RackController rackController)
        {
            _boardController = boardController;
            _rackController = rackController;
        }

        private void Start()
        {
            _boardController.OnBoardCleared += HandleWinCondition;
            _rackController.OnRackFull += HandleLoseCondition;

            LoadLevel(levelData);
        }

        private void OnDestroy()
        {
            if (_boardController != null)
                _boardController.OnBoardCleared -= HandleWinCondition;

            if (_rackController != null)
                _rackController.OnRackFull -= HandleLoseCondition;
        }

        public void LoadLevel(LevelDataSo levelDataSource)
        {
            _boardController.InitializeBoard(levelDataSource);
        }

        private void HandleWinCondition()
        {
            Debug.Log("Game Flow: You Win! Triggering UI...");
            // TODO: Gọi UIManager hiện màn hình Win
        }

        private void HandleLoseCondition()
        {
            Debug.Log("Game Flow: You Lose! Rack is full.");
            // TODO: Gọi UIManager hiện màn hình Lose
        }
    }
}