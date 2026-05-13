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
        [SerializeField] private List<Button> btnRestarts;
        [SerializeField] private Button btnHome;

        private GameManager gameManager;

        [Inject]
        private void Construct(GameManager gameManagerSource)
        {
            this.gameManager = gameManagerSource;
        }

        private void Awake()
        {
            // Hide panels at the start
            winPanel.SetActive(false);
            losePanel.SetActive(false);
        }

        #region Events

        private void OnEnable()
        {
            // Register events
            btnNextLevel.onClick.AddListener(OnNextLevelClicked);

            foreach (var btn in btnRestarts)
            {
                if (btn != null) btn.onClick.AddListener(OnRestartClicked);
            }

            btnHome.onClick.AddListener(OnHomeClicked);

            if (gameManager != null)
            {
                gameManager.OnLevelWon += ShowWinMenu;
                gameManager.OnLevelLost += ShowLoseMenu;
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

            if (gameManager != null)
            {
                gameManager.OnLevelWon -= ShowWinMenu;
                gameManager.OnLevelLost -= ShowLoseMenu;
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
            gameManager.LoadNextLevel();
        }

        private void OnRestartClicked()
        {
            losePanel.SetActive(false);
            gameManager.RestartCurrentLevel();
        }

        private void OnHomeClicked()
        {
            // Hide current display panel
            winPanel.SetActive(false);
            losePanel.SetActive(false);

            gameManager.ReturnToHomeScene();
        }

        #endregion
    }
}