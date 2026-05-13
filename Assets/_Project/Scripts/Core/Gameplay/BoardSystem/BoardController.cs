using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Reflex.Attributes;

namespace Game.Core.Gameplay
{
    public class BoardController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tile tilePrefab;
        [SerializeField] private Transform boardParent;
        
        [Header("Data")]
        [SerializeField] private Sprite[] availableIcons; 

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

            _rackController.Initialize(levelData.rackSlots);

            // 1. Sinh toạ độ gạch tự động thông qua BFS Generator
            List<Vector3> allPositions = BoardGenerator.GeneratePositions(levelData.tilesNumber, levelData.layersNumber);
            
            if (allPositions.Count == 0) return;

            // 2. [MỚI] Căn giữa và điều chỉnh Camera tự động theo tỉ lệ màn hình
            CenterAndFitBoard(allPositions);

            // 3. Phân bổ Icon theo thuật toán Chơi ngược (Bảo đảm giải được)
            int[] assignedIcons = GenerateSolvableIconDistribution(allPositions);

            // 4. Sinh object thực tế
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
        /// Tính toán khung bao quanh của bàn cờ, dời trọng tâm về (0,0) và Zoom camera cho vừa màn hình
        /// </summary>
        private void CenterAndFitBoard(List<Vector3> positions)
        {
            if (positions.Count == 0) return;

            // 1. Tìm giới hạn min, max của toạ độ
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var pos in positions)
            {
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
            }

            // 2. Tính toạ độ tâm và dời toàn bộ map về giữa (0,0)
            Vector3 centerOffset = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] -= centerOffset;
            }

            // 3. Phóng to/Thu nhỏ Camera để chứa trọn Board
            Camera cam = Camera.main;
            if (cam != null)
            {
                // Thêm padding cho Board: 1 đơn vị hai bên, 4 đơn vị dọc để chừa chỗ cho UI Rack phía dưới
                float boardWidth = (maxX - minX) + 1.5f; 
                float boardHeight = (maxY - minY) + 4.0f;

                float screenAspect = (float)Screen.width / Screen.height;

                // Quy đổi chiều rộng cần thiết sang hệ đo của Orthographic Size
                float requiredSizeX = (boardWidth / screenAspect) * 0.5f;
                float requiredSizeY = boardHeight * 0.5f;

                // Lấy kích thước lớn nhất để không phần nào bị cắt
                float optimalSize = Mathf.Max(requiredSizeX, requiredSizeY);
                
                // Set tối thiểu là 5f để board nhỏ không bị zoom vào quá to
                cam.orthographicSize = Mathf.Max(optimalSize, 5f);

                // Dịch chuyển Camera xuống dưới 1.5 đơn vị (Điều này giúp dời tâm Board nhích lên trên một chút, né phần RackUI)
                cam.transform.position = new Vector3(0, -1.5f, -10f);
            }
        }

        private int[] GenerateSolvableIconDistribution(List<Vector3> positions)
        {
            int totalTiles = positions.Count;
            int[] assignedIconIds = new int[totalTiles];
            bool[] isAssigned = new bool[totalTiles];
            
            int assignedCount = 0;
            while (assignedCount < totalTiles)
            {
                List<int> exposedIndices = new List<int>();

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