using UnityEngine;

// Component phụ gắn vào từng Chunk để đánh dấu loại Prefab và chiều rộng thực tế khi thu hồi
public class ChunkIdentifier : MonoBehaviour
{
    public int PrefabIndex { get; set; }
    public float ActualWidth { get; set; } = 50f;
}
