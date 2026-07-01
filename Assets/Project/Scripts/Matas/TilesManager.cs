using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Matas
{
    public class TilesManager : MonoBehaviour
    {
        [Header("UI")] [SerializeField] private GameObject _mapOverlay;

        [Header("Selection")] [SerializeField] private RectTransform _selectionOverlay;

        [Header("World")] [SerializeField] private GameObject _tilesParent;

        [Header("UI Grid")] [SerializeField] private float _tileSizeUI = 64f;

        [SerializeField] private float _horizontalGapUI = 8f;
        [SerializeField] private float _verticalGapUI = 8f;

        [Header("World Grid")] [SerializeField]
        private float _tileSizeWorld = 1f;

        [SerializeField] private float _horizontalGapWorld = 0.1f;
        [SerializeField] private float _verticalGapWorld = 0.1f;

        [Header("Animation")] [SerializeField] private float _swapAnimationDuration = 0.2f;

        [Header("Lock Overlay")] [SerializeField]
        private GameObject _lockOverlayPrefab;

        [SerializeField] private bool[] _lockedTiles;

        [Header("Inventory UI")] [SerializeField]
        private GameObject _inventoryOverlay;

        [Header("Character Controller")] [SerializeField]
        private CharacterController2D _characterController;

        [Header("UI Background")] [SerializeField]
        private GameObject _uiBackground;

        [Header("UI States")] [SerializeField] private TileState[] _tileStates;

        [SerializeField] private bool _debugShowInvalidTiles;
        [SerializeField] private bool _debugConnections;

        [SerializeField] private Color _normalTileColor = Color.white;
        [SerializeField] private Color _invalidNeighbourColor = new(1f, 0.45f, 0.45f);

        //[Header("Rotation")] [SerializeField] private float _rotationAngle = 90f;

        private int _dragIndex = -1;
        private int _gridSize;
        private GameObject[] _lockOverlays;
        private int _selectedIndex = -1;
        private GameObject[] _tilePrefabs;
        private GameObject[] _tilePrefabsUI;


        private void Start()
        {
            GetUITiles();
            GetWorldTiles();

            _gridSize = Mathf.CeilToInt(Mathf.Sqrt(_tilePrefabsUI.Length));
            _lockOverlays = new GameObject[_tilePrefabsUI.Length];
            //_tileStates = new TileState[_tilePrefabsUI.Length];

            SetupUITiles();
            SetupWorldTiles();

            InitializeLockedTiles();

            UpdateBoardHighlights();

            if (_selectionOverlay != null)
            {
                _selectionOverlay.gameObject.SetActive(false);
                _selectionOverlay.SetAsLastSibling();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                // Opening is always allowed
                if (!_mapOverlay.activeSelf)
                {
                    _mapOverlay.SetActive(true);
                    _inventoryOverlay.SetActive(true);
                    _uiBackground.SetActive(true);
                    _characterController.CanMove = false;

                    UpdateBoardHighlights();
                    return;
                }

                // Closing only if puzzle is valid
                if (!UpdateBoardHighlights())
                {
                    ShakeInvalidTiles();
                    return;
                }

                _mapOverlay.SetActive(false);
                _inventoryOverlay.SetActive(false);
                _uiBackground.SetActive(false);

                _characterController.CanMove = true;
                _selectionOverlay.gameObject.SetActive(false);
            }

            if (_mapOverlay.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Q)) RotateSelectedTile(false);

                if (Input.GetKeyDown(KeyCode.E)) RotateSelectedTile(true);
            }

            if (_debugShowInvalidTiles)
                for (var i = 0; i < _tileStates.Length; i++)
                {
                    var valid = IsTileValid(i);

                    var img = _tilePrefabsUI[i].GetComponent<Image>();
                    if (img != null)
                        img.color = valid ? Color.white : Color.red;
                }

            if (_debugConnections) RefreshAllDebugVisuals();
        }

        private void OnValidate()
        {
            var count = _mapOverlay == null
                ? 0
                : _mapOverlay.transform.childCount;

            if (_lockedTiles == null || _lockedTiles.Length != count)
                Array.Resize(ref _lockedTiles, count);

            if (_tileStates == null || _tileStates.Length != count)
                Array.Resize(ref _tileStates, count);
        }

        private bool IsTileEmpty(int index)
        {
            if (index < 0 || index >= _tilePrefabsUI.Length)
                return true;

            var img = _tilePrefabsUI[index].GetComponent<Image>();
            return img == null || img.sprite == null;
        }

        private IEnumerator ShakeTile(int index)
        {
            var rect = _tilePrefabsUI[index].GetComponent<RectTransform>();

            if (rect == null)
                yield break;

            var original = rect.anchoredPosition;

            const float duration = 0.25f;
            const float strength = 6f;
            const float frequency = 35f;

            var time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;

                var x = Mathf.Sin(time * frequency * Mathf.PI) * strength;

                rect.anchoredPosition = original + Vector2.right * x;

                yield return null;
            }

            rect.anchoredPosition = original;
        }

        private void ShakeInvalidTiles()
        {
            if (_tileStates == null)
                return;

            var invalid = new bool[_tileStates.Length];

            for (var i = 0; i < _tileStates.Length; i++)
                foreach (TileSide side in Enum.GetValues(typeof(TileSide)))
                {
                    var neighbour = GetNeighborIndex(i, side);

                    if (neighbour < 0)
                        continue;

                    if (!CanConnect(i, side, neighbour))
                    {
                        invalid[i] = true;
                        invalid[neighbour] = true;
                    }
                }

            for (var i = 0; i < invalid.Length; i++)
                if (invalid[i])
                    StartCoroutine(ShakeTile(i));
        }

        private bool HasBoardMismatch()
        {
            if (_tileStates == null)
                return false;

            for (var i = 0; i < _tileStates.Length; i++)
                foreach (TileSide side in Enum.GetValues(typeof(TileSide)))
                {
                    var neighbour = GetNeighborIndex(i, side);

                    if (neighbour < 0)
                        continue;

                    if (!CanConnect(i, side, neighbour))
                        return true;
                }

            return false;
        }

        private void ResetTileHighlights()
        {
            foreach (var tile in _tilePrefabsUI)
            {
                var image = tile.GetComponent<Image>();

                if (image != null)
                    image.color = _normalTileColor;
            }
        }

        private bool UpdateBoardHighlights()
        {
            ResetTileHighlights();

            if (_tileStates == null)
                return true;

            var invalid = new bool[_tileStates.Length];
            var hasMismatch = false;

            for (var i = 0; i < _tileStates.Length; i++)
                foreach (TileSide side in Enum.GetValues(typeof(TileSide)))
                {
                    var neighbour = GetNeighborIndex(i, side);

                    if (neighbour < 0)
                        continue;

                    if (!CanConnect(i, side, neighbour))
                    {
                        hasMismatch = true;
                        invalid[i] = true;
                        invalid[neighbour] = true;
                    }
                }

            for (var i = 0; i < invalid.Length; i++)
            {
                if (!invalid[i])
                    continue;

                var image = _tilePrefabsUI[i].GetComponent<Image>();

                if (image != null)
                    image.color = _invalidNeighbourColor;
            }

            return !hasMismatch;
        }

        private void UpdateTileDebugVisual(int index)
        {
            if (_tilePrefabsUI == null || index < 0 || index >= _tilePrefabsUI.Length)
                return;

            var img = _tilePrefabsUI[index].GetComponent<Image>();
            if (img == null)
                return;

            var valid = IsTileValid(index);

            if (valid)
            {
                img.color = Color.white;
                return;
            }

            var hasMismatch = false;

            for (var i = 0; i < 4; i++)
            {
                var side = (TileSide)i;
                var neighbor = GetNeighborIndex(index, side);

                if (neighbor == -1)
                    continue;

                if (!CanConnect(index, side, neighbor))
                {
                    hasMismatch = true;
                    break;
                }
            }

            img.color = hasMismatch ? new Color(1f, 0.4f, 0.4f) : new Color(1f, 1f, 0.6f);
        }

        private void RefreshAllDebugVisuals()
        {
            if (_tilePrefabsUI == null)
                return;

            for (var i = 0; i < _tilePrefabsUI.Length; i++)
                UpdateTileDebugVisual(i);
        }

        private bool CanConnect(int aIndex, TileSide aSide, int bIndex)
        {
            if (bIndex < 0 || bIndex >= _tileStates.Length)
                return false;

            // Empty tiles are ignored completely.
            if (IsTileEmpty(aIndex) || IsTileEmpty(bIndex))
                return true;

            var bSide = TileState.Opposite(aSide);

            return _tileStates[aIndex]
                .CanConnect(aSide, _tileStates[bIndex], bSide);
        }

        private int GetNeighborIndex(int index, TileSide side)
        {
            var x = index % _gridSize;
            var y = index / _gridSize;

            switch (side)
            {
                case TileSide.Top: y--; break;
                case TileSide.Bottom: y++; break;
                case TileSide.Left: x--; break;
                case TileSide.Right: x++; break;
            }

            if (x < 0 || x >= _gridSize || y < 0 || y >= _gridSize)
                return -1;

            return y * _gridSize + x;
        }

        private bool IsTileValid(int index)
        {
            if (IsTileEmpty(index))
                return true;

            for (var i = 0; i < 4; i++)
            {
                var side = (TileSide)i;
                var neighbor = GetNeighborIndex(index, side);

                if (neighbor == -1)
                    continue;

                if (!CanConnect(index, side, neighbor))
                    return false;
            }

            return true;
        }

        private void InitializeLockedTiles()
        {
            for (var i = 0;
                 i < _lockedTiles.Length &&
                 i < _tileStates.Length;
                 i++)
                SetTileLocked(i, _lockedTiles[i]);
        }

        private void RotateSelectedTile(bool clockwise)
        {
            if (_selectedIndex < 0) return;

            if (clockwise)
                _tileStates[_selectedIndex].Rotation =
                    (_tileStates[_selectedIndex].Rotation + 90f) % 360f;
            else
                _tileStates[_selectedIndex].Rotation =
                    (_tileStates[_selectedIndex].Rotation + 270f) % 360f;

            StartCoroutine(RotateUITile(_selectedIndex));
            StartCoroutine(RotateWorldTile(_selectedIndex));
            StartCoroutine(AnimateSelectionOverlay());
        }

        private IEnumerator RotateUITile(int index)
        {
            var rect =
                _tilePrefabsUI[index].GetComponent<RectTransform>();

            var startRot = rect.localRotation;

            var targetRot =
                Quaternion.Euler(
                    0f,
                    0f,
                    -_tileStates[index].Rotation);

            var time = 0f;

            while (time < _swapAnimationDuration)
            {
                time += Time.deltaTime;

                var t = Mathf.SmoothStep(0f, 1f, time / _swapAnimationDuration);

                rect.localRotation =
                    Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }

            rect.localRotation = targetRot;

            UpdateTileVisual(index);
            UpdateTileDebugVisual(index);
            UpdateBoardHighlights();
        }

        private IEnumerator RotateWorldTile(int index)
        {
            var tile =
                _tilePrefabs[index].transform;

            var startRot = tile.rotation;

            var targetRot =
                Quaternion.Euler(0f, _tileStates[index].Rotation, 0f);

            var time = 0f;

            while (time < _swapAnimationDuration)
            {
                time += Time.deltaTime;

                var t = Mathf.SmoothStep(0f, 1f, time / _swapAnimationDuration);

                tile.rotation =
                    Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }

            tile.rotation = targetRot;

            UpdateTileVisual(index);
            UpdateTileDebugVisual(index);
        }

        private void UpdateTileVisual(int index)
        {
            if (_tilePrefabsUI == null || index < 0 || index >= _tilePrefabsUI.Length)
                return;

            var vis = _tilePrefabsUI[index]
                .GetComponent<TileConnectionVisualizer>();

            if (vis != null)
                vis.Set(_tileStates[index]);
        }

        private void GetWorldTiles()
        {
            var count = _tilesParent.transform.childCount;

            _tilePrefabs = new GameObject[count];

            for (var i = 0; i < count; i++)
                _tilePrefabs[i] =
                    _tilesParent.transform.GetChild(i).gameObject;
        }

        private void SetupUITiles()
        {
            for (var i = 0; i < _tilePrefabsUI.Length; i++)
            {
                var tile = _tilePrefabsUI[i];

                var rect =
                    tile.GetComponent<RectTransform>();

                rect.localScale = Vector3.one;
                rect.anchorMin =
                    rect.anchorMax =
                        rect.pivot =
                            new Vector2(0.5f, 0.5f);

                rect.sizeDelta =
                    Vector2.one * _tileSizeUI;


                var image = tile.GetComponent<Image>();

                if (image != null)
                {
                    image.raycastTarget = true;

                    var color = image.color;
                    color.a = image.sprite == null ? 0f : 1f;

                    image.color = color;
                }

                AddDragHandler(tile, i);

                MoveUITile(i);

                if (_lockOverlayPrefab != null)
                {
                    var overlay =
                        Instantiate(_lockOverlayPrefab, tile.transform);

                    var rt =
                        overlay.GetComponent<RectTransform>();

                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    overlay.SetActive(false);

                    _lockOverlays[i] = overlay;
                }

                if (_tileStates != null &&
                    i < _tileStates.Length &&
                    _tileStates[i].IsLocked)
                    if (_lockOverlays != null &&
                        _lockOverlays[i] != null)
                        _lockOverlays[i].SetActive(true);

                UpdateTileVisual(i);
            }
        }

        private void SetTileLocked(int index, bool locked)
        {
            if (index < 0 || index >= _tileStates.Length)
                return;

            _tileStates[index].IsLocked = locked;

            if (_lockOverlays != null &&
                _lockOverlays[index] != null)
                _lockOverlays[index].SetActive(locked);
        }

        private void SetupWorldTiles()
        {
            for (var i = 0; i < _tilePrefabs.Length; i++) MoveWorldTile(i);
        }

        public void SelectTile(int index)
        {
            if (!IsTileSelectable(index)) return;

            _selectedIndex = index;

            if (_selectionOverlay == null) return;

            _selectionOverlay.gameObject.SetActive(true);
            _selectionOverlay.anchoredPosition =
                GetUITilePosition(index);

            _selectionOverlay.localRotation =
                Quaternion.Euler(0f, 0f, -_tileStates[index].Rotation);

            _selectionOverlay.SetAsLastSibling();

            UpdateTileDebugVisual(index);
            UpdateBoardHighlights();
        }

        private void GetUITiles()
        {
            var count = _mapOverlay.transform.childCount;

            _tilePrefabsUI = new GameObject[count];

            for (var i = 0; i < count; i++)
                _tilePrefabsUI[i] =
                    _mapOverlay.transform.GetChild(i).gameObject;
        }

        private Vector2 GetUITilePosition(int index)
        {
            var x = index % _gridSize;
            var y = index / _gridSize;

            var totalWidth =
                _gridSize * _tileSizeUI +
                (_gridSize - 1) * _horizontalGapUI;

            var totalHeight =
                _gridSize * _tileSizeUI +
                (_gridSize - 1) * _verticalGapUI;

            var startX =
                -totalWidth * 0.5f +
                _tileSizeUI * 0.5f;

            var startY =
                totalHeight * 0.5f -
                _tileSizeUI * 0.5f;

            return new Vector2(
                startX + x * (_tileSizeUI + _horizontalGapUI),
                startY - y * (_tileSizeUI + _verticalGapUI)
            );
        }

        private Vector3 GetWorldTilePosition(int index)
        {
            var x = index % _gridSize;
            var y = index / _gridSize;

            var totalWidth =
                _gridSize * _tileSizeWorld +
                (_gridSize - 1) * _horizontalGapWorld;

            var totalHeight =
                _gridSize * _tileSizeWorld +
                (_gridSize - 1) * _verticalGapWorld;

            var startX =
                -totalWidth * 0.5f +
                _tileSizeWorld * 0.5f;

            var startZ =
                totalHeight * 0.5f -
                _tileSizeWorld * 0.5f;

            var tile =
                _tilePrefabs[index].transform;

            return new Vector3(
                startX + x * (_tileSizeWorld + _horizontalGapWorld),
                tile.position.y,
                startZ - y * (_tileSizeWorld + _verticalGapWorld)
            );
        }

        private void MoveUITile(int index)
        {
            var rect =
                _tilePrefabsUI[index].GetComponent<RectTransform>();

            rect.anchoredPosition = GetUITilePosition(index);

            rect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    -_tileStates[index].Rotation);
        }

        private IEnumerator AnimateUITile(int index)
        {
            var rect =
                _tilePrefabsUI[index]
                    .GetComponent<RectTransform>();

            var start =
                rect.anchoredPosition;

            var target =
                GetUITilePosition(index);

            var time = 0f;

            while (time < _swapAnimationDuration)
            {
                time += Time.deltaTime;

                var t = Mathf.SmoothStep(
                    0f,
                    1f,
                    time / _swapAnimationDuration);

                rect.anchoredPosition =
                    Vector2.Lerp(start, target, t);

                yield return null;
            }

            rect.anchoredPosition = target;
        }

        private IEnumerator AnimateSelectionOverlay()
        {
            if (_selectionOverlay == null ||
                _selectedIndex < 0)
                yield break;

            var startPos =
                _selectionOverlay.anchoredPosition;

            var targetPos =
                GetUITilePosition(_selectedIndex);

            var startRot =
                _selectionOverlay.localRotation;

            var targetRot =
                Quaternion.Euler(
                    0f,
                    0f,
                    -_tileStates[_selectedIndex].Rotation);

            var time = 0f;

            while (time < _swapAnimationDuration)
            {
                time += Time.deltaTime;

                var t = Mathf.SmoothStep(
                    0f,
                    1f,
                    time / _swapAnimationDuration);

                _selectionOverlay.anchoredPosition =
                    Vector2.Lerp(startPos, targetPos, t);

                _selectionOverlay.localRotation =
                    Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }

            _selectionOverlay.anchoredPosition = targetPos;
            _selectionOverlay.localRotation = targetRot;
            _selectionOverlay.SetAsLastSibling();
        }

        private void MoveWorldTile(int index)
        {
            var tile =
                _tilePrefabs[index].transform;

            tile.position = GetWorldTilePosition(index);

            tile.rotation =
                Quaternion.Euler(0f, _tileStates[index].Rotation, 0f);
        }

        private IEnumerator AnimateWorldTile(int index)
        {
            var tile =
                _tilePrefabs[index].transform;

            var start =
                tile.position;

            var target =
                GetWorldTilePosition(index);

            var time = 0f;

            while (time < _swapAnimationDuration)
            {
                time += Time.deltaTime;

                var t = Mathf.SmoothStep(
                    0f,
                    1f,
                    time / _swapAnimationDuration);

                tile.position =
                    Vector3.Lerp(start, target, t);

                yield return null;
            }

            tile.position = target;
        }

        private void AddDragHandler(GameObject tile, int index)
        {
            var handler =
                tile.GetComponent<TileDragHandler>();

            if (handler == null)
                handler =
                    tile.AddComponent<TileDragHandler>();

            handler.Initialize(this, index);
        }

        public void StartDrag(int index)
        {
            _dragIndex = index;

            if (IsTileSelectable(index)) SelectTile(index);
        }

        public void DropTile(int targetIndex)
        {
            if (_tileStates[_dragIndex].IsLocked ||
                _tileStates[targetIndex].IsLocked)
            {
                _dragIndex = -1;
                return;
            }

            if (IsTileEmpty(_dragIndex) && IsTileEmpty(targetIndex))
            {
                _dragIndex = -1;
                return;
            }

            (_tilePrefabsUI[_dragIndex],
                    _tilePrefabsUI[targetIndex]) =
                (_tilePrefabsUI[targetIndex],
                    _tilePrefabsUI[_dragIndex]);

            (_tilePrefabs[_dragIndex],
                    _tilePrefabs[targetIndex]) =
                (_tilePrefabs[targetIndex],
                    _tilePrefabs[_dragIndex]);

            (_tileStates[_dragIndex],
                    _tileStates[targetIndex]) =
                (_tileStates[targetIndex],
                    _tileStates[_dragIndex]);

            RefreshHierarchies();

            StartCoroutine(AnimateUITile(_dragIndex));
            StartCoroutine(AnimateUITile(targetIndex));

            StartCoroutine(AnimateWorldTile(_dragIndex));
            StartCoroutine(AnimateWorldTile(targetIndex));

            _selectedIndex = targetIndex;
            StartCoroutine(AnimateSelectionOverlay());

            RefreshTileIndices();

            UpdateTileVisual(_dragIndex);
            UpdateTileVisual(targetIndex);

            RefreshAllDebugVisuals();
            UpdateBoardHighlights();

            _dragIndex = -1;
        }

        private void RefreshHierarchies()
        {
            for (var i = 0; i < _tilePrefabsUI.Length; i++)
                _tilePrefabsUI[i]
                    .transform
                    .SetSiblingIndex(i);

            if (_selectionOverlay != null) _selectionOverlay.SetAsLastSibling();

            for (var i = 0; i < _tilePrefabs.Length; i++)
                _tilePrefabs[i]
                    .transform
                    .SetSiblingIndex(i);
        }

        private void RefreshTileIndices()
        {
            for (var i = 0; i < _tilePrefabsUI.Length; i++)
            {
                var handler =
                    _tilePrefabsUI[i]
                        .GetComponent<TileDragHandler>();

                if (handler != null) handler.UpdateIndex(i);
            }
        }

        private bool IsTileSelectable(int index)
        {
            if (index < 0 || index >= _tileStates.Length)
                return false;

            if (_tileStates[index].IsLocked)
                return false;

            var img =
                _tilePrefabsUI[index].GetComponent<Image>();

            if (img == null)
                return false;

            return img.sprite != null;
        }
    }

    public enum TileConnectionType
    {
        Grass,
        Dirt,
        Path,
        River,
        Water
    }

    public enum TileSide
    {
        Top,
        Right,
        Bottom,
        Left
    }

    [Serializable]
    public struct TileState
    {
        [Range(0f, 270f)] public float Rotation;

        public bool IsLocked;

        public TileConnectionType Top;
        public TileConnectionType Right;
        public TileConnectionType Bottom;
        public TileConnectionType Left;

        public TileConnectionType GetSide(TileSide side)
        {
            return side switch
            {
                TileSide.Top => Top,
                TileSide.Right => Right,
                TileSide.Bottom => Bottom,
                TileSide.Left => Left,
                _ => Top
            };
        }

        public void SetSide(TileSide side, TileConnectionType type)
        {
            switch (side)
            {
                case TileSide.Top:
                    Top = type;
                    break;

                case TileSide.Right:
                    Right = type;
                    break;

                case TileSide.Bottom:
                    Bottom = type;
                    break;

                case TileSide.Left:
                    Left = type;
                    break;
            }
        }

        /// <summary>
        ///     Returns the connection type currently facing the specified world side,
        ///     taking the tile's rotation into account.
        /// </summary>
        public TileConnectionType GetRotatedSide(TileSide worldSide)
        {
            var rotationSteps = Mathf.RoundToInt(Rotation / 90f) % 4;

            var localSide = worldSide;

            for (var i = 0; i < rotationSteps; i++) localSide = RotateSideCounterClockwise(localSide);

            return GetSide(localSide);
        }

        private static TileSide RotateSideCounterClockwise(TileSide side)
        {
            return side switch
            {
                TileSide.Top => TileSide.Right,
                TileSide.Right => TileSide.Bottom,
                TileSide.Bottom => TileSide.Left,
                TileSide.Left => TileSide.Top,
                _ => side
            };
        }

        public static TileSide Opposite(TileSide side)
        {
            return side switch
            {
                TileSide.Top => TileSide.Bottom,
                TileSide.Right => TileSide.Left,
                TileSide.Bottom => TileSide.Top,
                TileSide.Left => TileSide.Right,
                _ => side
            };
        }

        public bool CanConnect(TileSide mySide, TileState other, TileSide otherSide)
        {
            return GetRotatedSide(mySide) == other.GetRotatedSide(otherSide);
        }
    }
}