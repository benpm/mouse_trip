using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelPortal2D : MonoBehaviour
{
    [Header("Next Level")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private bool useNextBuildIndex = true;

    [Header("Target")]
    [SerializeField] private LayerMask playerLayer;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (!IsInPlayerLayer(other.gameObject)) return;

        hasTriggered = true;

        LoadNextLevel();
    }

    private void LoadNextLevel()
    {
        if (useNextBuildIndex)
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentIndex + 1);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private bool IsInPlayerLayer(GameObject target)
    {
        return (playerLayer.value & (1 << target.layer)) != 0;
    }
}