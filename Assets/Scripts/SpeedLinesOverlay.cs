using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Hiệu ứng vệt xé gió siêu tốc (Speed Lines) + 2 viền đen điện ảnh (Cinematic Letterbox Bars)
// Che kín 100% rìa trên & rìa dưới của màn hình trong suốt cutscene, đồng thời tạo phong cách Anime / Action Movie cực chất
public class SpeedLinesOverlay : MonoBehaviour
{
    private static SpeedLinesOverlay _instance;
    public static SpeedLinesOverlay Instance => _instance;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private readonly List<SpeedLine> _lines = new List<SpeedLine>();
    private bool _isPlaying = false;
    private Coroutine _fadeCoroutine;

    // 2 thanh viền đen điện ảnh trên và dưới
    private RectTransform _topBar;
    private RectTransform _bottomBar;
    private Image _flashImage;
    private float _targetBarHeight = 65f;
    private Coroutine _barsCoroutine;

    private class SpeedLine
    {
        public RectTransform rect;
        public Image img;
        public float speed;
        public float length;
    }

    private bool _initialized = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        CreateCinematicBars();
        CreateLines(26);
        CreateFlashOverlay();
    }

    private void CreateCinematicBars()
    {
        // 1. Thanh viền đen trên (Top Letterbox Bar)
        GameObject topObj = new GameObject("CinematicBar_Top", typeof(RectTransform), typeof(Image));
        topObj.transform.SetParent(transform, false);
        _topBar = topObj.GetComponent<RectTransform>();
        _topBar.anchorMin = new Vector2(0f, 1f);
        _topBar.anchorMax = new Vector2(1f, 1f);
        _topBar.pivot = new Vector2(0.5f, 1f);
        _topBar.sizeDelta = new Vector2(0f, 0f);
        _topBar.anchoredPosition = Vector2.zero;

        Image topImg = topObj.GetComponent<Image>();
        topImg.color = Color.black;
        topImg.raycastTarget = false;

        // 2. Thanh viền đen dưới (Bottom Letterbox Bar)
        GameObject botObj = new GameObject("CinematicBar_Bottom", typeof(RectTransform), typeof(Image));
        botObj.transform.SetParent(transform, false);
        _bottomBar = botObj.GetComponent<RectTransform>();
        _bottomBar.anchorMin = new Vector2(0f, 0f);
        _bottomBar.anchorMax = new Vector2(1f, 0f);
        _bottomBar.pivot = new Vector2(0.5f, 0f);
        _bottomBar.sizeDelta = new Vector2(0f, 0f);
        _bottomBar.anchoredPosition = Vector2.zero;

        Image botImg = botObj.GetComponent<Image>();
        botImg.color = Color.black;
        botImg.raycastTarget = false;
    }

    private void CreateFlashOverlay()
    {
        GameObject flashObj = new GameObject("FlashOverlay", typeof(RectTransform), typeof(Image));
        flashObj.transform.SetParent(transform, false);
        RectTransform rt = flashObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        _flashImage = flashObj.GetComponent<Image>();
        _flashImage.color = new Color(1f, 1f, 1f, 0f);
        _flashImage.raycastTarget = false;
    }

    private void CreateLines(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject lineObj = new GameObject($"Line_{i}", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(transform, false);

            RectTransform rt = lineObj.GetComponent<RectTransform>();
            Image img = lineObj.GetComponent<Image>();

            // Màu vệt gió trắng phớt xanh neon
            img.color = new Color(0.85f, 0.95f, 1f, Random.Range(0.4f, 0.85f));
            img.raycastTarget = false;

            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);

            float length = Random.Range(200f, 600f);
            float thickness = Random.Range(2f, 5.5f);
            rt.sizeDelta = new Vector2(length, thickness);

            float randomY = Random.Range(-Screen.height * 0.42f, Screen.height * 0.42f);
            float randomX = Random.Range(-100f, Screen.width + 200f);
            rt.anchoredPosition = new Vector2(randomX, randomY);

            _lines.Add(new SpeedLine
            {
                rect = rt,
                img = img,
                speed = Random.Range(2400f, 4800f),
                length = length
            });
        }
    }

    private void Update()
    {
        if (!_isPlaying && _canvasGroup.alpha <= 0.001f) return;

        float dt = Time.deltaTime;
        float screenRight = Screen.width + 120f;
        float screenLeft = -650f;

        for (int i = 0; i < _lines.Count; i++)
        {
            SpeedLine line = _lines[i];
            Vector2 pos = line.rect.anchoredPosition;
            pos.x -= line.speed * dt;

            // Khi vệt gió bay hết mép trái thì tái sinh ở mép phải
            if (pos.x < screenLeft)
            {
                pos.x = screenRight + Random.Range(0f, 300f);
                pos.y = Random.Range(-Screen.height * 0.42f, Screen.height * 0.42f);
                line.length = Random.Range(220f, 600f);
                line.rect.sizeDelta = new Vector2(line.length, Random.Range(2f, 5.5f));
                line.speed = Random.Range(2600f, 5000f);
            }

            line.rect.anchoredPosition = pos;
        }
    }

    public void Play(float duration = 2.5f, float peakAlpha = 0.85f)
    {
        EnsureInitialized();
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(PlayRoutine(duration, peakAlpha));
    }

    public void TriggerFlash(float peakAlpha = 0.5f, float duration = 0.35f)
    {
        EnsureInitialized();
        StartCoroutine(FlashRoutine(peakAlpha, duration));
    }

    private IEnumerator FlashRoutine(float peakAlpha, float duration)
    {
        if (_flashImage == null) yield break;
        float halfTime = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfTime;
            _flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, peakAlpha, t));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfTime;
            _flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(peakAlpha, 0f, t));
            yield return null;
        }

        _flashImage.color = new Color(1f, 1f, 1f, 0f);
    }

    public void Stop()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(0f, 0.35f, () => _isPlaying = false));
        AnimateBars(0f, 0.35f);
    }

    private IEnumerator PlayRoutine(float duration, float peakAlpha)
    {
        _isPlaying = true;

        // Trượt 2 viền đen điện ảnh vào che mép trên và dưới màn hình
        float responsiveBarHeight = Mathf.Clamp(Screen.height * 0.085f, 50f, 85f);
        AnimateBars(responsiveBarHeight, 0.3f);

        // Fade in vệt gió cực nhanh trong 0.25s
        yield return StartCoroutine(FadeTo(peakAlpha, 0.25f, null));

        // Duy trì trong thời gian xe xé gió
        float holdDuration = Mathf.Max(0.1f, duration - 0.75f);
        yield return new WaitForSeconds(holdDuration);

        // Trượt viền đen ra và fade out vệt gió
        AnimateBars(0f, 0.45f);
        yield return StartCoroutine(FadeTo(0f, 0.5f, () => _isPlaying = false));
    }

    private void AnimateBars(float targetHeight, float duration)
    {
        if (_barsCoroutine != null) StopCoroutine(_barsCoroutine);
        _barsCoroutine = StartCoroutine(BarsRoutine(targetHeight, duration));
    }

    private IEnumerator BarsRoutine(float targetHeight, float duration)
    {
        if (_topBar == null || _bottomBar == null) yield break;

        float startHeight = _topBar.sizeDelta.y;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float h = Mathf.Lerp(startHeight, targetHeight, t);
            _topBar.sizeDelta = new Vector2(0f, h);
            _bottomBar.sizeDelta = new Vector2(0f, h);
            yield return null;
        }

        _topBar.sizeDelta = new Vector2(0f, targetHeight);
        _bottomBar.sizeDelta = new Vector2(0f, targetHeight);
    }

    private IEnumerator FadeTo(float targetAlpha, float fadeTime, System.Action onComplete)
    {
        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeTime);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    // Tự động tìm hoặc sinh SpeedLinesOverlay trên Canvas nếu chưa có
    public static SpeedLinesOverlay GetOrCreate(Canvas canvas)
    {
        if (_instance != null) return _instance;

        SpeedLinesOverlay existing = canvas.GetComponentInChildren<SpeedLinesOverlay>(true);
        if (existing != null)
        {
            _instance = existing;
            return existing;
        }

        GameObject overlayObj = new GameObject("SpeedLinesOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(SpeedLinesOverlay));
        overlayObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = overlayObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        // Đặt nằm dưới các panel UI chính để không che nút hay máu
        overlayObj.transform.SetSiblingIndex(Mathf.Max(0, canvas.transform.childCount - 3));

        _instance = overlayObj.GetComponent<SpeedLinesOverlay>();
        return _instance;
    }
}
