using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

public static class GameOverUIBuilder
{
    [MenuItem("Tools/Build Game Over UI (Refined Layout)")]
    public static void BuildGameOverUI()
    {
        Canvas canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        if (canvas == null)
        {
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c.gameObject.name == "Canvas" && c.transform.parent == null)
                {
                    canvas = c;
                    break;
                }
            }
            if (canvas == null && allCanvases.Length > 0) canvas = allCanvases[0];
        }

        if (canvas == null)
        {
            Debug.LogError("[GameOverUIBuilder] Không tìm thấy Canvas chính trong Scene!");
            return;
        }

        // Xóa bất kỳ GameOverPanel cũ nào tồn tại ở root scene
        GameObject rootGoPanel = GameObject.Find("GameOverPanel");
        while (rootGoPanel != null)
        {
            Object.DestroyImmediate(rootGoPanel);
            rootGoPanel = GameObject.Find("GameOverPanel");
        }

        // Xóa GameOverPanel cũ trong Canvas
        Transform oldPanelInCanvas = canvas.transform.Find("GameOverPanel");
        if (oldPanelInCanvas != null)
        {
            Object.DestroyImmediate(oldPanelInCanvas.gameObject);
        }

        // Tải các Asset cần thiết
        Sprite metalFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/mainpage/Rusty Pixel Metal Game UI Frame.png");
        Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI check di duc/button/button 256x64/button.png");
        Sprite iconPlay = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI check di duc/button/round button 128x128/play normal.png");
        Sprite iconReturn = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI check di duc/button/round button 128x128/return normal.png");
        Sprite iconExit = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI check di duc/button/round button 128x128/exit normal.png");

        // Sử dụng LiberationSans SDF cho độ sắc nét và dễ đọc tuyệt đối
        TMP_FontAsset fontReadable = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (fontReadable == null) fontReadable = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/LiberationSans SDF.asset");
        TMP_FontAsset fontTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Kenney Future SDF.asset");
        if (fontTitle == null) fontTitle = fontReadable;
        if (fontReadable == null) fontReadable = fontTitle;

        // 1. Panel bao phủ toàn màn hình (Overlay tối mờ 70%)
        GameObject panelGo = CreateUIObject("GameOverPanel", canvas.transform, Vector2.zero, Vector2.one);
        Image bgImg = panelGo.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.03f, 0.70f);

        // 2. Khung Kim Loại Trung Tâm (Rusty Metal Frame: 1360 x 820)
        GameObject frameGo = CreateUIObject("RustyMetalFrame", panelGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        RectTransform frameRt = frameGo.GetComponent<RectTransform>();
        frameRt.sizeDelta = new Vector2(1360, 820);
        frameRt.anchoredPosition = Vector2.zero;

        Image frameImg = frameGo.AddComponent<Image>();
        frameImg.sprite = metalFrame;
        frameImg.color = Color.white;

        // Nút Exit nhỏ góc trên phải
        if (iconExit != null)
        {
            GameObject exitGo = CreateUIObject("Btn_ExitTop", frameGo.transform, new Vector2(1f, 1f), new Vector2(1f, 1f));
            RectTransform exitRt = exitGo.GetComponent<RectTransform>();
            exitRt.sizeDelta = new Vector2(40, 40);
            exitRt.anchoredPosition = new Vector2(-75, -75);
            Image exitImg = exitGo.AddComponent<Image>();
            exitImg.sprite = iconExit;
            exitImg.preserveAspect = true;
        }

        // 3. Header Tiêu đề (Căn Giữa, Nổi Bật, Tông Màu Hậu Tận Thế)
        GameObject titleGo = CreateText(frameGo.transform, "TitleText", "SIGNAL LOST // VEHICLE DESTROYED", fontReadable, 34, new Color(1f, 0.24f, 0.24f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetAnchors(titleGo, 0.08f, 0.85f, 0.92f, 0.92f);

        GameObject subTitleGo = CreateText(frameGo.transform, "SubTitleText", "TACTICAL CASUALTY ASSESSMENT & SALVAGE MANIFEST", fontReadable, 16, new Color(0.78f, 0.75f, 0.70f), TextAlignmentOptions.Center);
        SetAnchors(subTitleGo, 0.08f, 0.80f, 0.92f, 0.85f);

        // 4. Cột Trái: COMBAT TELEMETRY
        GameObject col1 = CreateUIObject("LeftCol_Telemetry", frameGo.transform, new Vector2(0.075f, 0.17f), new Vector2(0.485f, 0.78f));
        Image col1Img = col1.AddComponent<Image>();
        col1Img.color = new Color(0.07f, 0.06f, 0.09f, 0.88f);

        GameObject tHeader = CreateText(col1.transform, "TelemetryHeader", "[ COMBAT TELEMETRY ]", fontReadable, 20, new Color(1f, 0.78f, 0.25f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetAnchors(tHeader, 0.05f, 0.88f, 0.95f, 0.97f);

        // Các hàng thông số dạng Card: Nhãn trái, Giá trị phải
        GameObject secText = CreateDataRow(col1.transform, "Row_Sector", 0.67f, 0.84f, "OPERATIONS ROUTE", "Sector 01", new Color(0.40f, 0.85f, 1f), fontReadable, 15, 20);
        GameObject distText = CreateDataRow(col1.transform, "Row_Distance", 0.48f, 0.65f, "DISTANCE TRAVELED", "145 m", Color.white, fontReadable, 15, 22);
        GameObject killText = CreateDataRow(col1.transform, "Row_Kills", 0.29f, 0.46f, "MUTANTS ELIMINATED", "26 Mutants", new Color(1f, 0.42f, 0.42f), fontReadable, 15, 22);
        GameObject speedText = CreateDataRow(col1.transform, "Row_Speed", 0.10f, 0.27f, "IMPACT VELOCITY", "84 Km/h", new Color(0.9f, 0.9f, 0.9f), fontReadable, 15, 20);

        secText.name = "SectorText";
        distText.name = "DistanceText";
        killText.name = "KillText";
        speedText.name = "SpeedText";

        // 5. Cột Phải: SALVAGE BOUNTY
        GameObject col2 = CreateUIObject("RightCol_Salvage", frameGo.transform, new Vector2(0.515f, 0.17f), new Vector2(0.925f, 0.78f));
        Image col2Img = col2.AddComponent<Image>();
        col2Img.color = new Color(0.07f, 0.06f, 0.09f, 0.88f);

        GameObject sHeader = CreateText(col2.transform, "SalvageHeader", "[ SALVAGE BOUNTY ]", fontReadable, 20, new Color(1f, 0.78f, 0.25f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetAnchors(sHeader, 0.05f, 0.88f, 0.95f, 0.97f);

        GameObject distScrapGo = CreateDataRow(col2.transform, "Row_DistScrap", 0.72f, 0.86f, "Distance Scrap ($1/m)", "+$145 Cash", new Color(0.45f, 1f, 0.60f), fontReadable, 15, 20);
        GameObject mutantScrapGo = CreateDataRow(col2.transform, "Row_KillScrap", 0.56f, 0.70f, "Mutant Bounty ($10/kill)", "+$260 Cash", new Color(0.45f, 1f, 0.60f), fontReadable, 15, 20);
        distScrapGo.name = "DistanceScrapText";
        mutantScrapGo.name = "MutantScrapText";

        // Card Nổi Bật: TOTAL SALVAGE CASH EARNED (Hero Highlight Box)
        GameObject bountyBox = CreateUIObject("BountyHighlightBox", col2.transform, new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.52f));
        Image bBoxImg = bountyBox.AddComponent<Image>();
        bBoxImg.color = new Color(0.20f, 0.14f, 0.05f, 0.95f);

        GameObject bLbl = CreateText(bountyBox.transform, "BountyLabel", "TOTAL SALVAGE CASH EARNED", fontReadable, 16, new Color(1f, 0.82f, 0.35f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetAnchors(bLbl, 0.05f, 0.60f, 0.95f, 0.92f);

        GameObject cashVal = CreateText(bountyBox.transform, "CashValueText", "+$405 CASH", fontTitle, 40, new Color(0.30f, 1f, 0.45f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetAnchors(cashVal, 0.05f, 0.08f, 0.95f, 0.62f);

        // Card Ngân Sách: TOTAL BUNKER FUNDS
        GameObject fundBox = CreateUIObject("TotalFundBox", col2.transform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.20f));
        Image fundImg = fundBox.AddComponent<Image>();
        fundImg.color = new Color(0.06f, 0.09f, 0.15f, 0.92f);

        GameObject fundLbl = CreateText(fundBox.transform, "FundLabel", "TOTAL BUNKER FUNDS:", fontReadable, 15, new Color(0.45f, 0.88f, 1f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        SetAnchors(fundLbl, 0.06f, 0f, 0.55f, 1f);

        GameObject fundVal = CreateText(fundBox.transform, "TotalFundsText", "$27,905 CASH", fontReadable, 22, Color.white, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        SetAnchors(fundVal, 0.55f, 0f, 0.94f, 1f);

        // 6. Hai Nút Bấm Phía Dưới (Thiết kế to bản, cân xứng, icon vuông vắn)
        GameObject btnRetry = CreateStyledButton(frameGo.transform, "Btn_Retry", new Vector2(0.10f, 0.045f), new Vector2(0.48f, 0.135f), btnSprite, new Color(0.90f, 0.25f, 0.20f), iconPlay, "RETRY SECTOR [R]", fontReadable);
        GameObject btnLobby = CreateStyledButton(frameGo.transform, "Btn_Lobby", new Vector2(0.52f, 0.045f), new Vector2(0.90f, 0.135f), btnSprite, new Color(0.30f, 0.42f, 0.55f), iconReturn, "MISSION LOBBY [ESC]", fontReadable);

        // 7. Kết nối Serialized Fields vào DistanceTrackerUI
        DistanceTrackerUI dt = Object.FindAnyObjectByType<DistanceTrackerUI>();
        if (dt != null)
        {
            Button bRetryComp = btnRetry.GetComponent<Button>();
            Button bLobbyComp = btnLobby.GetComponent<Button>();

            // Tự động gắn event click
            UnityEventTools.AddPersistentListener(bRetryComp.onClick, dt.RetryLevel);
            UnityEventTools.AddPersistentListener(bLobbyComp.onClick, dt.ReturnToLobby);

            SerializedObject so = new SerializedObject(dt);
            so.FindProperty("gameOverPanel").objectReferenceValue = panelGo;
            so.FindProperty("sectorSummaryText").objectReferenceValue = secText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("finalDistanceText").objectReferenceValue = distText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("killSummaryText").objectReferenceValue = killText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("speedSummaryText").objectReferenceValue = speedText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("distanceScrapText").objectReferenceValue = distScrapGo.GetComponent<TextMeshProUGUI>();
            so.FindProperty("mutantScrapText").objectReferenceValue = mutantScrapGo.GetComponent<TextMeshProUGUI>();
            so.FindProperty("earnedCashText").objectReferenceValue = cashVal.GetComponent<TextMeshProUGUI>();
            so.FindProperty("totalFundsText").objectReferenceValue = fundVal.GetComponent<TextMeshProUGUI>();
            so.FindProperty("retryButton").objectReferenceValue = bRetryComp;
            so.FindProperty("lobbyButton").objectReferenceValue = bLobbyComp;
            so.ApplyModifiedProperties();
        }

        // BẬT SẴN trong Scene view để kiểm tra
        panelGo.SetActive(true);

        EditorUtility.SetDirty(canvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log("[GameOverUIBuilder] Đã hoàn thành bố cục Game Over UI cao cấp, cân xứng và dễ nhìn!");
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

    private static GameObject CreateText(Transform parent, string name, string text, TMP_FontAsset font, float fontSize, Color color, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
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
        tmp.fontStyle = style;
        tmp.enableWordWrapping = true;
        return go;
    }

    private static GameObject CreateDataRow(Transform parent, string rowName, float minY, float maxY, string label, string defaultValue, Color valColor, TMP_FontAsset font, float labelSize, float valSize)
    {
        GameObject row = CreateUIObject(rowName, parent, new Vector2(0.04f, minY), new Vector2(0.96f, maxY));
        Image rowImg = row.AddComponent<Image>();
        rowImg.color = new Color(0.12f, 0.11f, 0.16f, 0.75f);

        GameObject lbl = CreateText(row.transform, "Label", label, font, labelSize, new Color(0.68f, 0.72f, 0.80f), TextAlignmentOptions.MidlineLeft);
        SetAnchors(lbl, 0.05f, 0f, 0.55f, 1f);

        GameObject val = CreateText(row.transform, "ValueText", defaultValue, font, valSize, valColor, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        SetAnchors(val, 0.52f, 0f, 0.95f, 1f);

        return val;
    }

    private static GameObject CreateStyledButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Sprite bgSprite, Color bgColor, Sprite iconSprite, string text, TMP_FontAsset font)
    {
        GameObject btnGo = CreateUIObject(name, parent, anchorMin, anchorMax);
        Image img = btnGo.AddComponent<Image>();
        if (bgSprite != null) img.sprite = bgSprite;
        img.color = bgColor;

        Button btn = btnGo.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = bgColor * 1.25f;
        cb.pressedColor = bgColor * 0.8f;
        btn.colors = cb;

        // Thêm Icon vuông vắn (không méo hình)
        if (iconSprite != null)
        {
            GameObject iconObj = CreateUIObject("BtnIcon", btnGo.transform, new Vector2(0.05f, 0.15f), new Vector2(0.18f, 0.85f));
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }

        // Chữ trong nút (Cỡ 20 Bold, căn giữa cân đối)
        GameObject textObj = CreateText(btnGo.transform, "BtnText", text, font, 20, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.16f, 0f);
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return btnGo;
    }
}
