using System;
using UnityEngine;

namespace Game.Core.Gameplay
{
    public enum TileState
    {
        Blocked,
        Exposed,
        InRack,
        Matched
    }

    [RequireComponent(typeof(BoxCollider2D))]
    public class Tile : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private BoxCollider2D tileCollider;

        [Header("Visual Settings")]
        [SerializeField] private Color blockedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color exposedColor = Color.white;

        public int IconID { get; private set; }
        public Vector3 GridCoordinate { get; private set; }
        public TileState State { get; private set; }

        public event Action<Tile> OnTileClicked;

        public void Init(int iconId, Sprite iconSprite, Vector3 gridCoord, int layerIndex)
        {
            IconID = iconId;
            iconRenderer.sprite = iconSprite;
            GridCoordinate = gridCoord;
            
            SetSortingOrder(layerIndex * 10);

            SetState(TileState.Exposed); 
        }

        public void SetState(TileState newState)
        {
            State = newState;

            switch (State)
            {
                case TileState.Blocked:
                    baseRenderer.color = blockedColor;
                    iconRenderer.color = blockedColor;
                    tileCollider.enabled = false;
                    break;

                case TileState.Exposed:
                    baseRenderer.color = exposedColor;
                    iconRenderer.color = exposedColor;
                    tileCollider.enabled = true;
                    break;

                case TileState.InRack:
                case TileState.Matched:
                    baseRenderer.color = exposedColor;
                    iconRenderer.color = exposedColor;
                    tileCollider.enabled = false; 
                    break;
            }
        }

        /// <summary>
        /// Hàm public để các hệ thống khác (như Rack) có thể yêu cầu Tile đè lên trên các Tile khác
        /// </summary>
        public void SetSortingOrder(int baseOrder)
        {
            if (baseRenderer != null) baseRenderer.sortingOrder = baseOrder;
            if (iconRenderer != null) iconRenderer.sortingOrder = baseOrder + 1;
        }

        private void OnMouseDown()
        {
            if (State == TileState.Exposed)
            {
                OnTileClicked?.Invoke(this);
            }
        }
    }
}