using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Reflex.Attributes;
using Game.Core.Data;
using Game.Core.Global;

namespace Game.Core.Home
{
    public class HomeGUI : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private Button playButton;

        [SerializeField] private TextMeshProUGUI levelText;

        [Header("Scene Management")] [SerializeField]
        private SceneGroup gameplaySceneGroup;

        private PlayerDataSoap playerData;

        [Inject]
        private void Construct(PlayerDataSoap playerDataSource)
        {
            this.playerData = playerDataSource;
        }

        private void OnEnable()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayButtonClicked);
            }

            UpdateLevelText();
        }

        private void OnDisable()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayButtonClicked);
            }
        }

        private void UpdateLevelText()
        {
            if (playerData != null && levelText != null)
            {
                int displayLevel = playerData.CurrentLevelIndex + 1;
                levelText.text = $"Level {displayLevel}";
            }
            else
            {
                Debug.LogWarning("[HomeGUI] Cannot update level text when player data is null");
            }
        }

        private void OnPlayButtonClicked()
        {
            if (gameplaySceneGroup == null)
            {
                Debug.LogWarning("[HomeGUI] No gameplay scene selected");
                return;
            }

            playButton.interactable = false;

            // Transition gameplay scene
            SceneLoader.Instance.Load(gameplaySceneGroup);
        }
    }
}