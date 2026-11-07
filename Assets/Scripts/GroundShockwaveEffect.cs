using System.Collections;
using UnityEngine;

// Hiệu ứng sóng chấn động & bụi đất khi Boss dẫm mạnh xuống mặt đường (Ground Slam)
public class GroundShockwaveEffect : MonoBehaviour
{
    public static void Spawn(Vector3 position, float maxRadius = 6.5f, float duration = 0.55f)
    {
        GameObject shockObj = new GameObject("GroundShockwave", typeof(SpriteRenderer), typeof(GroundShockwaveEffect));
        shockObj.transform.position = position;

        GroundShockwaveEffect fx = shockObj.GetComponent<GroundShockwaveEffect>();
        fx.StartCoroutine(fx.AnimateShockwave(maxRadius, duration));
    }

    private IEnumerator AnimateShockwave(float maxRadius, float duration)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // Tạo texture hình oval / ellipse màu bụi khói xám trắng bán trong suốt
        Texture2D tex = new Texture2D(64, 32, TextureFormat.RGBA32, false);
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                float nx = (x - 32f) / 32f;
                float ny = (y - 16f) / 16f;
                float dist = (nx * nx) + (ny * ny);

                // Viền sóng chấn động dạng vành đai (ring)
                if (dist <= 1f && dist >= 0.55f)
                {
                    float alpha = Mathf.Sin((dist - 0.55f) / 0.45f * Mathf.PI);
                    tex.SetPixel(x, y, new Color(1f, 0.95f, 0.85f, alpha * 0.9f));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0.5f), 32);
        sr.sprite = sprite;
        sr.sortingOrder = -5; // Nằm trên mặt đường nhưng dưới quái và nhân vật

        float elapsed = 0f;
        Vector3 initialScale = new Vector3(0.5f, 0.2f, 1f);
        Vector3 targetScale = new Vector3(maxRadius * 2f, maxRadius * 0.6f, 1f);
        Color startColor = new Color(1f, 0.95f, 0.8f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Nở to nhanh dần đều (Ease Out Quad)
            float scaleT = 1f - Mathf.Pow(1f - t, 2f);
            transform.localScale = Vector3.Lerp(initialScale, targetScale, scaleT);

            // Mờ dần theo thời gian
            sr.color = Color.Lerp(startColor, new Color(1f, 0.9f, 0.8f, 0f), t);

            yield return null;
        }

        Destroy(tex);
        Destroy(gameObject);
    }
}
