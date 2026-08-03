using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    public void OnRestartButtonClick() => playerHealth.Restart();
}