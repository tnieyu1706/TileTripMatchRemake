using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using Reflex.Attributes;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;

namespace Game.Core.Gameplay
{
    /// <summary>
    /// Presentation Handler (View)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class RackView : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private float moveSpeed = 25f;
        [SerializeField] private StyleSheet rackStyleSheet;

        private UIDocument _uiDocument;
        private VisualElement _rackContainer;
        private Vector3[] _slotWorldPositions;

        private RackController _rackController;

        private Dictionary<Tile, MotionHandle> _activeMoveRoutines = new Dictionary<Tile, MotionHandle>();

        [Inject]
        private void Construct(RackController rackController)
        {
            _rackController = rackController;
        }

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_rackController != null)
            {
                _rackController.OnRackInitialized += HandleRackInitialized;
                _rackController.OnRackUpdated += HandleRackUpdated;
                _rackController.OnTilesMatched += HandleTilesMatched;
            }
        }

        private void OnDisable()
        {
            if (_rackController != null)
            {
                _rackController.OnRackInitialized -= HandleRackInitialized;
                _rackController.OnRackUpdated -= HandleRackUpdated;
                _rackController.OnTilesMatched -= HandleTilesMatched;
            }

            // Hủy toàn bộ Motion đang chạy khi object bị tắt (hoặc chuyển Scene) để tránh rò rỉ bộ nhớ & lỗi Reference
            foreach (var handle in _activeMoveRoutines.Values)
            {
                if (handle.IsActive()) handle.Cancel();
            }

            _activeMoveRoutines.Clear();
        }

        private void HandleRackInitialized(int maxSlots)
        {
            _slotWorldPositions = new Vector3[maxSlots];
            GenerateUIToolkit(maxSlots);
        }

        private void GenerateUIToolkit(int maxSlots)
        {
            var root = _uiDocument.rootVisualElement;
            root.Clear();

            if (rackStyleSheet != null)
            {
                root.styleSheets.Add(rackStyleSheet);
            }

            var wrapper = new VisualElement();
            wrapper.AddToClassList("rack-container");
            root.Add(wrapper);

            _rackContainer = new VisualElement();
            _rackContainer.AddToClassList("rack-bg");
            wrapper.Add(_rackContainer);

            for (int i = 0; i < maxSlots; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("rack-slot");

                int slotIndex = i;
                slot.RegisterCallback<GeometryChangedEvent>(evt => UpdateSlotWorldPosition(evt, slotIndex, slot));

                _rackContainer.Add(slot);
            }
        }

        private void UpdateSlotWorldPosition(GeometryChangedEvent evt, int index, VisualElement slot)
        {
            var panel = slot.panel;
            if (panel == null) return;

            // Khai báo lại panelPos từ tọa độ bounds của UI slot
            Vector2 panelPos = slot.worldBound.center;

            float normalizedX = panelPos.x / panel.visualTree.layout.width;
            float normalizedY = panelPos.y / panel.visualTree.layout.height;

            Vector2 screenPos = new Vector2(
                normalizedX * Screen.width,
                (1f - normalizedY) * Screen.height
            );

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 worldPos = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y,
                    Mathf.Abs(mainCam.transform.position.z)));
                worldPos.z = 0f;
                _slotWorldPositions[index] = worldPos;
            }
        }

        private void HandleRackUpdated(IReadOnlyList<Tile> currentTiles)
        {
            for (int i = 0; i < currentTiles.Count; i++)
            {
                Tile tile = currentTiles[i];
                tile.SetSortingOrder(999);

                if (i < _slotWorldPositions.Length)
                {
                    Vector3 targetPos = _slotWorldPositions[i];

                    if (_activeMoveRoutines.TryGetValue(tile, out MotionHandle activeHandle) && activeHandle.IsActive())
                    {
                        activeHandle.Cancel();
                    }

                    float distance = Vector3.Distance(tile.transform.position, targetPos);
                    float duration = Mathf.Clamp(distance / moveSpeed, 0.1f, 0.4f);

                    MotionHandle newHandle = LMotion.Create(tile.transform.position, targetPos, duration)
                        .WithEase(Ease.OutQuad)
                        .BindToPosition(tile.transform);

                    _activeMoveRoutines[tile] = newHandle;
                }
            }
        }

        private void HandleTilesMatched(Tile t1, Tile t2, Tile t3, int iconId)
        {
            ProcessMatchTask(t1, t2, t3, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid ProcessMatchTask(Tile t1, Tile t2, Tile t3, CancellationToken cancellationToken)
        {
            t1.SetState(TileState.Matched);
            t2.SetState(TileState.Matched);
            t3.SetState(TileState.Matched);

            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: cancellationToken)
                .SuppressCancellationThrow();

            if (isCanceled) return;

            // Xoá bỏ an toàn: Hủy bỏ Motion đang chạy (nếu có) trước khi Destroy Object
            CancelMotionForTile(t1);
            CancelMotionForTile(t2);
            CancelMotionForTile(t3);

            if (t1 != null) Destroy(t1.gameObject);
            if (t2 != null) Destroy(t2.gameObject);
            if (t3 != null) Destroy(t3.gameObject);
        }

        private void CancelMotionForTile(Tile tile)
        {
            if (tile != null && _activeMoveRoutines.TryGetValue(tile, out MotionHandle handle))
            {
                if (handle.IsActive()) handle.Cancel();
                _activeMoveRoutines.Remove(tile);
            }
        }
    }
}