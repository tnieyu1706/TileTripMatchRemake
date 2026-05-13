using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Reflex.Attributes;

namespace Game.Core.Gameplay
{
    public class GameplayMenuGUI : MonoBehaviour
    {
        [Header("Panels")] [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        [Header("Buttons")] [SerializeField] private Button btnNextLevel;
        [SerializeField] private List<Button> btnRestarts; // Đã chuyển thành List
        [SerializeField] private Button btnHome;

        private GameManager _gameManager;

        [Inject]
        private void Construct(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        private void Awake()
        {
            // Ẩn các menu khi mới vào game
            winPanel.SetActive(false);
            losePanel.SetActive(false);
        }

        private void OnEnable()
        {
            // Đăng ký sự kiện click cho các Buttons
            btnNextLevel.onClick.AddListener(OnNextLevelClicked);

            foreach (var btn in btnRestarts)
            {
                if (btn != null) btn.onClick.AddListener(OnRestartClicked);
            }

            btnHome.onClick.AddListener(OnHomeClicked);

            if (_gameManager != null)
            {
                _gameManager.OnLevelWon += ShowWinMenu;
                _gameManager.OnLevelLost += ShowLoseMenu;
            }
        }

        private void OnDisable()
        {
            btnNextLevel.onClick.RemoveListener(OnNextLevelClicked);

            foreach (var btn in btnRestarts)
            {
                if (btn != null) btn.onClick.RemoveListener(OnRestartClicked);
            }

            btnHome.onClick.RemoveListener(OnHomeClicked);

            if (_gameManager != null)
            {
                _gameManager.OnLevelWon -= ShowWinMenu;
                _gameManager.OnLevelLost -= ShowLoseMenu;
            }
        }

        private void ShowWinMenu()
        {
            winPanel.SetActive(true);
        }

        private void ShowLoseMenu()
        {
            losePanel.SetActive(true);
        }

        private void OnNextLevelClicked()
        {
            winPanel.SetActive(false);
            _gameManager.LoadNextLevel();
        }

        private void OnRestartClicked()
        {
            losePanel.SetActive(false);
            _gameManager.RestartCurrentLevel();
        }

        private void OnHomeClicked()
        {
            // Ẩn panel nếu đang hiển thị
            winPanel.SetActive(false);
            losePanel.SetActive(false);

            _gameManager.ReturnToHomeScene();
        }
    }
}