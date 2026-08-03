using UnityEngine;

public class Zombie : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float checkDistance = 1f;

    private Transform _target;
    private PlayerHealth _playerHealth;
    private float _nextAttackTime;
    private bool _isAttacking;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        _target = playerObj.transform;
        _playerHealth = playerObj.GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (_isAttacking) return;

        Vector2 dirToPlayer = (_target.position - transform.position).normalized;
        Vector2 moveDir = FindClearDirection(dirToPlayer);

        if (moveDir != Vector2.zero)
        {
            transform.position += (Vector3)(moveDir * speed * Time.deltaTime);
        }
    }

    private Vector2 FindClearDirection(Vector2 preferredDir)
    {
        if (Physics2D.Raycast(transform.position, preferredDir, checkDistance, obstacleLayer).collider == null)
            return preferredDir;

        float[] angles = { 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 150f, -150f };

        foreach (float angle in angles)
        {
            Vector2 testDir = RotateVector(preferredDir, angle);
            if (Physics2D.Raycast(transform.position, testDir, checkDistance, obstacleLayer).collider == null)
                return testDir;
        }

        return Vector2.zero;
    }

    private Vector2 RotateVector(Vector2 vector, float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(cos * vector.x - sin * vector.y, sin * vector.x + cos * vector.y);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) _isAttacking = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) _isAttacking = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time >= _nextAttackTime)
        {
            _nextAttackTime = Time.time + attackInterval;
            _playerHealth.TakeDamage(damage);
        }
    }
}