using System.Collections;
using UnityEngine;

// Dấu hiệu Cảnh Báo Điểm Sinh Quái (Spawn Danger Crosshair) chuẩn Brotato:
// 1. Xuất hiện tại tọa độ quái sắp spawn.
// 2. Chớp nháy màu đỏ cam nguy hiểm trong 0.75s để người chơi kịp nhận biết.
// 3. Sau khi kết thúc thời gian cảnh báo, kích hoạt callback sinh quái thực sự.
public class SpawnWarningIndicator : MonoBehaviour
{
    public static void ShowWarning(Vector3 position, float duration, System.Action onComplete)
    {
        GameObject go = new GameObject("SpawnIndicator");
        go.transform.position = position;

        SpawnWarningIndicator indicator = go.AddComponent<SpawnWarningIndicator>();
        indicator.StartCoroutine(indicator.WarningRoutine(duration, onComplete));
    }

    private IEnumerator WarningRoutine(float duration, System.Action onComplete)
    {
        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();

        // Tạo dấu chữ thập đỏ X pixel art
        Texture2D tex = new Texture2D(12, 12, TextureFormat.RGBA32, false);
        for (int x = 0; x < 12; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                bool isDiag = (x == y || (11 - x) == y);
                bool isInnerDiag = (Mathf.Abs(x - y) <= 1 || Mathf.Abs((11 - x) - y) <= 1);
                if (isInnerDiag)
                {
                    tex.SetPixel(x, y, new Color(1f, 0.15f, 0.15f, 0.95f)); // Đỏ rực
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f), 12f);
        sr.sortingOrder = 5; // Nằm sát trên mặt sàn

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Chớp nháy nhanh dần khi sắp hết thời gian
            float pulse = 0.8f + Mathf.PingPong(elapsed * 12f, 0.5f);
            transform.localScale = new Vector3(pulse, pulse, 1f);

            float alpha = Mathf.PingPong(elapsed * 10f, 0.6f) + 0.4f;
            sr.color = new Color(1f, 0.2f, 0.2f, alpha);

            yield return null;
        }

        onComplete?.Invoke();
        Destroy(gameObject);
    }
}
