using UnityEngine;

namespace Project.Scripts.Matas
{
    public class InventoryManager : MonoBehaviour
    {
        [SerializeField] private GameObject _pickablesUIObject;
        public static InventoryManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) _pickablesUIObject.SetActive(!_pickablesUIObject.activeSelf);
        }

        public void Collect(InventoryItem item)
        {
            if (item == null || item.UIObject == null)
                return;

            item.UIObject.SetActive(true);
            _pickablesUIObject.SetActive(false);
        }

        public bool HasItem(InventoryItem item)
        {
            return item != null &&
                   item.UIObject != null &&
                   item.UIObject.activeSelf;
        }
    }
}