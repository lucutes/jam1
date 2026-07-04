using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Matas
{
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAI : MonoBehaviour
    {
        public enum State
        {
            Patrol,
            Chase,
            Search
        }

        [Header("References")]
        [SerializeField] private TilesManager _tiles;
        [SerializeField] private Transform _player;
        [SerializeField] private Rigidbody _rb;

        [Header("AI Settings")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _viewDistance = 6f;
        [SerializeField] private float _viewAngle = 90f;
        [SerializeField] private float _closeDetectionDistance = 0.65f;
        [SerializeField] private bool _requireViewAngle;
        [SerializeField] private float _arrivalDistance = 0.08f;
        [SerializeField] private float _repathInterval = 0.5f;
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Debug")]
        [SerializeField] private bool _drawDebug = true;
        [SerializeField] private Color _patrolColor = new(0.1f, 0.7f, 1f);
        [SerializeField] private Color _chaseColor = new(1f, 0.25f, 0.15f);
        [SerializeField] private Color _sightColor = new(1f, 0.9f, 0.1f);
        [SerializeField] private Color _closeDetectionColor = new(1f, 0.45f, 0f);
        [SerializeField] private Color _hiddenColor = Color.cyan;
        [SerializeField] private Color _blockedColor = Color.red;

        private readonly List<int> _path = new();
        private State _state = State.Patrol;
        private Vector3 _lookDirection = Vector3.forward;
        private Vector3 _lastKnownPlayerPosition;
        private float _repathTimer;
        private int _lastKnownPlayerTile = -1;

        private void Awake()
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            _rb.useGravity = false;
            _rb.freezeRotation = true;
        }

        private void Update()
        {
            if (!HasRequiredReferences())
                return;

            var canSeePlayer = CanSeePlayer();

            if (canSeePlayer)
            {
                if (_state != State.Chase)
                    SetPathTo(GetNearestPlayerTile());

                _state = State.Chase;
                _lastKnownPlayerPosition = _player.position;
                _lastKnownPlayerTile = GetNearestPlayerTile();
            }
            else if (_state == State.Chase)
            {
                _state = State.Search;
                _path.Clear();
            }

            switch (_state)
            {
                case State.Patrol:
                    PatrolUpdate();
                    break;
                case State.Chase:
                    ChaseUpdate();
                    break;
                case State.Search:
                    SearchUpdate();
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (!HasRequiredReferences())
                return;

            MoveAlongPath();
        }

        private bool HasRequiredReferences()
        {
            return _tiles != null &&
                   _tiles.IsReady &&
                   _tiles.WorldTileCount > 0 &&
                   _player != null &&
                   _rb != null;
        }

        private void PatrolUpdate()
        {
            if (_path.Count > 0)
                return;

            var randomTile = Random.Range(0, _tiles.WorldTileCount);
            SetPathTo(randomTile);
        }

        private void ChaseUpdate()
        {
            _repathTimer += Time.deltaTime;

            if (_repathTimer < _repathInterval)
                return;

            _repathTimer = 0f;
            SetPathTo(GetNearestPlayerTile());
        }

        private void SearchUpdate()
        {
            var direction = GetFlatDirectionTo(_lastKnownPlayerPosition);

            if (direction.magnitude > _arrivalDistance)
                return;

            _state = State.Patrol;
        }

        private void MoveAlongPath()
        {
            if (_state == State.Chase && CanSeePlayer())
            {
                MoveTowards(_player.position);
                return;
            }

            if (_state == State.Search)
            {
                MoveTowards(_lastKnownPlayerPosition);
                return;
            }

            if (_path.Count == 0)
                return;

            var targetPos = GetTileWorldPosition(_path[0]);
            var direction = GetFlatDirectionTo(targetPos);

            if (direction.magnitude <= _arrivalDistance)
            {
                _path.RemoveAt(0);
                return;
            }

            MoveTowards(targetPos);
        }

        private void MoveTowards(Vector3 targetPosition)
        {
            var direction = GetFlatDirectionTo(targetPosition);

            if (direction.magnitude <= _arrivalDistance)
                return;

            _lookDirection = direction.normalized;
            _rb.MovePosition(_rb.position + _lookDirection * _moveSpeed * Time.fixedDeltaTime);
        }

        private Vector3 GetFlatDirectionTo(Vector3 targetPosition)
        {
            var direction = targetPosition - transform.position;
            direction.y = 0f;
            return direction;
        }

        private bool CanSeePlayer()
        {
            if (_player == null)
                return false;

            var toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;

            var distance = toPlayer.magnitude;
            var foundByCloseDetection = distance <= _closeDetectionDistance;

            if (distance <= 0.001f || (!foundByCloseDetection && distance > _viewDistance))
                return false;

            var playerIsHidden = HidingSpot.IsPositionHidden(_player.position);

            if (playerIsHidden && _state != State.Chase && !foundByCloseDetection)
                return false;

            if (!foundByCloseDetection && _requireViewAngle && !IsPlayerInsideViewAngle(toPlayer.normalized))
                return false;

            return HasClearLineToPlayer(toPlayer.normalized, distance);
        }

        private bool IsPlayerInsideCloseDetection()
        {
            if (_player == null)
                return false;

            return GetFlatDirectionTo(_player.position).magnitude <= _closeDetectionDistance;
        }

        private bool IsPlayerInsideViewAngle(Vector3 directionToPlayer)
        {
            return Vector3.Angle(_lookDirection, directionToPlayer) <= _viewAngle * 0.5f;
        }

        private bool HasClearLineToPlayer(Vector3 directionToPlayer, float distance)
        {
            var rayOrigin = transform.position + Vector3.up * 0.5f;
            return !Physics.Raycast(rayOrigin, directionToPlayer, distance, _obstacleMask);
        }

        private void SetPathTo(int targetTile)
        {
            if (targetTile < 0)
                return;

            _path.Clear();
            _path.AddRange(FindPath(GetNearestTile(), targetTile));

            if (_path.Count > 0 && _path[0] == GetNearestTile())
                _path.RemoveAt(0);
        }

        private List<int> FindPath(int start, int goal)
        {
            var open = new List<int> { start };
            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, float> { [start] = 0f };
            var fScore = new Dictionary<int, float> { [start] = Heuristic(start, goal) };

            while (open.Count > 0)
            {
                var current = GetLowestF(open, fScore);

                if (current == goal)
                    return Reconstruct(cameFrom, current);

                open.Remove(current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    var tentative = gScore[current] + 1f;

                    if (gScore.ContainsKey(neighbor) && tentative >= gScore[neighbor])
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative;
                    fScore[neighbor] = tentative + Heuristic(neighbor, goal);

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }

            return new List<int>();
        }

        private List<int> GetNeighbors(int index)
        {
            var neighbors = new List<int>();

            foreach (TileSide side in System.Enum.GetValues(typeof(TileSide)))
            {
                if (_tiles.CanMoveBetween(index, side, out var neighbor))
                    neighbors.Add(neighbor);
            }

            return neighbors;
        }

        private int GetNearestTile()
        {
            return GetTileFromPosition(transform.position);
        }

        private int GetNearestPlayerTile()
        {
            return GetTileFromPosition(_player.position);
        }

        private int GetTileFromPosition(Vector3 position)
        {
            var bestDistance = float.MaxValue;
            var bestIndex = 0;

            for (var i = 0; i < _tiles.WorldTileCount; i++)
            {
                var tilePosition = GetTileWorldPosition(i);
                var distance = Vector3.SqrMagnitude(position - tilePosition);

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestIndex = i;
            }

            return bestIndex;
        }

        private Vector3 GetTileWorldPosition(int index)
        {
            return _tiles.TryGetWorldTilePosition(index, out var position)
                ? position
                : transform.position;
        }

        private int GetLowestF(List<int> open, Dictionary<int, float> fScore)
        {
            var best = open[0];
            var bestScore = fScore.TryGetValue(best, out var score) ? score : float.MaxValue;

            foreach (var index in open)
            {
                score = fScore.TryGetValue(index, out var value) ? value : float.MaxValue;

                if (score >= bestScore)
                    continue;

                best = index;
                bestScore = score;
            }

            return best;
        }

        private float Heuristic(int a, int b)
        {
            return Vector3.Distance(GetTileWorldPosition(a), GetTileWorldPosition(b));
        }

        private List<int> Reconstruct(Dictionary<int, int> cameFrom, int current)
        {
            var path = new List<int> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }

            return path;
        }

        private void OnDrawGizmos()
        {
            if (!_drawDebug)
                return;

            var origin = transform.position + Vector3.up * 0.05f;
            var forward = _lookDirection.sqrMagnitude > 0.001f ? _lookDirection.normalized : transform.forward;
            var left = Quaternion.AngleAxis(-_viewAngle * 0.5f, Vector3.up) * forward;
            var right = Quaternion.AngleAxis(_viewAngle * 0.5f, Vector3.up) * forward;

            Gizmos.color = _sightColor;
            Gizmos.DrawWireSphere(origin, _viewDistance);
            Gizmos.DrawLine(origin, origin + left * _viewDistance);
            Gizmos.DrawLine(origin, origin + right * _viewDistance);

            Gizmos.color = _closeDetectionColor;
            Gizmos.DrawWireSphere(origin, _closeDetectionDistance);

            if (_player != null)
            {
                if (CanSeePlayer() && IsPlayerInsideCloseDetection())
                    Gizmos.color = _closeDetectionColor;
                else if (CanSeePlayer())
                    Gizmos.color = Color.green;
                else if (HidingSpot.IsPositionHidden(_player.position))
                    Gizmos.color = _hiddenColor;
                else
                    Gizmos.color = _blockedColor;

                Gizmos.DrawLine(origin, _player.position + Vector3.up * 0.05f);
            }

            if (_state == State.Search)
            {
                Gizmos.color = _chaseColor;
                Gizmos.DrawWireSphere(_lastKnownPlayerPosition + Vector3.up * 0.1f, 0.2f);
            }

            if (_path == null || _path.Count == 0 || _tiles == null)
                return;

            Gizmos.color = _state == State.Chase ? _chaseColor : _patrolColor;

            var previous = transform.position + Vector3.up * 0.1f;

            foreach (var tileIndex in _path)
            {
                var next = GetTileWorldPosition(tileIndex) + Vector3.up * 0.1f;
                Gizmos.DrawSphere(next, 0.08f);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
