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
    [MenuItem("CONTEXT/ArenaShopSystem/Tự Động Tạo và Gán Canvas (1-Click)")]
    [MenuItem("CONTEXT/ArenaGameOverSystem/Tự Động Tạo và Gán Canvas (1-Click)")]
    public static void SetupArenaCanvasUI()
    {
        SetupArenaCanvasUIInternal(silent: false);
    }

    [InitializeOnLoadMethod]
    private static void AutoRunOnce()
    {
        EditorApplication.delayCall += () =>
        {
            string currentScene = EditorSceneManager.GetActiveScene().name;
            if (currentScene != "ArenaSurvival") return;

            GameObject canvasGo = GameObject.Find("ArenaCanvas");
            if (canvasGo != null && canvasGo.transform.Find("Shop_Panel") != null && canvasGo.transform.Find("EndGame_Panel") != null)
            {
                return;
            }

            SetupArenaCanvasUIInternal(silent: true);
        };
    }

    public static void SetupArenaCanvasUIInternal(bool silent)
    {
        // 1. Kiểm tra Scene hiện tại
        string currentScene = EditorSceneManager.GetActiveScene().name;
        if (currentScene != "ArenaSurvival")
        {
            if (!silent)
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
            else
            {
                return;
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

        TextMeshProUGUI materialsTmp = GetOrCreateTMP(matBoxTrans, "MaterialsText", "0 GEMS", 30, FontStyles.Bold, new Color(0.2f, 1f, 0.55f), TextAlignmentOptions.Right, Vector2.zero, new Vector2(250, 50));

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

        // 5. Xây dựng SHOP PANEL (Cửa Hàng Giữa Các Hiệp - Inter-Wave Shop)
        Transform shopTrans = canvasGo.transform.Find("Shop_Panel");
        GameObject shopGo = null;
        TextMeshProUGUI shopWaveTitleTmp = null;
        TextMeshProUGUI shopMaterialsTmp = null;
        Button rerollBtn = null;
        TextMeshProUGUI rerollCostTmp = null;
        Button nextWaveBtn = null;
        TextMeshProUGUI nextWaveBtnTmp = null;
        List<ArenaShopSystem.ShopCardUI> shopCardsList = new List<ArenaShopSystem.ShopCardUI>();

        if (shopTrans == null)
        {
            shopGo = new GameObject("Shop_Panel", typeof(RectTransform), typeof(Image));
            shopGo.transform.SetParent(canvasGo.transform, false);
            RectTransform rt = shopGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image bgOverlay = shopGo.GetComponent<Image>();
            bgOverlay.color = new Color(0.04f, 0.06f, 0.08f, 0.94f); // Tối làm nổi bật shop

            // Header Box
            GameObject headerBox = new GameObject("Header_Box", typeof(RectTransform));
            headerBox.transform.SetParent(shopGo.transform, false);
            RectTransform hRt = headerBox.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 0.5f);
            hRt.anchorMax = new Vector2(0.5f, 0.5f);
            hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.anchoredPosition = new Vector2(0, 310);
            hRt.sizeDelta = new Vector2(1100, 130);

            shopWaveTitleTmp = GetOrCreateTMP(headerBox.transform, "WaveTitleText", "WAVE 1 COMPLETED - SURVIVAL SHOP", 34, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center, new Vector2(0, 32), new Vector2(1000, 48));
            GetOrCreateTMP(headerBox.transform, "SubtitleText", "USE GEMS TO PURCHASE NEW WEAPONS & STAT UPGRADES", 17, FontStyles.Normal, new Color(0.75f, 0.85f, 0.95f), TextAlignmentOptions.Center, new Vector2(0, -6), new Vector2(1000, 30));
            shopMaterialsTmp = GetOrCreateTMP(headerBox.transform, "MaterialsText", "0 GEMS", 26, FontStyles.Bold, new Color(0.2f, 1f, 0.55f), TextAlignmentOptions.Center, new Vector2(0, -42), new Vector2(500, 36));

            // Cards Container (4 Thẻ)
            GameObject containerGo = new GameObject("ShopCardsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            containerGo.transform.SetParent(shopGo.transform, false);
            RectTransform contRt = containerGo.GetComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0.5f, 0.5f);
            contRt.anchorMax = new Vector2(0.5f, 0.5f);
            contRt.pivot = new Vector2(0.5f, 0.5f);
            contRt.anchoredPosition = new Vector2(0, 0);
            contRt.sizeDelta = new Vector2(1150, 420);

            HorizontalLayoutGroup hlg = containerGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            for (int i = 0; i < 4; i++)
            {
                ArenaShopSystem.ShopCardUI scUI = CreateShopCardElement(containerGo.transform, i);
                shopCardsList.Add(scUI);
            }

            // Bottom Actions Box
            GameObject bottomBox = new GameObject("BottomActions_Box", typeof(RectTransform));
            bottomBox.transform.SetParent(shopGo.transform, false);
            RectTransform bRt = bottomBox.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0.5f);
            bRt.anchorMax = new Vector2(0.5f, 0.5f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(0, -290);
            bRt.sizeDelta = new Vector2(1100, 75);

            // Reroll Button
            GameObject rerollGo = new GameObject("RerollButton", typeof(RectTransform), typeof(Image), typeof(Button));
            rerollGo.transform.SetParent(bottomBox.transform, false);
            RectTransform rrRt = rerollGo.GetComponent<RectTransform>();
            rrRt.anchorMin = new Vector2(0f, 0.5f);
            rrRt.anchorMax = new Vector2(0f, 0.5f);
            rrRt.pivot = new Vector2(0f, 0.5f);
            rrRt.anchoredPosition = new Vector2(30, 0);
            rrRt.sizeDelta = new Vector2(300, 56);
            rerollGo.GetComponent<Image>().color = new Color(0.9f, 0.55f, 0.15f, 1f);
            rerollBtn = rerollGo.GetComponent<Button>();
            rerollCostTmp = GetOrCreateTMP(rerollGo.transform, "RerollCostText", "Reroll (15)", 20, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(280, 50));

            // Next Wave Button
            GameObject nextGo = new GameObject("NextWaveButton", typeof(RectTransform), typeof(Image), typeof(Button));
            nextGo.transform.SetParent(bottomBox.transform, false);
            RectTransform nxRt = nextGo.GetComponent<RectTransform>();
            nxRt.anchorMin = new Vector2(1f, 0.5f);
            nxRt.anchorMax = new Vector2(1f, 0.5f);
            nxRt.pivot = new Vector2(1f, 0.5f);
            nxRt.anchoredPosition = new Vector2(-30, 0);
            nxRt.sizeDelta = new Vector2(300, 56);
            nextGo.GetComponent<Image>().color = new Color(0.15f, 0.65f, 0.95f, 1f);
            nextWaveBtn = nextGo.GetComponent<Button>();
            nextWaveBtnTmp = GetOrCreateTMP(nextGo.transform, "NextWaveText", "NEXT WAVE >", 20, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(280, 50));

            shopGo.SetActive(false); // Ẩn mặc định, chỉ mở khi hết Wave
        }
        else
        {
            shopGo = shopTrans.gameObject;
            Transform headerBox = shopTrans.Find("Header_Box");
            if (headerBox != null)
            {
                shopWaveTitleTmp = headerBox.Find("WaveTitleText")?.GetComponent<TextMeshProUGUI>();
                shopMaterialsTmp = headerBox.Find("MaterialsText")?.GetComponent<TextMeshProUGUI>();
            }

            Transform container = shopTrans.Find("ShopCardsContainer");
            if (container != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Transform cTrans = container.Find($"ShopCard_{i}");
                    if (cTrans != null)
                    {
                        ArenaShopSystem.ShopCardUI cardUI = new ArenaShopSystem.ShopCardUI();
                        cardUI.cardRoot = cTrans.gameObject;
                        cardUI.nameText = cTrans.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                        cardUI.descText = cTrans.Find("DescText")?.GetComponent<TextMeshProUGUI>();
                        cardUI.priceText = cTrans.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
                        cardUI.borderOrIcon = cTrans.Find("AccentStrip")?.GetComponent<Image>() ?? cTrans.GetComponent<Image>();
                        cardUI.buyButton = cTrans.Find("BuyButton")?.GetComponent<Button>();
                        cardUI.buyButtonText = cTrans.Find("BuyButton/BuyButtonText")?.GetComponent<TextMeshProUGUI>();
                        shopCardsList.Add(cardUI);
                    }
                }
            }

            Transform bottomBox = shopTrans.Find("BottomActions_Box");
            if (bottomBox != null)
            {
                Transform rrTrans = bottomBox.Find("RerollButton");
                if (rrTrans != null)
                {
                    rerollBtn = rrTrans.GetComponent<Button>();
                    rerollCostTmp = rrTrans.Find("RerollCostText")?.GetComponent<TextMeshProUGUI>();
                }

                Transform nxTrans = bottomBox.Find("NextWaveButton");
                if (nxTrans != null)
                {
                    nextWaveBtn = nxTrans.GetComponent<Button>();
                    nextWaveBtnTmp = nxTrans.Find("NextWaveText")?.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        // 6. Xây dựng ENDGAME PANEL (Thắng / Thua & Tổng kết trận đấu)
        Transform endGameTrans = canvasGo.transform.Find("EndGame_Panel");
        GameObject endGameGo = null;
        TextMeshProUGUI endTitleTmp = null;
        TextMeshProUGUI endSubtitleTmp = null;
        Image endAccentImg = null;
        TextMeshProUGUI endWaveStatTmp = null;
        TextMeshProUGUI endTimeStatTmp = null;
        TextMeshProUGUI endKillsStatTmp = null;
        TextMeshProUGUI endGemsStatTmp = null;
        TextMeshProUGUI endWeaponsStatTmp = null;
        Button endRestartBtn = null;
        Button endEndlessBtn = null;
        Button endMenuBtn = null;

        if (endGameTrans == null)
        {
            endGameGo = new GameObject("EndGame_Panel", typeof(RectTransform), typeof(Image));
            endGameGo.transform.SetParent(canvasGo.transform, false);
            RectTransform rt = endGameGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image bgOverlay = endGameGo.GetComponent<Image>();
            bgOverlay.color = new Color(0.02f, 0.03f, 0.06f, 0.92f); // Nền tối làm nổi bật bảng kết quả

            // Hộp trung tâm (Center Modal Dialog)
            GameObject dialogGo = new GameObject("EndGame_Dialog", typeof(RectTransform), typeof(Image), typeof(Outline));
            dialogGo.transform.SetParent(endGameGo.transform, false);
            RectTransform dRt = dialogGo.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0.5f);
            dRt.anchorMax = new Vector2(0.5f, 0.5f);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.anchoredPosition = Vector2.zero;
            dRt.sizeDelta = new Vector2(680, 580);

            Image dImg = dialogGo.GetComponent<Image>();
            dImg.color = new Color(0.10f, 0.13f, 0.19f, 0.98f);

            Outline dOutline = dialogGo.GetComponent<Outline>();
            dOutline.effectColor = new Color(0.35f, 0.55f, 0.85f, 0.6f);
            dOutline.effectDistance = new Vector2(3, -3);

            // Viền Accent Strip trên đỉnh hộp
            GameObject headerStripGo = new GameObject("AccentStrip", typeof(RectTransform), typeof(Image));
            headerStripGo.transform.SetParent(dialogGo.transform, false);
            RectTransform hRt = headerStripGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1);
            hRt.anchorMax = new Vector2(1, 1);
            hRt.pivot = new Vector2(0.5f, 1);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = new Vector2(0, 8);
            endAccentImg = headerStripGo.GetComponent<Image>();
            endAccentImg.color = new Color(1f, 0.85f, 0.2f, 1f);

            // Tiêu đề Thắng / Thua
            endTitleTmp = GetOrCreateTMP(dialogGo.transform, "TitleText", "VICTORY!", 34, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center, new Vector2(0, 220), new Vector2(640, 50));

            // Phụ đề
            endSubtitleTmp = GetOrCreateTMP(dialogGo.transform, "SubtitleText", "YOU SURVIVED ALL 10 WAVES AND CLEARED THE ZOMBIE APOCALYPSE!", 15, FontStyles.Normal, new Color(0.85f, 0.92f, 1f), TextAlignmentOptions.Center, new Vector2(0, 165), new Vector2(620, 50));
            endSubtitleTmp.enableWordWrapping = true;

            // Khung chứa các dòng thống kê (Stats Container)
            GameObject statsBox = new GameObject("Stats_Container", typeof(RectTransform));
            statsBox.transform.SetParent(dialogGo.transform, false);
            RectTransform sRt = statsBox.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 0.5f);
            sRt.anchorMax = new Vector2(0.5f, 0.5f);
            sRt.pivot = new Vector2(0.5f, 0.5f);
            sRt.anchoredPosition = new Vector2(0, 15);
            sRt.sizeDelta = new Vector2(580, 220);

            float rowStartY = 85f;
            float rowGap = 42f;

            endWaveStatTmp = CreateStatRow(statsBox.transform, "Wave_Row", "WAVES SURVIVED:", "10 (COMPLETED)", Color.cyan, new Vector2(0, rowStartY));
            endTimeStatTmp = CreateStatRow(statsBox.transform, "Time_Row", "BATTLE TIME:", "05:20", Color.white, new Vector2(0, rowStartY - rowGap));
            endKillsStatTmp = CreateStatRow(statsBox.transform, "Kills_Row", "ZOMBIES KILLED:", "180 Kills", new Color(1f, 0.45f, 0.45f), new Vector2(0, rowStartY - rowGap * 2));
            endGemsStatTmp = CreateStatRow(statsBox.transform, "Gems_Row", "GEMS COLLECTED:", "350 Gems", new Color(0.2f, 1f, 0.55f), new Vector2(0, rowStartY - rowGap * 3));
            endWeaponsStatTmp = CreateStatRow(statsBox.transform, "Weapons_Row", "WEAPONS OWNED:", "6/6 Weapons", new Color(1f, 0.75f, 0.2f), new Vector2(0, rowStartY - rowGap * 4));

            // Khung chứa các nút bấm (Buttons Container)
            GameObject btnBox = new GameObject("Buttons_Container", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            btnBox.transform.SetParent(dialogGo.transform, false);
            RectTransform bRt = btnBox.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0.5f);
            bRt.anchorMax = new Vector2(0.5f, 0.5f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(0, -225);
            bRt.sizeDelta = new Vector2(620, 56);

            HorizontalLayoutGroup bHlg = btnBox.GetComponent<HorizontalLayoutGroup>();
            bHlg.spacing = 20;
            bHlg.childAlignment = TextAnchor.MiddleCenter;
            bHlg.childControlWidth = false;
            bHlg.childControlHeight = false;
            bHlg.childForceExpandWidth = false;
            bHlg.childForceExpandHeight = false;

            endRestartBtn = CreateActionButton(btnBox.transform, "RestartButton", "RESTART", new Color(0.18f, 0.65f, 0.35f, 1f), new Vector2(180, 52));
            endEndlessBtn = CreateActionButton(btnBox.transform, "EndlessButton", "ENDLESS >", new Color(0.92f, 0.70f, 0.15f, 1f), new Vector2(180, 52));
            endMenuBtn = CreateActionButton(btnBox.transform, "MenuButton", "MAIN MENU", new Color(0.35f, 0.45f, 0.62f, 1f), new Vector2(180, 52));

            endGameGo.SetActive(false); // Ẩn mặc định
        }
        else
        {
            endGameGo = endGameTrans.gameObject;
            Transform dialogTrans = endGameTrans.Find("EndGame_Dialog") ?? endGameTrans;
            endTitleTmp = dialogTrans.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            endSubtitleTmp = dialogTrans.Find("SubtitleText")?.GetComponent<TextMeshProUGUI>();
            endAccentImg = dialogTrans.Find("AccentStrip")?.GetComponent<Image>();

            Transform statsBox = dialogTrans.Find("Stats_Container");
            if (statsBox != null)
            {
                endWaveStatTmp = statsBox.Find("Wave_Row/Value")?.GetComponent<TextMeshProUGUI>();
                endTimeStatTmp = statsBox.Find("Time_Row/Value")?.GetComponent<TextMeshProUGUI>();
                endKillsStatTmp = statsBox.Find("Kills_Row/Value")?.GetComponent<TextMeshProUGUI>();
                endGemsStatTmp = statsBox.Find("Gems_Row/Value")?.GetComponent<TextMeshProUGUI>();
                endWeaponsStatTmp = statsBox.Find("Weapons_Row/Value")?.GetComponent<TextMeshProUGUI>();
            }

            Transform btnBox = dialogTrans.Find("Buttons_Container");
            if (btnBox != null)
            {
                endRestartBtn = btnBox.Find("RestartButton")?.GetComponent<Button>();
                endEndlessBtn = btnBox.Find("EndlessButton")?.GetComponent<Button>();
                endMenuBtn = btnBox.Find("MenuButton")?.GetComponent<Button>();
            }
        }

        // 7. TỰ ĐỘNG GÁN VÀO CÁC Ô INSPECTOR (SERIALIZED PROPERTIES)
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

        // D. Gán vào ArenaShopSystem
        ArenaShopSystem shopSystem = Object.FindAnyObjectByType<ArenaShopSystem>();
        if (shopSystem == null && arenaManager != null)
        {
            shopSystem = arenaManager.GetComponent<ArenaShopSystem>();
            if (shopSystem == null) shopSystem = arenaManager.gameObject.AddComponent<ArenaShopSystem>();
        }

        if (shopSystem != null)
        {
            SerializedObject soShop = new SerializedObject(shopSystem);
            soShop.Update();
            soShop.FindProperty("shopPanel").objectReferenceValue = shopGo;
            soShop.FindProperty("waveTitleText").objectReferenceValue = shopWaveTitleTmp;
            soShop.FindProperty("materialsText").objectReferenceValue = shopMaterialsTmp;
            soShop.FindProperty("rerollButton").objectReferenceValue = rerollBtn;
            soShop.FindProperty("rerollCostText").objectReferenceValue = rerollCostTmp;
            soShop.FindProperty("nextWaveButton").objectReferenceValue = nextWaveBtn;
            soShop.FindProperty("nextWaveButtonText").objectReferenceValue = nextWaveBtnTmp;

            SerializedProperty shopCardsProp = soShop.FindProperty("shopCardsUI");
            if (shopCardsProp != null)
            {
                shopCardsProp.arraySize = shopCardsList.Count;
                for (int i = 0; i < shopCardsList.Count; i++)
                {
                    SerializedProperty elem = shopCardsProp.GetArrayElementAtIndex(i);
                    elem.FindPropertyRelative("cardRoot").objectReferenceValue = shopCardsList[i].cardRoot;
                    elem.FindPropertyRelative("nameText").objectReferenceValue = shopCardsList[i].nameText;
                    elem.FindPropertyRelative("descText").objectReferenceValue = shopCardsList[i].descText;
                    elem.FindPropertyRelative("priceText").objectReferenceValue = shopCardsList[i].priceText;
                    elem.FindPropertyRelative("borderOrIcon").objectReferenceValue = shopCardsList[i].borderOrIcon;
                    elem.FindPropertyRelative("buyButton").objectReferenceValue = shopCardsList[i].buyButton;
                    elem.FindPropertyRelative("buyButtonText").objectReferenceValue = shopCardsList[i].buyButtonText;
                }
            }

            // Gán các file WeaponData từ Assets/Weapon
            SerializedProperty weaponsProp = soShop.FindProperty("availableWeapons");
            if (weaponsProp != null)
            {
                string[] weaponGuids = AssetDatabase.FindAssets("t:WeaponData", new[] { "Assets/Weapon" });
                if (weaponGuids != null && weaponGuids.Length > 0)
                {
                    weaponsProp.arraySize = weaponGuids.Length;
                    for (int i = 0; i < weaponGuids.Length; i++)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(weaponGuids[i]);
                        WeaponData wd = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
                        weaponsProp.GetArrayElementAtIndex(i).objectReferenceValue = wd;
                    }
                }
            }

            soShop.ApplyModifiedProperties();
        }

        // E. Gán vào ArenaGameOverSystem
        ArenaGameOverSystem gameOverSystem = Object.FindAnyObjectByType<ArenaGameOverSystem>();
        if (gameOverSystem == null && arenaManager != null)
        {
            gameOverSystem = arenaManager.GetComponent<ArenaGameOverSystem>();
            if (gameOverSystem == null) gameOverSystem = arenaManager.gameObject.AddComponent<ArenaGameOverSystem>();
        }

        if (gameOverSystem != null)
        {
            SerializedObject soGameOver = new SerializedObject(gameOverSystem);
            soGameOver.Update();
            soGameOver.FindProperty("endGamePanel").objectReferenceValue = endGameGo;
            soGameOver.FindProperty("titleText").objectReferenceValue = endTitleTmp;
            soGameOver.FindProperty("subtitleText").objectReferenceValue = endSubtitleTmp;
            soGameOver.FindProperty("accentStrip").objectReferenceValue = endAccentImg;

            soGameOver.FindProperty("waveStatText").objectReferenceValue = endWaveStatTmp;
            soGameOver.FindProperty("timeStatText").objectReferenceValue = endTimeStatTmp;
            soGameOver.FindProperty("killsStatText").objectReferenceValue = endKillsStatTmp;
            soGameOver.FindProperty("gemsStatText").objectReferenceValue = endGemsStatTmp;
            soGameOver.FindProperty("weaponsStatText").objectReferenceValue = endWeaponsStatTmp;

            soGameOver.FindProperty("restartButton").objectReferenceValue = endRestartBtn;
            soGameOver.FindProperty("menuButton").objectReferenceValue = endMenuBtn;
            soGameOver.FindProperty("endlessButton").objectReferenceValue = endEndlessBtn;

            soGameOver.ApplyModifiedProperties();
        }

        // Đánh dấu Scene thay đổi và Lưu lại
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        if (!silent)
        {
            EditorUtility.DisplayDialog("Tạo Canvas Thành Công!",
                "Đã tự động tạo và gán 100% Canvas UI cho chế độ Đấu trường Sinh Tồn (Brotato):\n\n" +
                "✔ Top-Left: Thanh Máu (HP Bar + Text), Thanh Kinh Nghiệm (XP Bar + Text), Cấp độ (Level)\n" +
                "✔ Top-Center: Đợt Sóng (Wave) & Đồng hồ đếm ngược (Timer)\n" +
                "✔ Top-Right: Số Ngọc/Vật liệu nhặt được (Materials)\n" +
                "✔ Center Banner: Thông báo hoàn thành Wave\n" +
                "✔ Pop-up Level Up: Bảng 3 Thẻ Bài Nâng Cấp Kỹ Năng đẹp mắt (Tự động mở khi lên cấp)\n" +
                "✔ Cửa Hàng Giữa Các Hiệp (Shop Panel): 4 Thẻ mua súng mới & nâng cấp chỉ số, Nút Reroll và Next Wave!\n" +
                "✔ Bảng Thắng / Thua & Tổng Kết (EndGame Panel): Thống kê Wave, Thời gian, Kills, Gems, Súng & Nút Chơi Lại / Vô Tận / Menu!\n" +
                "✔ Đã gán kết nối thẳng vào ArenaManager, BrotatoLevelSystem, PlayerHealth, ArenaShopSystem và ArenaGameOverSystem!",
                "Tuyệt Vời!");
        }
        else
        {
            Debug.Log("<color=#00FFAA>[ArenaCanvasSetupTool] Đã tự động tạo và gán 100% Canvas UI (HUD + Level Up + Shop_Panel + EndGame_Panel)!</color>");
        }
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

    private static ArenaShopSystem.ShopCardUI CreateShopCardElement(Transform container, int index)
    {
        GameObject cardGo = new GameObject($"ShopCard_{index}", typeof(RectTransform), typeof(Image), typeof(Outline));
        cardGo.transform.SetParent(container, false);
        RectTransform rt = cardGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(250, 400);

        Image bgImg = cardGo.GetComponent<Image>();
        bgImg.color = new Color(0.11f, 0.14f, 0.20f, 0.98f);

        Outline outline = cardGo.GetComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        // Header / Accent strip
        GameObject headerGo = new GameObject("AccentStrip", typeof(RectTransform), typeof(Image));
        headerGo.transform.SetParent(cardGo.transform, false);
        RectTransform hRt = headerGo.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0, 1);
        hRt.anchorMax = new Vector2(1, 1);
        hRt.pivot = new Vector2(0.5f, 1);
        hRt.anchoredPosition = Vector2.zero;
        hRt.sizeDelta = new Vector2(0, 6);
        Image accentImg = headerGo.GetComponent<Image>();
        accentImg.color = new Color(1f, 0.6f, 0.2f, 1f);

        // Tên vật phẩm
        TextMeshProUGUI nameTmp = GetOrCreateTMP(cardGo.transform, "NameText", "[GUN] SHOTGUN", 20, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(0, 140), new Vector2(230, 48));

        // Mô tả vật phẩm
        TextMeshProUGUI descTmp = GetOrCreateTMP(cardGo.transform, "DescText", "Adds 1 orbiting gun that auto-aims and fires!", 15, FontStyles.Normal, new Color(0.85f, 0.9f, 0.95f), TextAlignmentOptions.Center, new Vector2(0, 40), new Vector2(230, 120));
        descTmp.enableWordWrapping = true;

        // Giá tiền
        TextMeshProUGUI priceTmp = GetOrCreateTMP(cardGo.transform, "PriceText", "65 GEMS", 22, FontStyles.Bold, new Color(0.2f, 1f, 0.55f), TextAlignmentOptions.Center, new Vector2(0, -60), new Vector2(220, 36));

        // Nút Mua
        GameObject btnGo = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(cardGo.transform, false);
        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0, 22);
        btnRt.sizeDelta = new Vector2(210, 50);

        Image btnImg = btnGo.GetComponent<Image>();
        btnImg.color = new Color(0.18f, 0.65f, 0.35f, 1f); // Nút màu xanh lá

        Button btn = btnGo.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.25f, 0.8f, 0.45f, 1f);
        cb.pressedColor = new Color(0.12f, 0.5f, 0.25f, 1f);
        btn.colors = cb;

        TextMeshProUGUI btnTextTmp = GetOrCreateTMP(btnGo.transform, "BuyButtonText", "BUY", 19, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(190, 44));

        ArenaShopSystem.ShopCardUI shopCard = new ArenaShopSystem.ShopCardUI();
        shopCard.cardRoot = cardGo;
        shopCard.nameText = nameTmp;
        shopCard.descText = descTmp;
        shopCard.priceText = priceTmp;
        shopCard.borderOrIcon = accentImg;
        shopCard.buyButton = btn;
        shopCard.buyButtonText = btnTextTmp;

        return shopCard;
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

    private static TextMeshProUGUI CreateStatRow(Transform container, string rowName, string labelText, string defaultValue, Color valColor, Vector2 pos)
    {
        GameObject rowGo = new GameObject(rowName, typeof(RectTransform));
        rowGo.transform.SetParent(container, false);
        RectTransform rt = rowGo.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(560, 36);

        // Label bên trái
        GetOrCreateTMP(rowGo.transform, "Label", labelText, 17, FontStyles.Bold, new Color(0.80f, 0.86f, 0.92f), TextAlignmentOptions.Left, new Vector2(-135, 0), new Vector2(270, 34));

        // Value bên phải
        TextMeshProUGUI val = GetOrCreateTMP(rowGo.transform, "Value", defaultValue, 18, FontStyles.Bold, valColor, TextAlignmentOptions.Right, new Vector2(135, 0), new Vector2(270, 34));

        return val;
    }

    private static Button CreateActionButton(Transform parent, string name, string labelText, Color btnColor, Vector2 size)
    {
        GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        Image img = btnGo.GetComponent<Image>();
        img.color = btnColor;

        Button btn = btnGo.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(Mathf.Min(1f, btnColor.r * 1.15f), Mathf.Min(1f, btnColor.g * 1.15f), Mathf.Min(1f, btnColor.b * 1.15f), 1f);
        cb.pressedColor = new Color(btnColor.r * 0.85f, btnColor.g * 0.85f, btnColor.b * 0.85f, 1f);
        btn.colors = cb;

        GetOrCreateTMP(btnGo.transform, "BtnText", labelText, 19, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, size);

        return btn;
    }
}
#endif
