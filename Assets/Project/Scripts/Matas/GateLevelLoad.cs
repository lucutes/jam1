using UnityEngine;
using UnityEngine.SceneManagement;

public class GateLevelLoad : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            // Load the next level
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}