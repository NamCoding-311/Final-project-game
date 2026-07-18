using UnityEngine;

public class PlaceholderMapGenerator : MonoBehaviour
{
    [SerializeField] private int width = 30;
    [SerializeField] private int height = 20;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Color[] palette = new Color[]
    {
        Color.red, Color.green, Color.yellow, new Color(0.6f, 0.2f, 0.8f)
    };

    private Sprite _whiteSprite;

    private void Start()
    {
        CreateWhiteSprite();
        GenerateGrid();
        SetupBoundsCollider();
        SetupWalls();
    }

    private void CreateWhiteSprite()
    {
        // Tạo 1 texture trắng dùng chung cho mọi tile — chỉ tạo 1 lần duy nhất, không lãng phí bộ nhớ
        Texture2D tex = new Texture2D(4, 4);
        Color32[] pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels32(pixels);
        tex.filterMode = FilterMode.Point; // giữ nét, đúng phong cách pixel art sau này
        tex.Apply();

        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

[SerializeField] private int zoneSize = 5; // mỗi khối 5x5 ô cùng màu

    private void GenerateGrid()
{
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            GameObject tile = new GameObject($"Tile_{x}_{y}");
            tile.transform.parent = transform;
            tile.transform.position = new Vector3(x * cellSize, y * cellSize, 0f);
            tile.transform.localScale = Vector3.one * cellSize * 1f;

            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = _whiteSprite;

            // Dùng zone index làm seed -> cùng zone luôn ra cùng màu
            int zoneX = x / zoneSize;
            int zoneY = y / zoneSize;
            int colorIndex = (zoneX * 7 + zoneY * 13) % palette.Length;
            sr.color = palette[colorIndex];
        }
    }
}

    private void SetupWalls()
{
    float w = width * cellSize;
    float h = height * cellSize;
    float t = 0.5f; // độ dày tường

    CreateWall(new Vector2(w / 2f - cellSize / 2f, -t / 2f), new Vector2(w, t));       // dưới
    CreateWall(new Vector2(w / 2f - cellSize / 2f, h - cellSize / 2f + t / 2f), new Vector2(w, t)); // trên
    CreateWall(new Vector2(-t / 2f, h / 2f - cellSize / 2f), new Vector2(t, h));       // trái
    CreateWall(new Vector2(w - cellSize / 2f + t / 2f, h / 2f - cellSize / 2f), new Vector2(t, h)); // phải
}

private void CreateWall(Vector2 pos, Vector2 size)
{
    GameObject wall = new GameObject("Wall");
    wall.transform.parent = transform;
    wall.transform.localPosition = pos;
    BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
    col.size = size;
    // isTrigger để mặc định false -> chặn va chạm thật
}
    private void SetupBoundsCollider()
    {
        BoxCollider2D bounds = gameObject.AddComponent<BoxCollider2D>();
        bounds.isTrigger = true;
        bounds.size = new Vector2(width * cellSize, height * cellSize);
        bounds.offset = new Vector2((width * cellSize) / 2f - cellSize / 2f,
                                     (height * cellSize) / 2f - cellSize / 2f);
    }
}