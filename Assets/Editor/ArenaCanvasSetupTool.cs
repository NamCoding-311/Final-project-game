#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ArenaCanvasSetupTool : EditorWindow
{
    [MenuItem("Tools/Tự Động Tạo và Gán Giao Diện Arena Canvas (1-Click)")]
    [MenuItem("GameObject/UI/Tự Động Tạo và Gán Arena Canvas (1-Click)", false, 0)]
    [MenuItem("CONTEXT/ArenaManager/Tự Động Tạo và Gán Canvas (1-Click)")]
    [MenuItem("CONTEXT/BrotatoLevelSystem/Tự Động Tạo và Gán Canvas (1-Click)")]
    public static void SetupArenaCanvasUI()
    {
        // 1. Kiểm tra Scene hiện tại
        string currentScene = EditorSceneManager.GetActiveScene().name;
        if (currentScene != "ArenaSurvival")
        {
            bool proceed = EditorUtility.DisplayDialog("Xác nhận Scene",
                $"Bạn đang ở Scene '{currentScene}'. Công cụ này được thiết kế tối ưu nhất cho Scene 'ArenaSurvival'.\n\nBạn có muốn mở Scene 'ArenaSurvival' trước khi thiết lập không?",
                "Mở ArenaSurvival & Thiết Lập", "Tiếp tục ở Scene hiện tại");
            
            if (proceed)
            {
                string scenePath = "Assets/Scenes/ArenaSurvival.unity";
                if (System.IO.File.Exists(scenePath))
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }
        }

        // Đảm bảo EventSystem tồn tại
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
        }

        // 2. Tìm hoặc tạo ArenaCanvas
        Canvas canvas = null;
        GameObject canvasGo = GameObject.Find("ArenaCanvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("ArenaCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create ArenaCanvas");
        }
        else
        {
            canvas = canvasGo.GetComponent<Canvas>();
        }

        // 3. Xây dựng HUD_Panel (Toàn bộ thanh chỉ số khi chơi)
        Transform hudTransform = canvasGo.transform.Find("HUD_Panel");
        if (hudTransform == null)
        {
            GameObject hudGo = new GameObject("HUD_Panel");
            hudGo.transform.SetParent(canvasGo.transform, false);
            RectTransform hudRt = hudGo.AddComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.sizeDelta = Vector2.zero;
            hudTransform = hudGo.transform;
        }

        // --- TOP-CENTER: Wave & Timer ---
        Transform waveBoxTrans = hudTransform.Find("WaveTimer_Box");
        if (waveBoxTrans == null)
        {
            GameObject wbGo = new GameObject("WaveTimer_Box", typeof(RectTransform));
            wbGo.transform.SetParent(hudTransform, false);
            RectTransform rt = wbGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -15);
            rt.sizeDelta = new Vector2(360, 90);
            waveBoxTrans = wbGo.transform;
        }

        TextMeshProUGUI waveTmp = GetOrCreateTMP(waveBoxTrans, "WaveText", "WAVE 1", 34, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(0, 0), new Vector2(320, 42));
        TextMeshProUGUI timerTmp = GetOrCreateTMP(waveBoxTrans, "TimerText", "20s", 28, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center, new Vector2(0, -38), new Vector2(320, 36));

        // --- TOP-RIGHT: Materials (Đá/Tiền) ---
        Transform matBoxTrans = hudTransform.Find("Materials_Box");
        if (matBoxTrans == null)
        {
            GameObject mbGo = new GameObject("Materials_Box", typeof(RectTransform));
            mbGo.transform.SetParent(hudTransform, false);
            RectTransform rt = mbGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-35, -20);
            rt.sizeDelta = new Vector2(250, 60);
            matBoxTrans = mbGo.transform;
        }

        TextMeshProUGUI materialsTmp = GetOrCreateTMP(matBoxTrans, "MaterialsText", "💎 0", 32, FontStyles.Bold, new Color(0.2f, 1f, 0.55f), TextAlignmentOptions.Right, Vector2.zero, new Vector2(250, 50));

        // --- TOP-LEFT: Level, HP Bar, XP Bar ---
        Transform statsBoxTrans = hudTransform.Find("PlayerStats_Box");
        if (statsBoxTrans == null)
        {
            GameObject sbGo = new GameObject("PlayerStats_Box", typeof(RectTransform));
            sbGo.transform.SetParent(hudTransform, false);
            RectTransform rt = sbGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(35, -20);
            rt.sizeDelta = new Vector2(350, 120);
            statsBoxTrans = sbGo.transform;
        }

        TextMeshProUGUI levelTmp = GetOrCreateTMP(statsBoxTrans, "LevelText", "LV. 1", 24, FontStyles.Bold, new Color(0.3f, 0.85f, 1f), TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(150, 32));

        // Slider Máu
        Slider hpSlider = GetOrCreateSlider(statsBoxTrans, "HealthBar", new Vector2(280, 24), new Vector2(0, -32), new Color(0.25f, 0.08f, 0.08f, 0.9f), new Color(0.95f, 0.2f, 0.25f, 1f));
        TextMeshProUGUI hpTmp = GetOrCreateTMP(hpSlider.transform, "HP_Text", "100 / 100", 15, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(280, 24));

        // Slider XP
        Slider xpSlider = GetOrCreateSlider(statsBoxTrans, "XPBar", new Vector2(280, 18), new Vector2(0, -62), new Color(0.12f, 0.16f, 0.2f, 0.9f), new Color(0f, 0.95f, 0.65f, 1f));
        TextMeshProUGUI xpTmp = GetOrCreateTMP(xpSlider.transform, "XP_Text", "XP: 0 / 5", 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(280, 18));

        // --- CENTER BANNER: Wave Completed ---
        Transform bannerTrans = hudTransform.Find("WaveCompletedBanner");
        GameObject bannerGo = null;
        TextMeshProUGUI waveCompleteTmp = null;
        if (bannerTrans == null)
        {
            bannerGo = new GameObject("WaveCompletedBanner", typeof(RectTransform), typeof(Image));
            bannerGo.transform.SetParent(hudTransform, false);
            RectTransform rt = bannerGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 80);
            rt.sizeDelta = new Vector2(620, 110);

            Image img = bannerGo.GetComponent<Image>();
            img.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);

            waveCompleteTmp = GetOrCreateTMP(bannerGo.transform, "WaveCompletedText", "WAVE COMPLETED!", 38, FontStyles.Bold, new Color(1f, 0.85f, 0.15f), TextAlignmentOptions.Center, Vector2.zero, new Vector2(600, 80));
            bannerGo.SetActive(false); // Ẩn mặc định
        }
        else
        {
            bannerGo = bannerTrans.gameObject;
            waveCompleteTmp = bannerTrans.Find("WaveCompletedText")?.GetComponent<TextMeshProUGUI>();
        }

        // 4. Xây dựng LEVEL UP PANEL (Bảng chọn 3 Thẻ bài Nâng cấp khi lên cấp)
        Transform levelUpTrans = canvasGo.transform.Find("LevelUp_Panel");
        GameObject levelUpGo = null;
        List<BrotatoLevelSystem.CustomCardUI> cardsUIList = new List<BrotatoLevelSystem.CustomCardUI>();

        if (levelUpTrans == null)
        {
            levelUpGo = new GameObject("LevelUp_Panel", typeof(RectTransform), typeof(Image));
            levelUpGo.transform.SetParent(canvasGo.transform, false);
            RectTransform rt = levelUpGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image bgOverlay = levelUpGo.GetComponent<Image>();
            bgOverlay.color = new Color(0.04f, 0.06f, 0.09f, 0.85f); // Làm tối hậu cảnh

            // Header Level Up
            GetOrCreateTMP(levelUpGo.transform, "Title_LevelUp", "LEVEL UP!", 46, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center, new Vector2(0, 220), new Vector2(500, 60));
            GetOrCreateTMP(levelUpGo.transform, "Subtitle_LevelUp", "CHOOSE AN UPGRADE TO ENHANCE YOUR POWER", 18, FontStyles.Normal, new Color(0.75f, 0.85f, 0.95f), TextAlignmentOptions.Center, new Vector2(0, 175), new Vector2(650, 40));

            // Container chứa 3 thẻ
            GameObject containerGo = new GameObject("CardsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            containerGo.transform.SetParent(levelUpGo.transform, false);
            RectTransform contRt = containerGo.GetComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0.5f, 0.5f);
            contRt.anchorMax = new Vector2(0.5f, 0.5f);
            contRt.pivot = new Vector2(0.5f, 0.5f);
            contRt.anchoredPosition = new Vector2(0, -30);
            contRt.sizeDelta = new Vector2(960, 420);

            HorizontalLayoutGroup hlg = containerGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Tạo 3 Card UI mẫu
            for (int i = 0; i < 3; i++)
            {
                BrotatoLevelSystem.CustomCardUI cardUI = CreateCardElement(containerGo.transform, i);
                cardsUIList.Add(cardUI);
            }

            levelUpGo.SetActive(false); // Ẩn mặc định, chỉ hiện khi Level Up
        }
        else
        {
            levelUpGo = levelUpTrans.gameObject;
            Transform container = levelUpTrans.Find("CardsContainer");
            if (container != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Transform cTrans = container.Find($"Card_{i}");
                    if (cTrans != null)
                    {
                        BrotatoLevelSystem.CustomCardUI cardUI = new BrotatoLevelSystem.CustomCardUI();
                        cardUI.cardRoot = cTrans.gameObject;
                        cardUI.titleText = cTrans.Find("Title")?.GetComponent<TextMeshProUGUI>();
                        cardUI.descriptionText = cTrans.Find("Description")?.GetComponent<TextMeshProUGUI>();
                        cardUI.borderOrBackground = cTrans.GetComponent<Image>();
                        cardUI.selectButton = cTrans.Find("SelectButton")?.GetComponent<Button>();
                        cardsUIList.Add(cardUI);
                    }
                }
            }
        }

        // 5. TỰ ĐỘNG GÁN VÀO CÁC Ô INSPECTOR (SERIALIZED PROPERTIES)
        // A. Gán vào ArenaManager
        ArenaManager arenaManager = Object.FindAnyObjectByType<ArenaManager>();
        if (arenaManager != null)
        {
            SerializedObject soArena = new SerializedObject(arenaManager);
            soArena.Update();
            soArena.FindProperty("customWaveText").objectReferenceValue = waveTmp;
            soArena.FindProperty("customTimerText").objectReferenceValue = timerTmp;
            soArena.FindProperty("customWaveCompletedBanner").objectReferenceValue = bannerGo;
            soArena.FindProperty("customWaveCompletedText").objectReferenceValue = waveCompleteTmp;
            soArena.ApplyModifiedProperties();
        }

        // B. Gán vào BrotatoLevelSystem
        BrotatoLevelSystem levelSystem = Object.FindAnyObjectByType<BrotatoLevelSystem>();
        if (levelSystem == null && arenaManager != null)
        {
            levelSystem = arenaManager.GetComponent<BrotatoLevelSystem>();
            if (levelSystem == null) levelSystem = arenaManager.gameObject.AddComponent<BrotatoLevelSystem>();
        }

        if (levelSystem != null)
        {
            SerializedObject soLevel = new SerializedObject(levelSystem);
            soLevel.Update();
            soLevel.FindProperty("customXpSlider").objectReferenceValue = xpSlider;
            soLevel.FindProperty("customLevelText").objectReferenceValue = levelTmp;
            soLevel.FindProperty("customMaterialsText").objectReferenceValue = materialsTmp;
            soLevel.FindProperty("customXpText").objectReferenceValue = xpTmp;
            soLevel.FindProperty("customLevelUpPanel").objectReferenceValue = levelUpGo;

            SerializedProperty cardsProp = soLevel.FindProperty("customCardsUI");
            if (cardsProp != null)
            {
                cardsProp.arraySize = cardsUIList.Count;
                for (int i = 0; i < cardsUIList.Count; i++)
                {
                    SerializedProperty elem = cardsProp.GetArrayElementAtIndex(i);
                    elem.FindPropertyRelative("cardRoot").objectReferenceValue = cardsUIList[i].cardRoot;
                    elem.FindPropertyRelative("titleText").objectReferenceValue = cardsUIList[i].titleText;
                    elem.FindPropertyRelative("descriptionText").objectReferenceValue = cardsUIList[i].descriptionText;
                    elem.FindPropertyRelative("borderOrBackground").objectReferenceValue = cardsUIList[i].borderOrBackground;
                    elem.FindPropertyRelative("selectButton").objectReferenceValue = cardsUIList[i].selectButton;
                }
            }

            soLevel.ApplyModifiedProperties();
        }

        // C. Gán vào PlayerHealth
        PlayerHealth playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            SerializedObject soPlayer = new SerializedObject(playerHealth);
            soPlayer.Update();
            soPlayer.FindProperty("hpBar").objectReferenceValue = hpSlider;
            soPlayer.FindProperty("hpText").objectReferenceValue = hpTmp;
            soPlayer.ApplyModifiedProperties();
        }

        // Đánh dấu Scene thay đổi và Lưu lại
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Tạo Canvas Thành Công!",
            "Đã tự động tạo và gán 100% Canvas UI cho chế độ Đấu trường Sinh Tồn (Brotato):\n\n" +
            "✔ Top-Left: Thanh Máu (HP Bar + Text), Thanh Kinh Nghiệm (XP Bar + Text), Cấp độ (Level)\n" +
            "✔ Top-Center: Đợt Sóng (Wave) & Đồng hồ đếm ngược (Timer)\n" +
            "✔ Top-Right: Số Ngọc/Vật liệu nhặt được (💎 Materials)\n" +
            "✔ Center Banner: Thông báo hoàn thành Wave\n" +
            "✔ Pop-up Level Up: Bảng 3 Thẻ Bài Nâng Cấp Kỹ Năng đẹp mắt (Tự động mở khi lên cấp)\n" +
            "✔ Đã gán kết nối thẳng vào ArenaManager, BrotatoLevelSystem và PlayerHealth!",
            "Tuyệt Vời!");
    }

    private static BrotatoLevelSystem.CustomCardUI CreateCardElement(Transform container, int index)
    {
        GameObject cardGo = new GameObject($"Card_{index}", typeof(RectTransform), typeof(Image), typeof(Outline));
        cardGo.transform.SetParent(container, false);
        RectTransform rt = cardGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280, 390);

        Image bgImg = cardGo.GetComponent<Image>();
        bgImg.color = new Color(0.12f, 0.15f, 0.22f, 0.98f);

        Outline outline = cardGo.GetComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.8f);
        outline.effectDistance = new Vector2(3, -3);

        // Header strip nhỏ trên đỉnh thẻ
        GameObject headerGo = new GameObject("HeaderStrip", typeof(RectTransform), typeof(Image));
        headerGo.transform.SetParent(cardGo.transform, false);
        RectTransform hRt = headerGo.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0, 1);
        hRt.anchorMax = new Vector2(1, 1);
        hRt.pivot = new Vector2(0.5f, 1);
        hRt.anchoredPosition = Vector2.zero;
        hRt.sizeDelta = new Vector2(0, 6);
        headerGo.GetComponent<Image>().color = new Color(0.3f, 0.8f, 1f, 1f);

        // Tiêu đề Thẻ
        TextMeshProUGUI titleTmp = GetOrCreateTMP(cardGo.transform, "Title", "DAMAGE", 22, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(0, 130), new Vector2(260, 40));

        // Mô tả Nâng cấp
        TextMeshProUGUI descTmp = GetOrCreateTMP(cardGo.transform, "Description", "+3 Base Damage\nto all equipped weapons", 17, FontStyles.Normal, new Color(0.85f, 0.9f, 0.95f), TextAlignmentOptions.Center, new Vector2(0, 20), new Vector2(250, 120));
        descTmp.enableWordWrapping = true;

        // Nút bấm Select
        GameObject btnGo = new GameObject("SelectButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(cardGo.transform, false);
        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0, 25);
        btnRt.sizeDelta = new Vector2(220, 52);

        Image btnImg = btnGo.GetComponent<Image>();
        btnImg.color = new Color(0.18f, 0.65f, 0.35f, 1f); // Nút màu xanh lá

        Button btn = btnGo.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.25f, 0.8f, 0.45f, 1f);
        cb.pressedColor = new Color(0.12f, 0.5f, 0.25f, 1f);
        btn.colors = cb;

        GetOrCreateTMP(btnGo.transform, "BtnText", "CHOOSE", 20, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(200, 45));

        BrotatoLevelSystem.CustomCardUI cardUI = new BrotatoLevelSystem.CustomCardUI();
        cardUI.cardRoot = cardGo;
        cardUI.titleText = titleTmp;
        cardUI.descriptionText = descTmp;
        cardUI.borderOrBackground = bgImg;
        cardUI.selectButton = btn;

        return cardUI;
    }

    private static TextMeshProUGUI GetOrCreateTMP(Transform parent, string name, string defaultText, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment, Vector2 pos, Vector2 size)
    {
        Transform trans = parent.Find(name);
        GameObject go = null;
        TextMeshProUGUI tmp = null;

        if (trans == null)
        {
            go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            tmp = go.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            go = trans.gameObject;
            tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;

        return tmp;
    }

    private static Slider GetOrCreateSlider(Transform parent, string name, Vector2 size, Vector2 pos, Color bgColor, Color fillColor)
    {
        Transform trans = parent.Find(name);
        if (trans != null) return trans.GetComponent<Slider>();

        GameObject sliderGo = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(parent, false);
        RectTransform sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchoredPosition = pos;
        sliderRt.sizeDelta = size;

        // Background
        GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(sliderGo.transform, false);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        bgGo.GetComponent<Image>().color = bgColor;

        // Fill Area
        GameObject fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        RectTransform faRt = fillAreaGo.GetComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.sizeDelta = Vector2.zero;

        // Fill
        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        Image fillImg = fillGo.GetComponent<Image>();
        fillImg.color = fillColor;

        Slider slider = sliderGo.GetComponent<Slider>();
        slider.targetGraphic = fillImg;
        slider.fillRect = fillRt;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        return slider;
    }
}
#endif
