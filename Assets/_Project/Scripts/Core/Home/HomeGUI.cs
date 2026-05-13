using UnityEngine;
using UnityEngine.UI;
using TMPro; // Sử dụng TextMeshPro cho UI Text
using Reflex.Attributes;
using Game.Core.Data;
using SceneManagement;

namespace Game.Core.Home
{
    public class HomeGUI : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private Button playButton;

        [SerializeField] private TextMeshProUGUI levelText;

        [Header("Scene Management")] [SerializeField]
        private SceneGroup gameplaySceneGroup; // Kéo thả SceneGroup của Gameplay vào đây

        private PlayerDataSoap _playerData;

        [Inject]
        private void Construct(PlayerDataSoap playerData)
        {
            _playerData = playerData;
        }

        private void OnEnable()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayButtonClicked);
            }

            // Luôn đồng bộ dữ liệu Level mới nhất mỗi khi màn hình Home hiển thị lên
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
            if (_playerData != null && levelText != null)
            {
                // Cộng 1 vì CurrentLevelIndex thường bắt đầu từ 0 trong logic hệ thống
                int displayLevel = _playerData.CurrentLevelIndex + 1;
                levelText.text = $"Level {displayLevel}";
            }
            else
            {
                Debug.LogWarning("[HomeGUI] Thiếu tham chiếu PlayerData hoặc LevelText chưa được gán!");
            }
        }

        private void OnPlayButtonClicked()
        {
            if (gameplaySceneGroup == null)
            {
                Debug.LogError("[HomeGUI] Chưa gán Gameplay Scene Group!");
                return;
            }

            // Vô hiệu hóa nút để tránh người chơi spam click 
            // trong lúc Animation Fade Out đang chạy
            playButton.interactable = false;

            // Chuyển Scene thông qua hệ thống SceneLoader của bạn
            SceneLoader.Instance.Load(gameplaySceneGroup);
        }
    }
}