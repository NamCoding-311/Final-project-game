using UnityEngine;

public class Zombie : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    private Transform _target;

    private void Start() => _target = GameObject.FindGameObjectWithTag("Player").transform;

    private void Update()
    {
        Vector2 dir = (_target.position - transform.position).normalized;
        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }
}