using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WarRoomUIBuilder
{
    [MenuItem("Tools/Build Tactical War Room UI")]
    public static void BuildUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found in current scene!");
            return;
        }

        // Tải tài nguyên font chữ & hình ảnh
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/mainpage/Dead Mile_ Sunset Over Ruins.png");
        Sprite woodBoardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/WoodenMissionBoard.png");
        Sprite frameSprite = woodBoardSprite != null ? woodBoardSprite : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/mainpage/Rusty Pixel Metal Game UI Frame.png");
        
        // Font chính: Consolas Bold (Typewriter / Máy đánh chữ hồ sơ quân sự sắc nét)
        TMP_FontAsset fontConsolas = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Consolas-Bold SDF.asset");
        if (fontConsolas == null)
        {
            Font ttf = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Consolas-Bold.ttf");
            if (ttf != null)
            {
                fontConsolas = TMP_FontAsset.CreateFontAsset(ttf);
                if (fontConsolas != null)
                {
                    fontConsolas.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    AssetDatabase.CreateAsset(fontConsolas, "Assets/Fonts/Consolas-Bold SDF.asset");
                    AssetDatabase.SaveAssets();
                }
            }
        }
        if (fontConsolas == null) fontConsolas = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Kenney Future SDF.asset");

        TMP_FontAsset fontHeader = fontConsolas;
        TMP_FontAsset fontBody = fontConsolas;

        // Tối ưu vật liệu SDF để nét chữ đậm đà, không bị mờ
        if (fontHeader != null && fontHeader.material != null)
        {
            fontHeader.material.SetColor("_FaceColor", Color.white);
            fontHeader.material.SetFloat("_FaceDilate", 0.05f);
            fontHeader.material.SetFloat("_Sharpness", 0.75f);
            EditorUtility.SetDirty(fontHeader.material);
        }

        AudioClip lobbyMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/sound eff/lobby music.mp3");

        Sprite preview1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainPalette/HCM/Ho Chi Minh City Sunset Skyline.png");
        Sprite preview2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainPalette/HCM/Ho Chi Minh City Sunset Skyline.png");
        Sprite preview3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainPalette/HCM/hcmnight.png");

        int uiLayer = LayerMask.NameToLayer("UI");

        // Tìm hoặc tạo TacticalWarRoomManager
        GameObject warRoomGo = GameObject.Find("TacticalWarRoomManager");
        if (warRoomGo == null) warRoomGo = new GameObject("TacticalWarRoomManager");

        TacticalWarRoomManager manager = warRoomGo.GetComponent<TacticalWarRoomManager>();
        if (manager == null) manager = warRoomGo.AddComponent<TacticalWarRoomManager>();

        AudioSource musicSource = warRoomGo.GetComponent<AudioSource>();
        if (musicSource == null) musicSource = warRoomGo.AddComponent<AudioSource>();
        musicSource.clip = lobbyMusic;
        musicSource.loop = true;
        musicSource.playOnAwake = true;
        musicSource.volume = 0.65f;

        // Dọn dẹp panel cũ
        Transform existingPanel = canvas.transform.Find("WarRoomPanel");
        if (existingPanel != null) Object.DestroyImmediate(existingPanel.gameObject);

        Transform existingLobbyHUD = canvas.transform.Find("LobbyHUD");
        if (existingLobbyHUD != null) Object.DestroyImmediate(existingLobbyHUD.gameObject);

        // =====================================================================
        // 1. LOBBY HUD (Giao diện sảnh khi đóng War Room)
        // =====================================================================
        GameObject lobbyHUD = CreateUIObject("LobbyHUD", canvas.transform, Vector2.zero, Vector2.one);

        GameObject btnOpenMap = CreateButton(lobbyHUD.transform, "Btn_OpenWarRoom", new Vector2(0.5f, 0.12f), new Vector2(380, 58), new Color(0.92f, 0.45f, 0.1f));
        SetButtonText(btnOpenMap, "TACTICAL WAR MAP", fontHeader, 20, Color.white);

        GameObject btnExitMenu = CreateButton(lobbyHUD.transform, "Btn_ExitToMenu", new Vector2(0.09f, 0.90f), new Vector2(170, 44), new Color(0.20f, 0.22f, 0.26f));
        SetButtonText(btnExitMenu, "MAIN MENU", fontHeader, 14, Color.white);

        // =====================================================================
        // 2. WAR ROOM PANEL (BẢNG GỖ SINH TỒN - WOODEN MISSION BOARD)
        // =====================================================================
        GameObject warPanel = CreateUIObject("WarRoomPanel", canvas.transform, Vector2.zero, Vector2.one);
        Image imgWarBg = warPanel.AddComponent<Image>();
        if (bgSprite != null)
        {
            imgWarBg.sprite = bgSprite;
            imgWarBg.color = new Color(0.12f, 0.13f, 0.16f, 0.98f);
        }
        else
        {
            imgWarBg.color = new Color(0.06f, 0.07f, 0.08f, 0.98f);
        }

        // Khung Bảng Gỗ Viền Sắt Hoen Rỉ (Wooden Mission Board)
        GameObject boardFrame = CreateUIObject("WoodenBoardOverlay", warPanel.transform, Vector2.zero, Vector2.one);
        Image imgFrame = boardFrame.AddComponent<Image>();
        imgFrame.sprite = frameSprite;
        imgFrame.color = Color.white;
        imgFrame.raycastTarget = false;

        // Container nội dung an toàn bên trong lòng bảng gỗ (cách viền 8.5% các cạnh)
        GameObject mainContainer = CreateUIObject("MainSafeContainer", warPanel.transform, new Vector2(0.085f, 0.095f), new Vector2(0.915f, 0.905f));

        // ---------------------------------------------------------------------
        // TOP HEADER BAR (TIÊU ĐỀ BẢNG GỖ TÁC CHIẾN)
        // ---------------------------------------------------------------------
        GameObject topBar = CreateUIObject("TopHeaderBar", mainContainer.transform, new Vector2(0.01f, 0.91f), new Vector2(0.99f, 0.99f));
        
        GameObject titleObj = CreateText(topBar.transform, "TitleText", "BUNKER 04 // TACTICAL BOUNTY BOARD", fontHeader, 24, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.MidlineLeft);
        SetAnchors(titleObj, 0.01f, 0f, 0.65f, 1f);

        GameObject cashObj = CreateText(topBar.transform, "CashTopText", "FUNDS: $25,000", fontHeader, 22, new Color(0.40f, 0.85f, 0.50f), TextAlignmentOptions.MidlineRight);
        SetAnchors(cashObj, 0.65f, 0f, 0.99f, 1f);

        // ---------------------------------------------------------------------
        // CỘT TRÁI: SECTOR MISSIONS (36% chiều rộng - Phong cách thẻ lệnh dán bảng)
        // ---------------------------------------------------------------------
        GameObject leftCol = CreateUIObject("LeftColumn_Sectors", mainContainer.transform, new Vector2(0.01f, 0.10f), new Vector2(0.37f, 0.89f));
        Image imgLeftBg = leftCol.AddComponent<Image>();
        imgLeftBg.color = new Color(0.06f, 0.045f, 0.035f, 0.72f); // Nền mờ tự nhiên lộ vân gỗ
        AddSoftShadow(leftCol, new Vector2(4f, -5f), new Color(0f, 0f, 0f, 0.65f));

        GameObject leftHeader = CreateText(leftCol.transform, "LeftHeader", "OPERATIONS SECTORS", fontHeader, 15, new Color(0.98f, 0.95f, 0.90f), TextAlignmentOptions.MidlineLeft);
        SetAnchors(leftHeader, 0.05f, 0.91f, 0.95f, 0.98f);

        Button[] pinButtons = new Button[3];
        GameObject[] pinLocked = new GameObject[3];
        GameObject[] pinCleared = new GameObject[3];
        Image[] pinGlows = new Image[3];

        string[] secCodes = new string[] { "SECTOR 01", "SECTOR 02", "SECTOR 03" };
        string[] secNames = new string[] { "Highway Outskirts", "Contaminated Zone", "Apex Ground Zero" };
        string[] secThreats = new string[] { "Threat: Low (Tier 1)", "Threat: High (Tier 3)", "Threat: Extreme (Tier 5)" };
        Color[] threatColors = new Color[] { new Color(0.40f, 0.85f, 0.50f), new Color(0.95f, 0.80f, 0.35f), new Color(0.90f, 0.35f, 0.30f) };

        float cardHeight = 0.25f;
        float cardGap = 0.28f;
        float cardStartY = 0.63f;

        for (int i = 0; i < 3; i++)
        {
            float minY = cardStartY - (i * cardGap);
            float maxY = minY + cardHeight;

            GameObject cardObj = CreateUIObject($"SectorCard_{i + 1}", leftCol.transform, new Vector2(0.04f, minY), new Vector2(0.96f, maxY));
            Image imgCard = cardObj.AddComponent<Image>();
            imgCard.color = new Color(0.12f, 0.09f, 0.07f, 0.90f);
            AddSoftShadow(cardObj, new Vector2(3f, -4f), new Color(0f, 0f, 0f, 0.75f));
            pinButtons[i] = cardObj.AddComponent<Button>();

            ColorBlock cb = pinButtons[i].colors;
            cb.normalColor = new Color(0.12f, 0.09f, 0.07f, 0.90f);
            cb.highlightedColor = new Color(0.20f, 0.15f, 0.11f, 1f);
            cb.pressedColor = new Color(0.08f, 0.06f, 0.04f, 1f);
            pinButtons[i].colors = cb;

            // Highlight chọn nhẹ nhàng
            GameObject glowObj = CreateUIObject("SelectionGlow", cardObj.transform, Vector2.zero, Vector2.one);
            Image imgGlow = glowObj.AddComponent<Image>();
            imgGlow.color = new Color(1f, 0.65f, 0.1f, 0.08f);
            imgGlow.raycastTarget = false;
            pinGlows[i] = imgGlow;
            glowObj.SetActive(false); // Ẩn mặc định để tránh chói, chỉ bật AccentBar

            // Dải màu nhấn AccentBar bên trái
            GameObject accentBar = CreateUIObject("AccentBar", cardObj.transform, new Vector2(0f, 0f), new Vector2(0.025f, 1f));
            Image imgAccent = accentBar.AddComponent<Image>();
            imgAccent.color = (i == 0) ? new Color(0.92f, 0.55f, 0.16f) : new Color(0.30f, 0.24f, 0.19f);

            // Số hiệu Sector (Font to, rõ, rực rỡ)
            GameObject numObj = CreateText(cardObj.transform, "Number", $"0{i + 1}", fontHeader, 30, (i == 0) ? new Color(0.95f, 0.60f, 0.20f) : new Color(0.65f, 0.60f, 0.55f), TextAlignmentOptions.Center);
            SetAnchors(numObj, 0.03f, 0.15f, 0.20f, 0.85f);

            // Tên Sector Code (Consolas)
            GameObject codeText = CreateText(cardObj.transform, "Code", secCodes[i], fontHeader, 13, new Color(0.68f, 0.65f, 0.60f), TextAlignmentOptions.MidlineLeft);
            SetAnchors(codeText, 0.22f, 0.65f, 0.68f, 0.93f);

            // Tên Sector chi tiết (Consolas - Rõ ràng, dễ đọc)
            GameObject nameText = CreateText(cardObj.transform, "Name", secNames[i], fontHeader, 18, new Color(0.98f, 0.95f, 0.90f), TextAlignmentOptions.MidlineLeft);
            SetAnchors(nameText, 0.22f, 0.35f, 0.70f, 0.65f);

            // Mức độ nguy hiểm (Consolas - Nâng lên 16pt)
            GameObject threatText = CreateText(cardObj.transform, "Threat", secThreats[i], fontHeader, 16, threatColors[i], TextAlignmentOptions.MidlineLeft);
            SetAnchors(threatText, 0.22f, 0.08f, 0.70f, 0.35f);

            // Lock Badge (Consolas)
            GameObject lockBadge = CreateText(cardObj.transform, "LockBadge", "[ LOCKED ]", fontHeader, 15, new Color(0.90f, 0.35f, 0.30f), TextAlignmentOptions.MidlineRight);
            SetAnchors(lockBadge, 0.68f, 0.35f, 0.96f, 0.65f);
            pinLocked[i] = lockBadge;
            lockBadge.SetActive(i > 0);

            // Cleared Badge (Consolas)
            GameObject clearBadge = CreateText(cardObj.transform, "ClearedBadge", "[ SECURED ]", fontHeader, 15, new Color(0.40f, 0.85f, 0.50f), TextAlignmentOptions.MidlineRight);
            SetAnchors(clearBadge, 0.68f, 0.35f, 0.96f, 0.65f);
            pinCleared[i] = clearBadge;
            clearBadge.SetActive(false);
        }

        // ---------------------------------------------------------------------
        // CỘT PHẢI: MISSION DOSSIER (60% chiều rộng - Sắp xếp theo khối mạch lạc)
        // ---------------------------------------------------------------------
        GameObject rightCol = CreateUIObject("RightColumn_Dossier", mainContainer.transform, new Vector2(0.39f, 0.10f), new Vector2(0.99f, 0.89f));
        Image imgRightBg = rightCol.AddComponent<Image>();
        imgRightBg.color = new Color(0.06f, 0.045f, 0.035f, 0.72f);
        AddSoftShadow(rightCol, new Vector2(4f, -5f), new Color(0f, 0f, 0f, 0.65f));

        GameObject dosHeader = CreateText(rightCol.transform, "DossierHeader", "MISSION DOSSIER // BRIEFING", fontHeader, 16, new Color(0.98f, 0.95f, 0.90f), TextAlignmentOptions.MidlineLeft);
        SetAnchors(dosHeader, 0.03f, 0.92f, 0.97f, 0.98f);

        // KHỐI 1: KHUNG ẢNH TRINH SÁT SECTOR (Panorama)
        GameObject previewFrame = CreateUIObject("PreviewFrame", rightCol.transform, new Vector2(0.03f, 0.64f), new Vector2(0.97f, 0.91f));
        Image imgPrevFrame = previewFrame.AddComponent<Image>();
        imgPrevFrame.color = new Color(0.12f, 0.09f, 0.06f, 1f);
        AddSoftShadow(previewFrame, new Vector2(2f, -3f), new Color(0f, 0f, 0f, 0.60f));

        GameObject previewImgObj = CreateUIObject("SectorPreviewImage", previewFrame.transform, new Vector2(0.008f, 0.02f), new Vector2(0.992f, 0.98f));
        Image imgPreview = previewImgObj.AddComponent<Image>();
        imgPreview.sprite = preview1;
        imgPreview.preserveAspect = false;

        // KHỐI 2: THÔNG TIN CHIẾN ĐỊA & ĐỐI PHƯƠNG
        GameObject infoCard = CreateUIObject("TacticalInfoCard", rightCol.transform, new Vector2(0.03f, 0.40f), new Vector2(0.97f, 0.62f));
        Image imgInfoCard = infoCard.AddComponent<Image>();
        imgInfoCard.color = new Color(0.10f, 0.08f, 0.06f, 0.88f);
        AddSoftShadow(infoCard, new Vector2(2f, -3f), new Color(0f, 0f, 0f, 0.60f));

        // Tiêu đề Sector (Consolas - Vàng ấm 20pt)
        GameObject secTitleObj = CreateText(infoCard.transform, "SectorTitle", "SECTOR 01: HIGHWAY OUTSKIRTS", fontHeader, 20, new Color(0.95f, 0.80f, 0.35f), TextAlignmentOptions.MidlineLeft);
        SetAnchors(secTitleObj, 0.03f, 0.72f, 0.97f, 0.96f);

        // Mức độ nguy hiểm & Cự ly (Consolas - Nâng lên 16pt)
        GameObject threatTxtObj = CreateText(infoCard.transform, "ThreatText", "Threat: Low (Tier 1)", fontHeader, 16, new Color(0.40f, 0.85f, 0.50f), TextAlignmentOptions.MidlineLeft);
        SetAnchors(threatTxtObj, 0.03f, 0.46f, 0.50f, 0.72f);

        GameObject distTxtObj = CreateText(infoCard.transform, "DistText", "Distance: 160 Meters", fontHeader, 16, new Color(0.98f, 0.95f, 0.90f), TextAlignmentOptions.MidlineRight);
        SetAnchors(distTxtObj, 0.50f, 0.46f, 0.97f, 0.72f);

        // Tình báo quái vật (Nâng lên 16pt)
        GameObject enemyIntel = CreateText(infoCard.transform, "EnemyIntelText", "Mutants: Basic Walkers, Rapid Jumpers, Heavy Chargers", fontHeader, 16, new Color(0.85f, 0.80f, 0.72f), TextAlignmentOptions.MidlineLeft);
        SetAnchors(enemyIntel, 0.03f, 0.24f, 0.97f, 0.46f);

        // Tình báo Trùm (Nâng lên 16pt)
        GameObject bossIntel = CreateText(infoCard.transform, "BossIntelText", "Boss: Outskirts Mutated Horde", fontHeader, 16, new Color(0.90f, 0.45f, 0.40f), TextAlignmentOptions.MidlineLeft);
        SetAnchors(bossIntel, 0.03f, 0.02f, 0.97f, 0.24f);

        // KHỐI 3: ĐIỆN ĐÀM BỘ CHỈ HUY (Radio Dispatch Terminal)
        GameObject radioBox = CreateUIObject("RadioDispatchBox", rightCol.transform, new Vector2(0.03f, 0.17f), new Vector2(0.97f, 0.38f));
        Image imgRadio = radioBox.AddComponent<Image>();
        imgRadio.color = new Color(0.04f, 0.045f, 0.05f, 0.92f);
        AddSoftShadow(radioBox, new Vector2(2f, -3f), new Color(0f, 0f, 0f, 0.60f));

        GameObject radioHeader = CreateText(radioBox.transform, "RadioHeader", "[ HQ DISPATCH // ENCRYPTED TRANSCRIPT ]", fontHeader, 14, new Color(0.55f, 0.72f, 0.80f), TextAlignmentOptions.MidlineLeft);
        SetAnchors(radioHeader, 0.03f, 0.78f, 0.97f, 0.96f);

        // Chữ điện đàm: Consolas xanh bạc hà dịu mắt (Nâng lên 16pt)
        GameObject radioText = CreateText(radioBox.transform, "RadioTranscript", "HQ to Survivor: Outskirts highway is swarming with infected. Punch through barricades, dismount and secure Checkpoint Alpha!", fontHeader, 16, new Color(0.80f, 0.94f, 0.86f), TextAlignmentOptions.TopLeft);
        SetAnchors(radioText, 0.03f, 0.06f, 0.97f, 0.76f);

        // KHỐI 4: MỤC TIÊU & PHẦN THƯỞNG (Nằm dưới cùng cột phải - Nâng lên 16pt)
        GameObject objText = CreateText(rightCol.transform, "ObjectivesText", "Tactical Objectives:\n- 1. Ram through barricades to reach 160m\n- 2. Dismount vehicle and eliminate 10 mutants on foot", fontHeader, 16, new Color(0.88f, 0.85f, 0.80f), TextAlignmentOptions.TopLeft);
        SetAnchors(objText, 0.03f, 0.06f, 0.97f, 0.16f);

        GameObject rewText = CreateText(rightCol.transform, "RewardsText", "Rewards: +1,500 Cash  |  Armored Truck Unlocked", fontHeader, 16, new Color(0.95f, 0.80f, 0.35f), TextAlignmentOptions.MidlineLeft);
        SetAnchors(rewText, 0.03f, 0.005f, 0.97f, 0.06f);

        // ---------------------------------------------------------------------
        // BOTTOM ACTION BAR
        // ---------------------------------------------------------------------
        GameObject btnClose = CreateButton(mainContainer.transform, "Btn_CloseWarRoom", new Vector2(0.18f, 0.045f), new Vector2(240, 48), new Color(0.18f, 0.19f, 0.21f));
        AddSoftShadow(btnClose, new Vector2(2f, -3f), new Color(0f, 0f, 0f, 0.65f));
        SetButtonText(btnClose, "RETURN TO LOBBY", fontHeader, 15, Color.white);

        GameObject btnDeploy = CreateButton(mainContainer.transform, "Btn_Deploy", new Vector2(0.69f, 0.045f), new Vector2(380, 52), new Color(0.85f, 0.38f, 0.10f));
        AddSoftShadow(btnDeploy, new Vector2(3f, -4f), new Color(0f, 0f, 0f, 0.75f));
        GameObject deployTxt = SetButtonText(btnDeploy, "DEPLOY VEHICLE", fontHeader, 20, Color.white);

        // =====================================================================
        // KẾT NỐI SERIALIZED FIELDS VÀO TACTICALWARROOMMANAGER
        // =====================================================================
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("warRoomPanel").objectReferenceValue = warPanel;
        so.FindProperty("lobbyDefaultHUD").objectReferenceValue = lobbyHUD;

        so.FindProperty("sectorTitleText").objectReferenceValue = secTitleObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("threatLevelText").objectReferenceValue = threatTxtObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("distanceText").objectReferenceValue = distTxtObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("enemyIntelText").objectReferenceValue = enemyIntel.GetComponent<TextMeshProUGUI>();
        so.FindProperty("bossIntelText").objectReferenceValue = bossIntel.GetComponent<TextMeshProUGUI>();
        so.FindProperty("radioTranscriptText").objectReferenceValue = radioText.GetComponent<TextMeshProUGUI>();
        so.FindProperty("objectivesText").objectReferenceValue = objText.GetComponent<TextMeshProUGUI>();
        so.FindProperty("rewardsText").objectReferenceValue = rewText.GetComponent<TextMeshProUGUI>();
        so.FindProperty("sectorPreviewImage").objectReferenceValue = imgPreview;

        so.FindProperty("deployButton").objectReferenceValue = btnDeploy.GetComponent<Button>();
        so.FindProperty("deployButtonText").objectReferenceValue = deployTxt.GetComponent<TextMeshProUGUI>();
        so.FindProperty("closeButton").objectReferenceValue = btnClose.GetComponent<Button>();
        so.FindProperty("openWarRoomButton").objectReferenceValue = btnOpenMap.GetComponent<Button>();
        so.FindProperty("returnToMenuButton").objectReferenceValue = btnExitMenu.GetComponent<Button>();

        SerializedProperty propPins = so.FindProperty("mapPinButtons");
        propPins.arraySize = 3;
        for (int i = 0; i < 3; i++) propPins.GetArrayElementAtIndex(i).objectReferenceValue = pinButtons[i];

        SerializedProperty propLocked = so.FindProperty("pinLockedIcons");
        propLocked.arraySize = 3;
        for (int i = 0; i < 3; i++) propLocked.GetArrayElementAtIndex(i).objectReferenceValue = pinLocked[i];

        SerializedProperty propCleared = so.FindProperty("pinClearedIcons");
        propCleared.arraySize = 3;
        for (int i = 0; i < 3; i++) propCleared.GetArrayElementAtIndex(i).objectReferenceValue = pinCleared[i];

        SerializedProperty propGlows = so.FindProperty("pinSelectionGlows");
        propGlows.arraySize = 3;
        for (int i = 0; i < 3; i++) propGlows.GetArrayElementAtIndex(i).objectReferenceValue = pinGlows[i];

        so.FindProperty("musicSource").objectReferenceValue = musicSource;
        so.FindProperty("lobbyMusicClip").objectReferenceValue = lobbyMusic;
        so.FindProperty("cashDisplayTopText").objectReferenceValue = cashObj.GetComponent<TextMeshProUGUI>();

        so.ApplyModifiedProperties();

        manager.SetSectorPreviewSprites(preview1, preview2, preview3);

        SetLayerRecursively(canvas.gameObject, uiLayer);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(canvas.gameObject.scene);

        Debug.Log("Tactical War Room UI successfully updated with highly legible LiberationSans & Kenney Future typography!");
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
    }

    private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    private static void SetAnchors(GameObject go, float minX, float minY, float maxX, float maxY)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject CreateText(Transform parent, string name, string text, TMP_FontAsset font, float fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        if (font != null) tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, Vector2 normalizedPos, Vector2 size, Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = normalizedPos;
        rt.anchorMax = normalizedPos;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.color = bgColor;

        Button btn = go.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = bgColor * 1.25f;
        cb.pressedColor = bgColor * 0.8f;
        btn.colors = cb;

        return go;
    }

    private static GameObject SetButtonText(GameObject btnGo, string text, TMP_FontAsset font, float fontSize, Color color)
    {
        GameObject textObj = CreateText(btnGo.transform, "BtnText", text, font, fontSize, color, TextAlignmentOptions.Center);
        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return textObj;
    }

    private static void AddSoftShadow(GameObject go, Vector2 dist, Color color)
    {
        Shadow shadow = go.GetComponent<Shadow>();
        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectDistance = dist;
        shadow.effectColor = color;
    }

    [InitializeOnLoadMethod]
    private static void AutoRunOnCompile()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool("WarRoomUI_Consolas_V1", false)) return;
            SessionState.SetBool("WarRoomUI_Consolas_V1", true);

            BuildUI();

            var cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                int width = 1280;
                int height = 720;
                var rt = new RenderTexture(width, height, 24);
                var prevRt = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                cam.targetTexture = prevRt;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);
                byte[] bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);
                string path = @"C:\Users\PC\.gemini\antigravity\brain\ee16f53b-2bef-4dcf-a2e9-109b3992d251\wooden_board_showcase.png";
                System.IO.File.WriteAllBytes(path, bytes);
                Debug.Log("[WarRoomUIBuilder] Auto-baked Wooden Board UI with Consolas Bold and exported screenshot to: " + path);
            }
        };
    }
}
