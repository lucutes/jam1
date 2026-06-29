using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Matas
{
    public class TilesManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject _mapOverlay;

        [Header("World")]
        [SerializeField] private GameObject _tilesParent;

        [Header("UI Grid")]
        [SerializeField] private float _tileSizeUI = 64f;
        [SerializeField] private float _horizontalGapUI = 8f;
        [SerializeField] private float _verticalGapUI = 8f;

        [Header("World Grid")]
        [SerializeField] private float _tileSizeWorld = 1f;
        [SerializeField] private float _horizontalGapWorld = 0.1f;
        [SerializeField] private float _verticalGapWorld = 0.1f;

        [Header("Animation")]
        [SerializeField] private float _swapAnimationDuration = 0.2f;

        private GameObject[] _tilePrefabsUI;
        private GameObject[] _tilePrefabs;

        private int _gridSize;
        private int _dragIndex = -1;


        private void Start()
        {
            GetUITiles();
            GetWorldTiles();

            _gridSize = Mathf.CeilToInt(Mathf.Sqrt(_tilePrefabsUI.Length));

            SetupUITiles();
            SetupWorldTiles();
        }


        private void GetUITiles()
        {
            int count = _mapOverlay.transform.childCount;

            _tilePrefabsUI = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                _tilePrefabsUI[i] =
                    _mapOverlay.transform.GetChild(i).gameObject;
            }
        }


        private void GetWorldTiles()
        {
            int count = _tilesParent.transform.childCount;

            _tilePrefabs = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                _tilePrefabs[i] =
                    _tilesParent.transform.GetChild(i).gameObject;
            }
        }


        private void SetupUITiles()
        {
            for (int i = 0; i < _tilePrefabsUI.Length; i++)
            {
                GameObject tile = _tilePrefabsUI[i];

                RectTransform rect =
                    tile.GetComponent<RectTransform>();

                rect.localScale = Vector3.one;
                rect.anchorMin =
                    rect.anchorMax =
                    rect.pivot =
                    new Vector2(0.5f, 0.5f);

                rect.sizeDelta =
                    Vector2.one * _tileSizeUI;


                Image image = tile.GetComponent<Image>();

                if (image != null)
                {
                    image.raycastTarget = true;

                    Color color = image.color;
                    color.a = image.sprite == null ? 0f : 1f;
                    image.color = color;
                }

                AddDragHandler(tile, i);

                MoveUITile(i);
            }
        }


        private void SetupWorldTiles()
        {
            for (int i = 0; i < _tilePrefabs.Length; i++)
            {
                MoveWorldTile(i);
            }
        }


        private Vector2 GetUITilePosition(int index)
        {
            int x = index % _gridSize;
            int y = index / _gridSize;

            float totalWidth =
                _gridSize * _tileSizeUI +
                (_gridSize - 1) * _horizontalGapUI;

            float totalHeight =
                _gridSize * _tileSizeUI +
                (_gridSize - 1) * _verticalGapUI;

            float startX =
                -totalWidth * 0.5f +
                _tileSizeUI * 0.5f;

            float startY =
                totalHeight * 0.5f -
                _tileSizeUI * 0.5f;

            return new Vector2(
                startX + x * (_tileSizeUI + _horizontalGapUI),
                startY - y * (_tileSizeUI + _verticalGapUI)
            );
        }


        private Vector3 GetWorldTilePosition(int index)
        {
            int x = index % _gridSize;
            int y = index / _gridSize;

            float totalWidth =
                _gridSize * _tileSizeWorld +
                (_gridSize - 1) * _horizontalGapWorld;

            float totalHeight =
                _gridSize * _tileSizeWorld +
                (_gridSize - 1) * _verticalGapWorld;

            float startX =
                -totalWidth * 0.5f +
                _tileSizeWorld * 0.5f;

            float startZ =
                totalHeight * 0.5f -
                _tileSizeWorld * 0.5f;

            Transform tile =
                _tilePrefabs[index].transform;

            return new Vector3(
                startX + x * (_tileSizeWorld + _horizontalGapWorld),
                tile.position.y,
                startZ - y * (_tileSizeWorld + _verticalGapWorld)
            );
        }


        private void MoveUITile(int index)
        {
            RectTransform rect =
                _tilePrefabsUI[index]
                    .GetComponent<RectTransform>();

            rect.anchoredPosition =
                GetUITilePosition(index);
        }


        private IEnumerator AnimateUITile(int index)
        {
            RectTransform rect =
                _tilePrefabsUI[index]
                    .GetComponent<RectTransform>();

            Vector2 start =
                rect.anchoredPosition;

            Vector2 target =
                GetUITilePosition(index);

            float time = 0f;

            while (time < _swapAnimationDuration)
            {
                time += Time.deltaTime;

                float t = Mathf.SmoothStep(
                    0f,
                    1f,
                    time / _swapAnimationDuration);

                rect.anchoredPosition =
                    Vector2.Lerp(start, target, t);

                yield return null;
            }

            rect.anchoredPosition = target;
        }


        private void MoveWorldTile(int index)
        {
            _tilePrefabs[index].transform.position =
                GetWorldTilePosition(index);
        }


        private IEnumerator AnimateWorldTile(int index)
        {
            Transform tile =
                _tilePrefabs[index].transform;

            Vector3 start =
                tile.position;

            Vector3 target =
                GetWorldTilePosition(index);

            float time = 0f;

            while (time < _swapAnimationDuration)
            {
                time += Time.deltaTime;

                float t = Mathf.SmoothStep(
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
            TileDragHandler handler =
                tile.GetComponent<TileDragHandler>();

            if (handler == null)
            {
                handler =
                    tile.AddComponent<TileDragHandler>();
            }

            handler.Initialize(this, index);
        }


        public void StartDrag(int index)
        {
            _dragIndex = index;
        }


        public void DropTile(int targetIndex)
        {
            if (_dragIndex == -1 ||
                _dragIndex == targetIndex)
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


            RefreshHierarchies();


            StartCoroutine(AnimateUITile(_dragIndex));
            StartCoroutine(AnimateUITile(targetIndex));

            StartCoroutine(AnimateWorldTile(_dragIndex));
            StartCoroutine(AnimateWorldTile(targetIndex));


            RefreshTileIndices();

            _dragIndex = -1;
        }


        private void RefreshHierarchies()
        {
            for (int i = 0; i < _tilePrefabsUI.Length; i++)
            {
                _tilePrefabsUI[i]
                    .transform
                    .SetSiblingIndex(i);
            }

            for (int i = 0; i < _tilePrefabs.Length; i++)
            {
                _tilePrefabs[i]
                    .transform
                    .SetSiblingIndex(i);
            }
        }


        private void RefreshTileIndices()
        {
            for (int i = 0; i < _tilePrefabsUI.Length; i++)
            {
                TileDragHandler handler =
                    _tilePrefabsUI[i]
                        .GetComponent<TileDragHandler>();

                if (handler != null)
                {
                    handler.UpdateIndex(i);
                }
            }
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _mapOverlay.SetActive(
                    !_mapOverlay.activeSelf);
            }
        }
    }
}
