using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Matas
{
    public class TileConnectionVisualizer : MonoBehaviour
    {
        [Header("Edge Images")] [SerializeField]
        private Image _top;

        [SerializeField] private Image _right;
        [SerializeField] private Image _bottom;
        [SerializeField] private Image _left;

        public void Set(TileState state)
        {
            _top.color = GetColor(state.Top);
            _right.color = GetColor(state.Right);
            _bottom.color = GetColor(state.Bottom);
            _left.color = GetColor(state.Left);
        }

        private Color GetColor(TileConnectionType type)
        {
            return type switch
            {
                TileConnectionType.Grass => new Color(0.2f, 0.8f, 0.2f),
                TileConnectionType.Dirt => new Color(0.45f, 0.25f, 0.1f),
                TileConnectionType.Path => new Color(0.85f, 0.75f, 0.2f),
                TileConnectionType.River => new Color(0.2f, 0.5f, 0.9f),
                TileConnectionType.Water => new Color(0.1f, 0.3f, 0.8f),
                _ => Color.magenta
            };
        }
    }
}