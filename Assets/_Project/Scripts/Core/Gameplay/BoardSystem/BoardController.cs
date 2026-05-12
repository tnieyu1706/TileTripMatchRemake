using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Reflex.Attributes;

namespace Game.Core.Gameplay
{
    public class BoardController : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Tile tilePrefab;

        [SerializeField] private Transform boardParent;

        [Header("Data")] [SerializeField] private Sprite[] availableIcons;

        private RackController _rackController;
        private List<Tile> activeTiles = new List<Tile>();
        public event Action OnBoardCleared;

        [Inject]
        private void Construct(RackController rackController)
        {
            _rackController = rackController;
        }

        public void InitializeBoard(LevelDataSo levelData)
        {
            ClearBoard();

            // Khởi tạo số lượng Slot cho Rack dựa trên Data của Level này (Cập nhật hàm Initialize theo chuẩn MVC)
            _rackController.Initialize(levelData.rackSlots);

            // 1. Sinh toạ độ gạch tự động thông qua BFS Generator
            List<Vector3> allPositions =
                BoardGenerator.GeneratePositions(levelData.tilesNumber, levelData.layersNumber);

            if (allPositions.Count == 0) return;

            // 2. Phân bổ Icon theo thuật toán Chơi ngược (Bảo đảm giải được)
            int[] assignedIcons = GenerateSolvableIconDistribution(allPositions);

            // 3. Sinh object thực tế
            for (int i = 0; i < allPositions.Count; i++)
            {
                Tile newTile = Instantiate(tilePrefab, allPositions[i], Quaternion.identity, boardParent);
                Sprite iconSprite = availableIcons[assignedIcons[i]];

                newTile.Init(assignedIcons[i], iconSprite, allPositions[i], (int)allPositions[i].z);
                newTile.OnTileClicked += HandleTileClicked;

                activeTiles.Add(newTile);
            }

            UpdateTilesState();
        }

        /// <summary>
        /// Thuật toán đục ngược (Reverse Construction) từ trên xuống dưới
        /// </summary>
        private int[] GenerateSolvableIconDistribution(List<Vector3> positions)
        {
            int totalTiles = positions.Count;
            int[] assignedIconIds = new int[totalTiles];
            bool[] isAssigned = new bool[totalTiles];

            // Lặp cho đến khi gán hết icon (mỗi lần gán 3 viên)
            int assignedCount = 0;
            while (assignedCount < totalTiles)
            {
                List<int> exposedIndices = new List<int>();

                // Tìm tất cả các ô đang "Exposed" (Không bị ô CHƯA GÁN nào đè lên)
                for (int i = 0; i < totalTiles; i++)
                {
                    if (isAssigned[i]) continue;

                    bool isOverlapped = false;
                    for (int j = 0; j < totalTiles; j++)
                    {
                        if (i == j || isAssigned[j]) continue;

                        if (IsOverlapping(positions[i], positions[j]))
                        {
                            isOverlapped = true;
                            break;
                        }
                    }

                    if (!isOverlapped)
                    {
                        exposedIndices.Add(i);
                    }
                }

                // Chọn ngẫu nhiên 3 ô từ danh sách Exposed
                if (exposedIndices.Count >= 3)
                {
                    exposedIndices = exposedIndices.OrderBy(x => UnityEngine.Random.value).Take(3).ToList();
                    int randomIconId = UnityEngine.Random.Range(0, availableIcons.Length);

                    foreach (int index in exposedIndices)
                    {
                        assignedIconIds[index] = randomIconId;
                        isAssigned[index] = true;
                        assignedCount++;
                    }
                }
                else
                {
                    // Fallback an toàn phòng trường hợp map design lỗi (không đủ 3 ô exposed)
                    Debug.LogWarning("Map có thiết kế quá hẹp, thuật toán fallback được kích hoạt.");
                    for (int i = 0; i < totalTiles; i++)
                    {
                        if (!isAssigned[i])
                        {
                            assignedIconIds[i] = UnityEngine.Random.Range(0, availableIcons.Length);
                            isAssigned[i] = true;
                            assignedCount++;
                        }
                    }
                }
            }

            return assignedIconIds;
        }

        /// <summary>
        /// Cập nhật trạng thái Exposed/Blocked cho toàn bộ gạch trên bàn
        /// </summary>
        public void UpdateTilesState()
        {
            foreach (var tile in activeTiles)
            {
                bool isBlocked = false;
                foreach (var other in activeTiles)
                {
                    if (tile == other) continue;

                    if (IsOverlapping(tile.GridCoordinate, other.GridCoordinate))
                    {
                        isBlocked = true;
                        break;
                    }
                }

                tile.SetState(isBlocked ? TileState.Blocked : TileState.Exposed);
            }
        }

        /// <summary>
        /// Logic toán học check xem tile Top có nằm đè lên tile Bottom không.
        /// Kích thước giả định của mỗi tile là 1x1. Khoảng cách < 0.99 để tránh lỗi số thực float.
        /// </summary>
        private bool IsOverlapping(Vector3 bottomPos, Vector3 topPos)
        {
            if (topPos.z <= bottomPos.z) return false;

            bool overlapX = Mathf.Abs(topPos.x - bottomPos.x) < 0.99f;
            bool overlapY = Mathf.Abs(topPos.y - bottomPos.y) < 0.99f;

            return overlapX && overlapY;
        }

        private void HandleTileClicked(Tile clickedTile)
        {
            if (!_rackController.CanAcceptTile()) return;

            clickedTile.OnTileClicked -= HandleTileClicked;
            activeTiles.Remove(clickedTile);

            // Revert: Không gọi SetParent nữa để toạ độ World Space được tính toán chính xác
            _rackController.AddTile(clickedTile);

            UpdateTilesState();

            if (activeTiles.Count == 0)
            {
                OnBoardCleared?.Invoke();
            }
        }

        private void ClearBoard()
        {
            foreach (var tile in activeTiles)
            {
                if (tile != null) Destroy(tile.gameObject);
            }

            activeTiles.Clear();
        }
    }
}