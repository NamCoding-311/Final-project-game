
using System.Collections;
using UnityEngine;

public class Map2 : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";

    [Header("Zombie Prefabs")]
    [SerializeField] private GameObject[] zombiePrefabs;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn Settings")]
    [SerializeField] private int zombieCount = 4;
    [SerializeField] private float spawnInterval = 1.5f;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || !other.CompareTag(playerTag))
            return;

        hasTriggered = true;
        StartCoroutine(SpawnEncounter());
    }

    private IEnumerator SpawnEncounter()
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0 ||
            spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning(
                "Map2: Missing zombie prefabs or spawn points.",
                this
            );
            yield break;
        }

        for (int i = 0; i < zombieCount; i++)
        {
            Transform point = spawnPoints[
                Random.Range(0, spawnPoints.Length)
            ];

            GameObject prefab = zombiePrefabs[
                Random.Range(0, zombiePrefabs.Length)
            ];

            Instantiate(prefab, point.position, point.rotation);

            if (i < zombieCount - 1)
                yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log("Map 2 encounter spawned.", this);
    }
}
