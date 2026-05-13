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
    [RequireComponent(typeof(UIDocument))]
    public class RackView : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private float moveSpeed = 25f;
        [SerializeField] private StyleSheet rackStyleSheet;

        [SerializeField] private Vector3 targetTileScaleInRack = new Vector3(1f, 1f, 1f);

        private UIDocument _uiDocument;
        private VisualElement _rackContainer;
        private Vector2[] _slotScreenPositions;
        private RackController _rackController;

        private struct TileMotions
        {
            public MotionHandle PositionHandle;
            public MotionHandle ScaleHandle;

            public void CancelAll()
            {
                if (PositionHandle.IsActive()) PositionHandle.Cancel();
                if (ScaleHandle.IsActive()) ScaleHandle.Cancel();
            }
        }

        private Dictionary<Tile, TileMotions> _activeMoveRoutines = new Dictionary<Tile, TileMotions>();

        // [MỚI] Các biến dùng để trì hoãn việc dồn gạch khi đang có hiệu ứng nổ
        private bool _isProcessingMatch = false;
        private IReadOnlyList<Tile> _pendingRackUpdate = null;

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

            foreach (var handle in _activeMoveRoutines.Values)
            {
                handle.CancelAll();
            }

            _activeMoveRoutines.Clear();
            _isProcessingMatch = false;
            _pendingRackUpdate = null;
        }

        private void HandleRackInitialized(int maxSlots)
        {
            _isProcessingMatch = false;
            _pendingRackUpdate = null;
            _slotScreenPositions = new Vector2[maxSlots];
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
                slot.RegisterCallback<GeometryChangedEvent>(evt => UpdateSlotScreenPosition(evt, slotIndex, slot));

                _rackContainer.Add(slot);
            }
        }

        private void UpdateSlotScreenPosition(GeometryChangedEvent evt, int index, VisualElement slot)
        {
            var panel = slot.panel;
            if (panel == null) return;

            Vector2 panelPos = slot.worldBound.center;

            float normalizedX = panelPos.x / panel.visualTree.layout.width;
            float normalizedY = panelPos.y / panel.visualTree.layout.height;

            _slotScreenPositions[index] = new Vector2(
                normalizedX * Screen.width,
                (1f - normalizedY) * Screen.height
            );
        }

        private void HandleRackUpdated(IReadOnlyList<Tile> currentTiles)
        {
            // [MỚI] Nếu đang có gạch nổ, tạm lưu danh sách update lại để lát nữa dồn gạch sau
            if (_isProcessingMatch)
            {
                _pendingRackUpdate = new List<Tile>(currentTiles);
                return;
            }

            Camera mainCam = Camera.main;

            for (int i = 0; i < currentTiles.Count; i++)
            {
                Tile tile = currentTiles[i];
                tile.SetSortingOrder(999);

                tile.transform.SetParent(null);

                if (i < _slotScreenPositions.Length && mainCam != null)
                {
                    Vector2 screenPos = _slotScreenPositions[i];
                    Vector3 targetPos = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y,
                        Mathf.Abs(mainCam.transform.position.z)));
                    targetPos.z = 0f;

                    if (_activeMoveRoutines.TryGetValue(tile, out TileMotions activeHandle))
                    {
                        activeHandle.CancelAll();
                    }

                    float distance = Vector3.Distance(tile.transform.position, targetPos);
                    float duration = Mathf.Clamp(distance / moveSpeed, 0.1f, 0.4f);

                    MotionHandle posHandle = LMotion.Create(tile.transform.position, targetPos, duration)
                        .WithEase(Ease.OutBack)
                        .BindToPosition(tile.transform);

                    MotionHandle scaleHandle = LMotion
                        .Create(tile.transform.localScale, targetTileScaleInRack, duration)
                        .WithEase(Ease.OutQuad)
                        .BindToLocalScale(tile.transform);

                    _activeMoveRoutines[tile] = new TileMotions
                    {
                        PositionHandle = posHandle,
                        ScaleHandle = scaleHandle
                    };
                }
            }
        }

        private void HandleTilesMatched(Tile t1, Tile t2, Tile t3, int iconId)
        {
            // Bật cờ chặn dịch chuyển gạch
            _isProcessingMatch = true;
            // TODO: Bật âm thanh ghép 3 thành công ở đây (Vui nhộn, Pop)
            ProcessMatchTask(t1, t2, t3, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid ProcessMatchTask(Tile t1, Tile t2, Tile t3, CancellationToken cancellationToken)
        {
            t1.SetState(TileState.Matched);
            t2.SetState(TileState.Matched);
            t3.SetState(TileState.Matched);

            // 1. Chờ viên gạch thứ 3 bay tới nơi
            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: cancellationToken)
                .SuppressCancellationThrow();

            if (isCanceled) return;

            // 2. Hủy Motion bay vào khay trước khi chạy Motion Pop
            CancelMotionForTile(t1);
            CancelMotionForTile(t2);
            CancelMotionForTile(t3);

            // 3. Chạy hoạt ảnh Pop và DÙNG AWAIT ĐỂ CHỜ CHÚNG CHẠY XONG 100%
            float popDuration = 0.25f;
            var t1Task = PlayPopAnimationAsync(t1, popDuration, cancellationToken);
            var t2Task = PlayPopAnimationAsync(t2, popDuration, cancellationToken);
            var t3Task = PlayPopAnimationAsync(t3, popDuration, cancellationToken);

            await UniTask.WhenAll(t1Task, t2Task, t3Task).SuppressCancellationThrow();

            if (cancellationToken.IsCancellationRequested) return;

            // 4. An toàn để Destroy vì lúc này Animation chắc chắn đã dứt điểm
            if (t1 != null) Destroy(t1.gameObject);
            if (t2 != null) Destroy(t2.gameObject);
            if (t3 != null) Destroy(t3.gameObject);

            // 5. Mở khóa cờ và tiến hành dồn gạch ở phía sau lên (nếu có)
            _isProcessingMatch = false;
            if (_pendingRackUpdate != null)
            {
                HandleRackUpdated(_pendingRackUpdate);
                _pendingRackUpdate = null;
            }
        }

        private async UniTask PlayPopAnimationAsync(Tile tile, float duration, CancellationToken cancellationToken)
        {
            if (tile == null) return;

            try
            {
                // Nửa đầu: Phóng to lên 1.25 lần
                await LMotion.Create(tile.transform.localScale, targetTileScaleInRack * 1.25f, duration * 0.5f)
                    .WithEase(Ease.OutQuad)
                    .BindToLocalScale(tile.transform)
                    .ToUniTask(cancellationToken);

                if (tile == null) return;

                // Nửa sau: Giật nhanh kích thước về 0 để lặn mất
                await LMotion.Create(tile.transform.localScale, Vector3.zero, duration * 0.5f)
                    .WithEase(Ease.InBack)
                    .BindToLocalScale(tile.transform)
                    .ToUniTask(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Task bị ngắt do chuyển Scene hoặc Destroy => Bỏ qua lỗi một cách an toàn
            }
        }

        private void CancelMotionForTile(Tile tile)
        {
            if (tile != null && _activeMoveRoutines.TryGetValue(tile, out TileMotions handle))
            {
                handle.CancelAll();
                _activeMoveRoutines.Remove(tile);
            }
        }
    }
}