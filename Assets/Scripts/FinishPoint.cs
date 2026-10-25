
using UnityEngine;

public class FinishPoint : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LevelCompleteUI levelCompleteUI;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || !other.CompareTag(playerTag))
            return;

        if (levelCompleteUI == null)
        {
            Debug.LogError(
                "FinishPoint: LevelCompleteUI is not assigned!",
                this
            );
            return;
        }

        hasTriggered = true;
        levelCompleteUI.ShowCompletePanel();
    }

    [ContextMenu("TEST: Show Complete Panel")]
    private void TestShowCompletePanel()
    {
        if (levelCompleteUI != null)
        {
            levelCompleteUI.ShowCompletePanel();
        }
        else
        {
            Debug.LogError(
                "FinishPoint: Please assign LevelCompleteUI first!",
                this
            );
        }
    }
}
