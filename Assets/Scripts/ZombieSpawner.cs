
    using UnityEngine;

    public class ZombieSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] Transform playerTransform;
        [SerializeField] private float minSpawnDistance = 5f;
        [SerializeField] private float maxSpawnDistance = 10f;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private int MaxSpawnAttempts = 10;

        [SerializeField] private BoxCollider2D mapBounds;

        private void Start() => InvokeRepeating(nameof(SpawnZombie), 1f, spawnInterval);

        private void SpawnZombie()
    {
        Bounds b = mapBounds.bounds;
        for (int i = 0; i < MaxSpawnAttempts; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Vector2 spawnPosition = (Vector2)playerTransform.position + offset;

            // Ensure the spawn position is within the map bounds
            spawnPosition = new Vector2(
                Mathf.Clamp(spawnPosition.x, mapBounds.bounds.min.x, mapBounds.bounds.max.x),
                Mathf.Clamp(spawnPosition.y, mapBounds.bounds.min.y, mapBounds.bounds.max.y)
            );
            bool isBlocked = Physics2D.OverlapCircle(spawnPosition, 0.3f, obstacleLayer) != null;

            if (!isBlocked)
            {
                Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
                return; // Exit after successfully spawning a zombie
            }
        }
    }
    }