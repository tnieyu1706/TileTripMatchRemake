using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Gameplay
{
    public class RackController : MonoBehaviour
    {
        private int maxSlots;
        private readonly List<Tile> rackTiles = new List<Tile>();

        public event Action<int> OnRackInitialized;
        public event Action<IReadOnlyList<Tile>> OnRackUpdated;
        public event Action<Tile, Tile, Tile, int> OnTilesMatched;
        public event Action OnRackFull;

        public void Initialize(int slotsCount)
        {
            maxSlots = slotsCount;

            // ReInitialize rack state
            foreach (var tile in rackTiles)
            {
                if (tile != null)
                {
                    Destroy(tile.gameObject);
                }
            }

            rackTiles.Clear();
            OnRackInitialized?.Invoke(maxSlots);
        }

        public bool CanAcceptTile()
        {
            return rackTiles.Count < maxSlots;
        }

        public void AddTile(Tile tile)
        {
            if (!CanAcceptTile()) return;

            tile.SetState(TileState.InRack);

            int insertIndex = GetInsertIndex(tile.IconID);
            rackTiles.Insert(insertIndex, tile);

            OnRackUpdated?.Invoke(rackTiles);

            if (!CheckForMatches())
            {
                if (rackTiles.Count >= maxSlots)
                {
                    OnRackFull?.Invoke();
                }
            }
        }

        private int GetInsertIndex(int iconId)
        {
            int lastIndex = -1;
            for (int i = 0; i < rackTiles.Count; i++)
            {
                if (rackTiles[i].IconID == iconId)
                {
                    lastIndex = i;
                }
            }

            return lastIndex != -1 ? lastIndex + 1 : rackTiles.Count;
        }

        private bool CheckForMatches()
        {
            for (int i = 0; i <= rackTiles.Count - 3; i++)
            {
                int id = rackTiles[i].IconID;
                if (rackTiles[i + 1].IconID == id && rackTiles[i + 2].IconID == id)
                {
                    Tile t1 = rackTiles[i];
                    Tile t2 = rackTiles[i + 1];
                    Tile t3 = rackTiles[i + 2];

                    rackTiles.RemoveRange(i, 3);

                    OnTilesMatched?.Invoke(t1, t2, t3, id);
                    OnRackUpdated?.Invoke(rackTiles);

                    return true;
                }
            }

            return false;
        }
    }
}