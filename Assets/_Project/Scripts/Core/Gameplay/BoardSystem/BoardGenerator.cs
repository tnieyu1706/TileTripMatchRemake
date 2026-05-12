using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Gameplay
{
    public static class BoardGenerator
    {
        /// <summary>
        /// Tạo danh sách toạ độ gạch tự động dựa trên tổng số lượng và số tầng.
        /// </summary>
        public static List<Vector3> GeneratePositions(int totalTiles, int layersCount)
        {
            // Đảm bảo tổng số luôn chia hết cho 3 để không bị deadlock
            totalTiles -= totalTiles % 3;

            List<Vector3> allPositions = new List<Vector3>();
            
            // Phân bổ số lượng Tile cho từng Layer (Tầng dưới nhiều hơn tầng trên)
            int[] tilesPerLayer = DistributeTiles(totalTiles, layersCount);

            // Dùng BFS để lan rộng gạch cho từng Layer
            for (int z = 0; z < layersCount; z++)
            {
                int targetCount = tilesPerLayer[z];
                if (targetCount <= 0) continue;

                // Tầng lẻ lệch 0.5 để tạo cảm giác so le tự nhiên
                float offset = (z % 2 == 0) ? 0f : 0.5f;
                List<Vector2Int> gridPositions = GenerateLayerBFS(targetCount);

                foreach (var gridPos in gridPositions)
                {
                    allPositions.Add(new Vector3(gridPos.x + offset, gridPos.y + offset, z));
                }
            }

            return allPositions;
        }

        /// <summary>
        /// Tính toán số lượng gạch cho mỗi tầng theo công thức tỷ trọng (dưới to, trên nhỏ)
        /// </summary>
        private static int[] DistributeTiles(int total, int layers)
        {
            int[] distribution = new int[layers];
            int remaining = total;
            
            // Tính tổng trọng số. Ví dụ 3 layer -> Trọng số: 3 + 2 + 1 = 6
            int weightSum = (layers * (layers + 1)) / 2; 

            for (int i = 0; i < layers; i++)
            {
                if (i == layers - 1)
                {
                    distribution[i] = remaining; // Tầng trên cùng nhận nốt số dư
                }
                else
                {
                    int weight = layers - i;
                    int count = Mathf.RoundToInt((float)total * weight / weightSum);
                    
                    count = Mathf.Min(count, remaining);
                    distribution[i] = count;
                    remaining -= count;
                }
            }
            return distribution;
        }

        /// <summary>
        /// Thuật toán Loang (BFS) để sinh toạ độ liền kề nhau từ gốc (0,0)
        /// </summary>
        private static List<Vector2Int> GenerateLayerBFS(int targetCount)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            Vector2Int startPos = Vector2Int.zero;
            queue.Enqueue(startPos);
            visited.Add(startPos);

            Vector2Int[] directions = new Vector2Int[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            while (positions.Count < targetCount && queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                positions.Add(current);

                // Trộn ngẫu nhiên hướng đi để tạo ra hình dáng "blob" tự nhiên (tránh bị hình thoi cứng nhắc)
                ShuffleArray(directions);

                foreach (var dir in directions)
                {
                    Vector2Int neighbor = current + dir;
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return positions;
        }

        private static void ShuffleArray(Vector2Int[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                Vector2Int temp = array[i];
                int randomIndex = Random.Range(i, array.Length);
                array[i] = array[randomIndex];
                array[randomIndex] = temp;
            }
        }
    }
}