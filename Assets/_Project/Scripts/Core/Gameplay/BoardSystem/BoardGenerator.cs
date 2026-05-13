using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Gameplay
{
    public static class BoardGenerator
    {
        public static List<Vector3> GeneratePositions(int totalTiles, int layersCount)
        {
            // Force total to be a multiple of 3 to avoid early deadlocks
            // TODO: Might need a simulation step later to guarantee 100% solvable boards
            totalTiles -= totalTiles % 3;

            List<Vector3> allPositions = new List<Vector3>();
            int[] tilesPerLayer = DistributeTiles(totalTiles, layersCount);

            for (int z = 0; z < layersCount; z++)
            {
                int targetCount = tilesPerLayer[z];
                if (targetCount <= 0) continue;

                // Offset odd layers to create overlapping mechanics
                float offset = (z % 2 == 0) ? 0f : 0.5f;
                List<Vector2Int> gridPositions = GenerateLayerBfs(targetCount);

                foreach (var gridPos in gridPositions)
                {
                    allPositions.Add(new Vector3(gridPos.x + offset, gridPos.y + offset, z));
                }
            }

            return allPositions;
        }

        // Bottom layers get more tiles, top layers get fewer (pyramid distribution)
        private static int[] DistributeTiles(int total, int layers)
        {
            int[] distribution = new int[layers];
            int remaining = total;

            int weightSum = (layers * (layers + 1)) / 2;

            for (int i = 0; i < layers; i++)
            {
                if (i == layers - 1)
                {
                    distribution[i] = remaining;
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

        // Generate organic blob shapes using BFS instead of strict rectangles
        private static List<Vector2Int> GenerateLayerBfs(int targetCount)
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

                // Shuffle directions to prevent perfect diamond-shaped patterns
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