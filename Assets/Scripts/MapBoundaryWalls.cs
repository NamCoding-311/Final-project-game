using UnityEngine;

public class MapBoundaryWalls : MonoBehaviour
{
    [SerializeField] private BoxCollider2D mapBounds; // kéo chính BoxCollider2D của Grid vào đây
    [SerializeField] private float wallThickness = 0.5f;

    private void Start()
    {
        Bounds b = mapBounds.bounds;

        CreateWall("Wall_Top", new Vector2(b.center.x, b.max.y + wallThickness / 2f), new Vector2(b.size.x + wallThickness * 2, wallThickness));
        CreateWall("Wall_Bottom", new Vector2(b.center.x, b.min.y - wallThickness / 2f), new Vector2(b.size.x + wallThickness * 2, wallThickness));
        CreateWall("Wall_Left", new Vector2(b.min.x - wallThickness / 2f, b.center.y), new Vector2(wallThickness, b.size.y));
        CreateWall("Wall_Right", new Vector2(b.max.x + wallThickness / 2f, b.center.y), new Vector2(wallThickness, b.size.y));
    }

    private void CreateWall(string name, Vector2 pos, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = transform;
        wall.transform.position = pos;
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
        // isTrigger để mặc định false -> chặn va chạm thật
    }
}