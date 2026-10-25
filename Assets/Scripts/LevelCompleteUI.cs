
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCompleteUI : MonoBehaviour
{
    [SerializeField] private GameObject levelCompletePanel;

    private bool hasCompleted;

    private void Start()
    {
        Time.timeScale = 1f;

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
    }

    public void ShowCompletePanel()
    {
        if (hasCompleted || levelCompletePanel == null)
            return;

        hasCompleted = true;
        levelCompletePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ContinueToMap2()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map2");
    }

    public void ReplayMap1()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainGame");
    }
}
