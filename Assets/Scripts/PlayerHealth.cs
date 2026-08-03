using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Slider hpBar; // thêm field mới

    private int _currentHP;
    private bool _isDead;

    private void Start()
    {
        _currentHP = maxHP;
        UpdateHPBar();
    }

    public void TakeDamage(int amount)
    {
        if (_isDead) return;

        _currentHP -= amount;
        UpdateHPBar();

        if (_currentHP <= 0) Die();
    }

    private void UpdateHPBar()
    {
        if (hpBar != null) hpBar.value = (float)_currentHP / maxHP;
    }

    private void Die()
    {
        _isDead = true;
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}