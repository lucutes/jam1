using UnityEngine;

namespace Project.Scripts.Matas
{
    public class PickupItem : MonoBehaviour
    {
        [SerializeField] private InventoryItem _item;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            Pickup();
        }

        public void Pickup()
        {
            InventoryManager.Instance.Collect(_item);
            gameObject.SetActive(false);
        }
    }
}