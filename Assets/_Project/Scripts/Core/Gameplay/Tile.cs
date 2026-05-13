using System;
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

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
        [Header("References")] [SerializeField]
        private SpriteRenderer baseRenderer;

        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private BoxCollider2D tileCollider;

        [Header("Visual Settings")] [SerializeField]
        private Color blockedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        [SerializeField] private Color exposedColor = Color.white;

        [Header("Animation Settings")] [SerializeField]
        private float hoverScale = 1.05f; // To lên 5% khi rê chuột

        [SerializeField] private float clickScale = 0.9f; // Lõm xuống 10% khi click
        [SerializeField] private float animDuration = 0.15f;

        public int IconID { get; private set; }
        public Vector3 GridCoordinate { get; private set; }
        public TileState State { get; private set; }

        public event Action<Tile> OnTileClicked;

        private MotionHandle _scaleMotion;
        private Vector3 _originalScale = Vector3.one;

        public void Init(int iconId, Sprite iconSprite, Vector3 gridCoord, int layerIndex)
        {
            IconID = iconId;
            iconRenderer.sprite = iconSprite;
            GridCoordinate = gridCoord;

            SetSortingOrder(layerIndex * 10);
            SetState(TileState.Exposed);

            _originalScale = transform.localScale;
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

                    // Phục hồi kích thước khi rời khỏi bàn
                    PlayScaleAnim(_originalScale, animDuration);
                    break;
            }
        }

        public void SetSortingOrder(int baseOrder)
        {
            if (baseRenderer != null) baseRenderer.sortingOrder = baseOrder;
            if (iconRenderer != null) iconRenderer.sortingOrder = baseOrder + 1;
        }

        private void OnMouseEnter()
        {
            if (State == TileState.Exposed)
            {
                // TODO: Gọi SFX Hover nhẹ nhàng ở đây nếu muốn
                PlayScaleAnim(_originalScale * hoverScale, animDuration);
            }
        }

        private void OnMouseExit()
        {
            if (State == TileState.Exposed)
            {
                PlayScaleAnim(_originalScale, animDuration);
            }
        }

        private void OnMouseDown()
        {
            if (State == TileState.Exposed)
            {
                // TODO: Gọi SFX Click gạch rộp rộp ở đây
                PlayScaleAnim(_originalScale * clickScale, animDuration * 0.5f);
                OnTileClicked?.Invoke(this);
            }
        }

        private void PlayScaleAnim(Vector3 targetScale, float duration)
        {
            if (_scaleMotion.IsActive()) _scaleMotion.Cancel();

            _scaleMotion = LMotion.Create(transform.localScale, targetScale, duration)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(transform);
        }

        private void OnDisable()
        {
            if (_scaleMotion.IsActive()) _scaleMotion.Cancel();
        }
    }
}