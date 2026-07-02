using System.Collections.Generic;
using UnityEngine;

/*
zydri circles hiding spots, jei player yra AI chasing circle ir tuo paciu pasislepia, AI vistiek
toliau gaudys, zdz turi buti uz circle NE chase sequence metu jei nori pasislept.
*/

namespace Project.Scripts.Matas
{
    public class HidingSpot : MonoBehaviour
    {
        private static readonly List<HidingSpot> ActiveSpots = new();

        [SerializeField] private float _radius = 1.25f;
        [SerializeField] private bool _drawDebug = true;
        [SerializeField] private Color _debugColor = new(0.25f, 0.9f, 0.45f, 0.8f);

        public float Radius => _radius;

        private void OnEnable()
        {
            if (!ActiveSpots.Contains(this))
                ActiveSpots.Add(this);
        }

        private void OnDisable()
        {
            ActiveSpots.Remove(this);
        }

        public static bool IsPositionHidden(Vector3 position)
        {
            foreach (var spot in ActiveSpots)
            {
                if (spot == null || !spot.isActiveAndEnabled)
                    continue;

                var offset = position - spot.transform.position;
                offset.y = 0f;

                if (offset.sqrMagnitude <= spot.Radius * spot.Radius)
                    return true;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            if (!_drawDebug)
                return;

            Gizmos.color = _debugColor;
            var center = transform.position + Vector3.up * 0.06f;

            DrawCircle(center, _radius);
            Gizmos.DrawLine(center + Vector3.forward * _radius, center - Vector3.forward * _radius);
            Gizmos.DrawLine(center + Vector3.right * _radius, center - Vector3.right * _radius);
            Gizmos.DrawSphere(center, 0.08f);
        }

        private static void DrawCircle(Vector3 center, float radius)
        {
            const int segments = 48;
            var previous = center + Vector3.forward * radius;

            for (var i = 1; i <= segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                var next = center + new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);

                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
