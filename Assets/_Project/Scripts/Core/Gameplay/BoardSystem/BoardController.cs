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

        private RackController rackController;
        private SfxManager sfxManager;
        private readonly List<Tile> activeTiles = new List<Tile>();
        public event Action OnBoardCleared;

        [Inject]
        private void Construct(RackController rackControllerSource, SfxManager sfxManagerSource)
        {
            this.rackController = rackControllerSource;
            this.sfxManager = sfxManagerSource;
        }

        public void InitializeBoard(LevelDataSo levelData)
        {
            ClearBoard();

            rackController.Initialize(levelData.rackSlots);

            List<Vector3> allPositions =
                BoardGenerator.GeneratePositions(levelData.tilesNumber, levelData.layersNumber);

            if (allPositions.Count == 0) return;

            int[] assignedIcons = GenerateSolvableIconDistribution(allPositions);

            for (int i = 0; i < allPositions.Count; i++)
            {
                Tile newTile = Instantiate(tilePrefab, allPositions[i], Quaternion.identity, boardParent);
                Sprite iconSprite = availableIcons[assignedIcons[i]];

                newTile.Init(assignedIcons[i], iconSprite, allPositions[i], (int)allPositions[i].z, sfxManager);
                newTile.OnTileClicked += HandleTileClicked;

                activeTiles.Add(newTile);
            }

            UpdateTilesState();
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
                    Debug.LogWarning(
                        "Using Fallback algorithm for icon assignment due to insufficient exposed tiles. This may lead to less optimal distributions.");

                    List<int> remainingIndices = new List<int>();
                    for (int i = 0; i < totalTiles; i++)
                    {
                        if (!isAssigned[i]) remainingIndices.Add(i);
                    }

                    remainingIndices = remainingIndices.OrderBy(x => UnityEngine.Random.value).ToList();

                    for (int i = 0; i < remainingIndices.Count; i += 3)
                    {
                        int randomIconId = UnityEngine.Random.Range(0, availableIcons.Length);
                        for (int j = 0; j < 3 && (i + j) < remainingIndices.Count; j++)
                        {
                            int index = remainingIndices[i + j];
                            assignedIconIds[index] = randomIconId;
                            isAssigned[index] = true;
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
            if (!rackController.CanAcceptTile()) return;

            clickedTile.OnTileClicked -= HandleTileClicked;
            activeTiles.Remove(clickedTile);

            rackController.AddTile(clickedTile);

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