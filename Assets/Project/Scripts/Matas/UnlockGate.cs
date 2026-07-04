using UnityEngine;

public class UnlockGate : MonoBehaviour
{
    [SerializeField] private GameObject _gate;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _gate.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}