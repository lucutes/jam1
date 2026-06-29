using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Scripts.Matas
{
    public class TileDragHandler : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler,
        IPointerClickHandler
    {
        private TilesManager _manager;
        private int _index;

        private CanvasGroup _canvasGroup;


        public void Initialize(TilesManager manager, int index)
        {
            _manager = manager;
            _index = index;

            _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }


        public void UpdateIndex(int index)
        {
            _index = index;
        }


        public void OnBeginDrag(PointerEventData eventData)
        {
            _manager.StartDrag(_index);

            // Allows the object below the cursor to receive OnDrop
            _canvasGroup.blocksRaycasts = false;
        }


        public void OnDrag(PointerEventData eventData)
        {
            // Intentionally empty.
            // Grid tiles snap only after drop.
        }


        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
        }


        public void OnDrop(PointerEventData eventData)
        {
            _manager.DropTile(_index);
        }


        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _manager.SelectTile(_index);
            }
        }
    }
}