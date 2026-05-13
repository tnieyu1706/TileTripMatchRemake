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
        private float hoverScale = 1.05f;

        [SerializeField] private float tapScale = 0.9f;
        [SerializeField] private float animDuration = 0.15f;

        [Header("Audio")] [SerializeField] private AudioClip tapClip;

        public int IconID { get; private set; }
        public Vector3 GridCoordinate { get; private set; }
        public TileState State { get; private set; }

        public event Action<Tile> OnTileClicked;

        private SfxManager sfxManager;
        private MotionHandle scaleMotion;
        private Vector3 originalScale = Vector3.one;

        // Bỏ Attribute [Inject] ở đây. Nhận SfxManager thông qua hàm Init
        public void Init(int iconId, Sprite iconSprite, Vector3 gridCoord, int layerIndex, SfxManager sfxManager)
        {
            IconID = iconId;
            iconRenderer.sprite = iconSprite;
            GridCoordinate = gridCoord;
            this.sfxManager = sfxManager;

            SetSortingOrder(layerIndex * 10);
            SetState(TileState.Exposed);

            originalScale = transform.localScale;
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

                    PlayScaleAnim(originalScale, animDuration);
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
                PlayScaleAnim(originalScale * hoverScale, animDuration);
            }
        }

        private void OnMouseExit()
        {
            if (State == TileState.Exposed)
            {
                PlayScaleAnim(originalScale, animDuration);
            }
        }

        private void OnMouseDown()
        {
            if (State == TileState.Exposed)
            {
                if (tapClip != null && sfxManager != null) sfxManager.Play(tapClip, 1f);
                PlayScaleAnim(originalScale * tapScale, animDuration * 0.5f);
                OnTileClicked?.Invoke(this);
            }
        }

        private void PlayScaleAnim(Vector3 targetScale, float duration)
        {
            if (scaleMotion.IsActive()) scaleMotion.Cancel();

            scaleMotion = LMotion.Create(transform.localScale, targetScale, duration)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(transform);
        }

        private void OnDisable()
        {
            if (scaleMotion.IsActive()) scaleMotion.Cancel();
        }
    }
}