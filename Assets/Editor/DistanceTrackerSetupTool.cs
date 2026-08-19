#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

public class DistanceTrackerSetupTool : EditorWindow
{
    [MenuItem("Tools/Tự Động Tạo Giao Diện Distance Tracker (1-Click)")]
    public static void SetupDistanceTrackerUI()
    {
        // 1. Tìm hoặc tạo Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // 2. Tìm hoặc tạo HUD_Panel
        Transform hudTransform = canvas.transform.Find("HUD_Tracker");
        if (hudTransform == null)
        {
            GameObject hudGo = new GameObject("HUD_Tracker");
            hudGo.transform.SetParent(canvas.transform, false);
            RectTransform rt = hudGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -20);
            rt.sizeDelta = new Vector2(0, 100);
            hudTransform = hudGo.transform;
        }

        // 3. Tạo DistanceText (Hiển thị Mét ở giữa trên cùng)
        TextMeshProUGUI distanceTmp = null;
        Transform distTrans = hudTransform.Find("DistanceText");
        if (distTrans == null)
        {
            GameObject distGo = new GameObject("DistanceText");
            distGo.transform.SetParent(hudTransform, false);
            distanceTmp = distGo.AddComponent<TextMeshProUGUI>();
            distanceTmp.text = "0 m";
            distanceTmp.fontSize = 42;
            distanceTmp.fontStyle = FontStyles.Bold;
            distanceTmp.alignment = TextAlignmentOptions.Center;
            distanceTmp.color = new Color(1f, 0.85f, 0.2f); // Màu vàng gold

            RectTransform rt = distGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -10);
            rt.sizeDelta = new Vector2(300, 60);
        }
        else
        {
            distanceTmp = distTrans.GetComponent<TextMeshProUGUI>();
        }

        // 4. Tạo SpeedText (Hiển thị Tốc độ km/h ở góc trái)
        TextMeshProUGUI speedTmp = null;
        Transform speedTrans = hudTransform.Find("SpeedText");
        if (speedTrans == null)
        {
            GameObject speedGo = new GameObject("SpeedText");
            speedGo.transform.SetParent(hudTransform, false);
            speedTmp = speedGo.AddComponent<TextMeshProUGUI>();
            speedTmp.text = "0 km/h";
            speedTmp.fontSize = 26;
            speedTmp.fontStyle = FontStyles.Bold;
            speedTmp.alignment = TextAlignmentOptions.Left;
            speedTmp.color = Color.cyan;

            RectTransform rt = speedGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(40, -15);
            rt.sizeDelta = new Vector2(200, 50);
        }
        else
        {
            speedTmp = speedTrans.GetComponent<TextMeshProUGUI>();
        }

        // 5. Tạo KillCountText (Hiển thị Số Zombie hạ gục ở góc phải)
        TextMeshProUGUI killTmp = null;
        Transform killTrans = hudTransform.Find("KillCountText");
        if (killTrans == null)
        {
            GameObject killGo = new GameObject("KillCountText");
            killGo.transform.SetParent(hudTransform, false);
            killTmp = killGo.AddComponent<TextMeshProUGUI>();
            killTmp.text = "Kills: 0";
            killTmp.fontSize = 26;
            killTmp.fontStyle = FontStyles.Bold;
            killTmp.alignment = TextAlignmentOptions.Right;
            killTmp.color = new Color(1f, 0.3f, 0.3f); // Màu đỏ cam

            RectTransform rt = killGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-40, -15);
            rt.sizeDelta = new Vector2(200, 50);
        }
        else
        {
            killTmp = killTrans.GetComponent<TextMeshProUGUI>();
        }

        // 6. Gán script DistanceTrackerUI vào GameManagers
        GameObject managers = GameObject.Find("GameManagers");
        if (managers == null) managers = canvas.gameObject;

        DistanceTrackerUI tracker = managers.GetComponent<DistanceTrackerUI>();
        if (tracker == null) tracker = managers.AddComponent<DistanceTrackerUI>();

        // Gán tự động các tham chiếu qua SerializedObject
        SerializedObject so = new SerializedObject(tracker);
        so.Update();

        GameObject player = GameObject.Find("Square") ?? GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            so.FindProperty("playerTransform").objectReferenceValue = player.transform;
        }

        so.FindProperty("distanceText").objectReferenceValue = distanceTmp;
        so.FindProperty("speedText").objectReferenceValue = speedTmp;
        so.FindProperty("killCountText").objectReferenceValue = killTmp;

        // Tìm GameOverPanel nếu có
        GameObject gameOverPanel = GameObject.Find("GameOverPanel");
        if (gameOverPanel != null)
        {
            so.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
        }

        so.ApplyModifiedProperties();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        EditorUtility.DisplayDialog("Tạo UI Thành Công!",
            "Đã tự động tạo xong toàn bộ giao diện Distance Tracker:\n" +
            "• Quãng đường (Mét - Giữa màn hình)\n" +
            "• Vận tốc (km/h - Góc trên bên trái)\n" +
            "• Số Zombie tiêu diệt (Kills - Góc trên bên phải)\n" +
            "• Tự động kết nối với GameManagers và Player!",
            "Tuyệt Vời!");
    }
}
#endif
