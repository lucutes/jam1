using UnityEngine;

namespace Project.Scripts.Matas
{
    public class PlayerSightlineVision : MonoBehaviour
    {
        [Header("Vision")]
        [SerializeField] private float _viewRadius = 7f;
        [SerializeField, Range(0f, 1f), Tooltip("Fraction of viewRadius that stays fully bright before fading to edgeColor")]
        private float _innerBrightFraction = 0.4f;
        [SerializeField] private int _rayCount = 160;
        [SerializeField] private float _rayHeight = 0.5f;
        [SerializeField] private float _meshHeightOffset = 0.03f;
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Darkness")]
        [SerializeField] private float _darknessRadius = 60f;
        [SerializeField] private Color _voidColor = new(0f, 0f, 0f, 1f);

        [Header("Visuals")]
        [SerializeField] private Material _visionMaterial;
        [SerializeField] private Color _centerColor = new(0.28f, 0.75f, 0.42f, 0.58f);
        [SerializeField] private Color _edgeColor = new(0f, 0f, 0f, 1f);
        [SerializeField] private bool _drawDebug = true;

        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Vector3[] _vertices;
        private int[] _triangles;
        private Color[] _colors;

        private int RingAStart => 1;
        private int RingBStart => _rayCount + 2;
        private int RingCStart => 2 * (_rayCount + 1) + 1;
        private int VertexCount => 3 * (_rayCount + 1) + 1;

        private void Awake()
        {
            CreateMeshObject();
            RebuildBuffers();
        }

        private void OnValidate()
        {
            _viewRadius = Mathf.Max(0.1f, _viewRadius);
            _darknessRadius = Mathf.Max(_viewRadius + 0.1f, _darknessRadius);
            _rayCount = Mathf.Clamp(_rayCount, 24, 512);
        }

        [ContextMenu("Reset Vision Colors To Defaults")]
        private void ResetVisionColors()
        {
            _centerColor = new Color(0.28f, 0.75f, 0.42f, 0.58f);
            _edgeColor = new Color(0f, 0f, 0f, 1f);
            _voidColor = new Color(0f, 0f, 0f, 1f);
        }

        private void LateUpdate()
        {
            if (_mesh == null || _vertices == null || _vertices.Length != VertexCount)
                RebuildBuffers();

            UpdateVisionMesh();
        }

        private void CreateMeshObject()
        {
            var meshObject = new GameObject("Player Sightline Vision");
            meshObject.transform.SetParent(transform, false);
            meshObject.transform.localPosition = Vector3.zero;
            meshObject.transform.localRotation = Quaternion.identity;
            meshObject.transform.localScale = Vector3.one;

            _meshFilter = meshObject.AddComponent<MeshFilter>();
            _meshRenderer = meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;

            if (_visionMaterial != null)
            {
                _meshRenderer.sharedMaterial = _visionMaterial;
            }
            else
            {
                var shader = Shader.Find("Project/PlayerSightGradient");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Unlit");

                _meshRenderer.sharedMaterial = new Material(shader);
            }

            _mesh = new Mesh { name = "Player Sightline Vision Mesh" };
            _meshFilter.sharedMesh = _mesh;
        }

        private void RebuildBuffers()
        {
            _vertices = new Vector3[VertexCount];
            _colors = new Color[VertexCount];
            _triangles = new int[_rayCount * 9];

            var t = 0;

            // Fan: center -> visible boundary (Ring A)
            for (var i = 0; i < _rayCount; i++)
            {
                _triangles[t++] = 0;
                _triangles[t++] = RingAStart + i;
                _triangles[t++] = RingAStart + i + 1;
            }

            // Skirt: Ring B (hard black, same position as Ring A) -> Ring C (black, pushed out)
            for (var i = 0; i < _rayCount; i++)
            {
                var b0 = RingBStart + i;
                var b1 = RingBStart + i + 1;
                var c0 = RingCStart + i;
                var c1 = RingCStart + i + 1;

                _triangles[t++] = b0;
                _triangles[t++] = c0;
                _triangles[t++] = c1;

                _triangles[t++] = b0;
                _triangles[t++] = c1;
                _triangles[t++] = b1;
            }

            if (_mesh == null)
                _mesh = new Mesh { name = "Player Sightline Vision Mesh" };

            _mesh.Clear();
        }

        private void UpdateVisionMesh()
        {
            var origin = transform.position + Vector3.up * _rayHeight;
            var innerRadius = _viewRadius * _innerBrightFraction;

            _vertices[0] = Vector3.up * _meshHeightOffset;
            _colors[0] = _centerColor;

            for (var i = 0; i <= _rayCount; i++)
            {
                var angle = i / (float)_rayCount * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                var distance = GetVisibleDistance(origin, direction);

                var fadeT = Mathf.InverseLerp(innerRadius, _viewRadius, distance);
                var edgePos = direction * distance + Vector3.up * _meshHeightOffset;
                var edgeCol = Color.Lerp(_centerColor, _edgeColor, fadeT);

                // Ring A: the actual visible boundary — normal distance-based gradient,
                // stays bright right up to an obstacle's surface
                _vertices[RingAStart + i] = edgePos;
                _colors[RingAStart + i] = edgeCol;

                // Ring B: SAME position as Ring A, but forced to edge color.
                // Two vertices sharing a spot with different colors = instant hard cutoff,
                // no gradient bleeding forward onto the visible side.
                _vertices[RingBStart + i] = edgePos;
                _colors[RingBStart + i] = _edgeColor;

                // Ring C: pushed out to darkness radius, fully opaque black
                _vertices[RingCStart + i] = direction * _darknessRadius + Vector3.up * _meshHeightOffset;
                _colors[RingCStart + i] = _voidColor;
            }

            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.triangles = _triangles;
            _mesh.colors = _colors;
            _mesh.RecalculateBounds();
        }

        private float GetVisibleDistance(Vector3 origin, Vector3 direction)
        {
            return Physics.Raycast(origin, direction, out var hit, _viewRadius, _obstacleMask)
                ? hit.distance
                : _viewRadius;
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawDebug)
                return;

            var origin = transform.position + Vector3.up * _rayHeight;

            Gizmos.color = _centerColor;
            Gizmos.DrawWireSphere(origin, _viewRadius);

            Gizmos.color = _edgeColor;
            for (var i = 0; i < _rayCount; i += Mathf.Max(1, _rayCount / 32))
            {
                var angle = i / (float)_rayCount * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Gizmos.DrawLine(origin, origin + direction * GetVisibleDistance(origin, direction));
            }
        }
    }
}