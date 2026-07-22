using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private Vector2 mapMin;
    [SerializeField] private Vector2 mapMax;

    private void Start() => InvokeRepeating(nameof(SpawnZombie), 1f, spawnInterval);

    private void SpawnZombie()
    {
        Vector2 pos = new Vector2(Random.Range(mapMin.x, mapMax.x), Random.Range(mapMin.y, mapMax.y));
        Instantiate(zombiePrefab, pos, Quaternion.identity);
    }
}