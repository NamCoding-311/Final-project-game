using UnityEngine;

public class PlayerPlaceholderMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D _rb;
    private Vector2 _input;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Đọc input mỗi frame, xử lý vật lý ở FixedUpdate
        _input.x = Input.GetAxisRaw("Horizontal"); // A/D hoặc mũi tên trái/phải
        _input.y = Input.GetAxisRaw("Vertical");    // W/S hoặc mũi tên lên/xuống
        _input.Normalize(); // tránh đi chéo nhanh hơn đi thẳng
    }

    private void FixedUpdate()
    {
        _rb.MovePosition(_rb.position + _input * moveSpeed * Time.fixedDeltaTime);
    }
}