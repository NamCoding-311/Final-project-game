using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 2f;
    private Vector2 _dir;

    public void SetDirection(Vector2 dir) => _dir = dir;

    private void Start() => Destroy(gameObject, lifeTime);

    private void Update() => transform.position += (Vector3)(_dir * speed * Time.deltaTime);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Zombie"))
        {
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }
}
