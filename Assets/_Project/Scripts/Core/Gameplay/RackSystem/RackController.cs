using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Gameplay
{
    /// <summary>
    /// Pure Logic & Data Handler (Model/Controller)
    /// </summary>
    public class RackController : MonoBehaviour
    {
        private int _maxSlots;
        private List<Tile> _rackTiles = new List<Tile>();

        public event Action<int> OnRackInitialized;
        public event Action<IReadOnlyList<Tile>> OnRackUpdated;
        public event Action<Tile, Tile, Tile, int> OnTilesMatched;
        public event Action OnRackFull;

        public void Initialize(int slotsCount)
        {
            _maxSlots = slotsCount;

            // [SỬA LỖI]: Phải phá huỷ các GameObject gạch đang nằm trong Rack trước khi xoá Data
            foreach (var tile in _rackTiles)
            {
                if (tile != null)
                {
                    Destroy(tile.gameObject);
                }
            }

            _rackTiles.Clear();
            OnRackInitialized?.Invoke(_maxSlots);
        }

        public bool CanAcceptTile()
        {
            return _rackTiles.Count < _maxSlots;
        }

        public void AddTile(Tile tile)
        {
            if (!CanAcceptTile()) return;

            tile.SetState(TileState.InRack);

            int insertIndex = GetInsertIndex(tile.IconID);
            _rackTiles.Insert(insertIndex, tile);

            OnRackUpdated?.Invoke(_rackTiles);

            if (!CheckForMatches())
            {
                if (_rackTiles.Count >= _maxSlots)
                {
                    OnRackFull?.Invoke();
                }
            }
        }

        private int GetInsertIndex(int iconId)
        {
            int lastIndex = -1;
            for (int i = 0; i < _rackTiles.Count; i++)
            {
                if (_rackTiles[i].IconID == iconId)
                {
                    lastIndex = i;
                }
            }

            return lastIndex != -1 ? lastIndex + 1 : _rackTiles.Count;
        }

        private bool CheckForMatches()
        {
            for (int i = 0; i <= _rackTiles.Count - 3; i++)
            {
                int id = _rackTiles[i].IconID;
                if (_rackTiles[i + 1].IconID == id && _rackTiles[i + 2].IconID == id)
                {
                    Tile t1 = _rackTiles[i];
                    Tile t2 = _rackTiles[i + 1];
                    Tile t3 = _rackTiles[i + 2];

                    _rackTiles.RemoveRange(i, 3);

                    OnTilesMatched?.Invoke(t1, t2, t3, id);
                    OnRackUpdated?.Invoke(_rackTiles);

                    return true;
                }
            }

            return false;
        }
    }
}