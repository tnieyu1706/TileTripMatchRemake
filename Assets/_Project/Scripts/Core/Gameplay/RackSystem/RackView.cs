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
        [Header("Settings")] [SerializeField] private float moveSpeed = 35f;
        [SerializeField] private StyleSheet rackStyleSheet;
        [SerializeField] private Vector3 targetTileScaleInRack = new Vector3(1f, 1f, 1f);

        [Header("Audio")] [SerializeField] private AudioClip matchClip;

        private UIDocument _uiDocument;
        private VisualElement _rackContainer;
        private Vector2[] _slotScreenPositions;

        private RackController _rackController;
        private SfxManager _sfxManager;

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
        private List<Tile> _visualSlots = new List<Tile>();

        [Inject]
        private void Construct(RackController rackController, SfxManager sfxManager)
        {
            _rackController = rackController;
            _sfxManager = sfxManager;
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
            _visualSlots.Clear();
        }

        private void HandleRackInitialized(int maxSlots)
        {
            _visualSlots.Clear();
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
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            foreach (var tile in currentTiles)
            {
                if (!_visualSlots.Contains(tile))
                {
                    tile.SetSortingOrder(999);
                    tile.transform.SetParent(null);

                    int insertIndex = GetVisualInsertIndex(tile.IconID);
                    _visualSlots.Insert(insertIndex, tile);
                }
            }

            UpdateAllVisualTilePositions(mainCam);
        }

        private int GetVisualInsertIndex(int iconId)
        {
            int lastIndex = -1;
            for (int i = 0; i < _visualSlots.Count; i++)
            {
                if (_visualSlots[i].IconID == iconId) lastIndex = i;
            }

            return lastIndex != -1 ? lastIndex + 1 : _visualSlots.Count;
        }

        private void UpdateAllVisualTilePositions(Camera mainCam)
        {
            for (int i = 0; i < _visualSlots.Count; i++)
            {
                Tile tile = _visualSlots[i];

                if (tile.State == TileState.Matched) continue;

                Vector3 targetPos = GetWorldPositionForSlot(i, mainCam);

                if (_activeMoveRoutines.TryGetValue(tile, out TileMotions activeHandle))
                {
                    activeHandle.CancelAll();
                }

                float distance = Vector3.Distance(tile.transform.position, targetPos);
                float duration = Mathf.Clamp(distance / moveSpeed, 0.1f, 0.35f);

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

        private Vector3 GetWorldPositionForSlot(int index, Camera mainCam)
        {
            Vector2 screenPos;
            if (index < _slotScreenPositions.Length)
            {
                screenPos = _slotScreenPositions[index];
            }
            else
            {
                if (_slotScreenPositions.Length >= 2)
                {
                    Vector2 lastPos = _slotScreenPositions[_slotScreenPositions.Length - 1];
                    Vector2 secondLast = _slotScreenPositions[_slotScreenPositions.Length - 2];
                    Vector2 dir = lastPos - secondLast;

                    screenPos = lastPos + dir * (index - _slotScreenPositions.Length + 1);
                }
                else
                {
                    screenPos = _slotScreenPositions[0];
                }
            }

            Vector3 targetPos =
                mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y,
                    Mathf.Abs(mainCam.transform.position.z)));
            targetPos.z = 0f;
            return targetPos;
        }

        private void HandleTilesMatched(Tile t1, Tile t2, Tile t3, int iconId)
        {
            // [MỚI] Phát âm thanh khi ghép cặp 3 viên gạch!
            if (matchClip != null && _sfxManager != null) _sfxManager.Play(matchClip, 1f);

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

            CancelMotionForTile(t1);
            CancelMotionForTile(t2);
            CancelMotionForTile(t3);

            float popDuration = 0.25f;
            var t1Task = PlayPopAnimationAsync(t1, popDuration, cancellationToken);
            var t2Task = PlayPopAnimationAsync(t2, popDuration, cancellationToken);
            var t3Task = PlayPopAnimationAsync(t3, popDuration, cancellationToken);

            await UniTask.WhenAll(t1Task, t2Task, t3Task).SuppressCancellationThrow();

            if (cancellationToken.IsCancellationRequested) return;

            _visualSlots.Remove(t1);
            _visualSlots.Remove(t2);
            _visualSlots.Remove(t3);

            if (t1 != null) Destroy(t1.gameObject);
            if (t2 != null) Destroy(t2.gameObject);
            if (t3 != null) Destroy(t3.gameObject);

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                UpdateAllVisualTilePositions(mainCam);
            }
        }

        private async UniTask PlayPopAnimationAsync(Tile tile, float duration, CancellationToken cancellationToken)
        {
            if (tile == null) return;

            try
            {
                await LMotion.Create(tile.transform.localScale, targetTileScaleInRack * 1.25f, duration * 0.5f)
                    .WithEase(Ease.OutQuad)
                    .BindToLocalScale(tile.transform)
                    .ToUniTask(cancellationToken);

                if (tile == null) return;

                await LMotion.Create(tile.transform.localScale, Vector3.zero, duration * 0.5f)
                    .WithEase(Ease.InBack)
                    .BindToLocalScale(tile.transform)
                    .ToUniTask(cancellationToken);
            }
            catch (OperationCanceledException)
            {
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