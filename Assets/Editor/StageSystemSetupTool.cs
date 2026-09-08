#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Công cụ tự động thiết lập Hệ Thống 3 Chặng (Sáng - Chiều - Tối) & Trùm Zombie trên 1 Scene
[InitializeOnLoad]
public class StageSystemSetupTool : EditorWindow
{
    static StageSystemSetupTool()
    {
        // Tự động kiểm tra và cấu hình ngay khi Unity compile/reload nếu scene chưa có StageLevelManager
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying) return;

            StageLevelManager existing = Object.FindAnyObjectByType<StageLevelManager>(FindObjectsInactive.Include);
            if (existing == null)
            {
                SetupStageAndBossSystem(silent: true);
            }
        };
    }

    [MenuItem("Tools/Tự Động Cài Đặt Hệ Thống 3 Chặng & Boss (1-Click)")]
    public static void ManualSetupMenuItem()
    {
        SetupStageAndBossSystem(silent: false);
    }

    public static void SetupStageAndBossSystem(bool silent = false)
    {
        // 1. Tìm hoặc tạo Prefab Boss từ Zombie hiện có
        string bossPrefabPath = "Assets/PreFab/Zombie Boss.prefab";
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bossPrefabPath);

        if (bossPrefab == null)
        {
            bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFab/ZombieBoss.prefab");
        }

        if (bossPrefab == null)
        {
            GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFab/Zombie.prefab");
            if (zombiePrefab != null)
            {
                GameObject bossInstance = Object.Instantiate(zombiePrefab);
                bossInstance.name = "Zombie Boss";
                bossInstance.transform.localScale = new Vector3(3.2f, 3.2f, 1f); // To gấp đôi zombie thường

                Zombie regularZombie = bossInstance.GetComponent<Zombie>();
                if (regularZombie != null) Object.DestroyImmediate(regularZombie);

                ZombieBoss zb = bossInstance.GetComponent<ZombieBoss>();
                if (zb == null) zb = bossInstance.AddComponent<ZombieBoss>();

                SpriteRenderer sr = bossInstance.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(1f, 0.4f, 0.4f); // Nhuộm đỏ

                bossPrefab = PrefabUtility.SaveAsPrefabAsset(bossInstance, bossPrefabPath);
                Object.DestroyImmediate(bossInstance);
                AssetDatabase.Refresh();
            }
        }

        // 2. Tìm hoặc bổ sung UI cho Boss và Chuyển Chặng trên Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        // Tạo Text Thông Báo Chặng (Stage Announcement)
        Transform announceTr = canvas.transform.Find("StageAnnouncementText");
        TextMeshProUGUI announceText = null;
        if (announceTr == null)
        {
            GameObject announceGo = new GameObject("StageAnnouncementText");
            announceGo.transform.SetParent(canvas.transform, false);
            announceText = announceGo.AddComponent<TextMeshProUGUI>();
            announceText.text = "CHẶNG 1: BỜ SÔNG BAN NGÀY";
            announceText.fontSize = 44;
            announceText.fontStyle = FontStyles.Bold;
            announceText.alignment = TextAlignmentOptions.Center;
            announceText.color = Color.yellow;

            RectTransform rt = announceGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(800, 150);
            rt.anchoredPosition = new Vector2(0, 100);
            announceGo.SetActive(false);
        }
        else
        {
            announceText = announceTr.GetComponent<TextMeshProUGUI>();
        }

        // Tạo Banner Cảnh Báo Boss (Warning Banner)
        Transform warningTr = canvas.transform.Find("BossWarningBanner");
        GameObject warningGo = null;
        if (warningTr == null)
        {
            warningGo = new GameObject("BossWarningBanner");
            warningGo.transform.SetParent(canvas.transform, false);

            TextMeshProUGUI warnText = warningGo.AddComponent<TextMeshProUGUI>();
            warnText.text = "⚠️ CẢNH BÁO: TRÙM ZOMBIE XUẤT HIỆN! ⚠️";
            warnText.fontSize = 40;
            warnText.fontStyle = FontStyles.Bold;
            warnText.alignment = TextAlignmentOptions.Center;
            warnText.color = Color.red;

            RectTransform rt = warningGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.8f);
            rt.anchorMax = new Vector2(0.5f, 0.8f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(900, 80);
            rt.anchoredPosition = Vector2.zero;
            warningGo.SetActive(false);
        }
        else
        {
            warningGo = warningTr.gameObject;
        }

        // Tạo Thanh Máu Boss (Boss Health Bar Slider)
        Transform bossHpTr = canvas.transform.Find("BossHealthBar");
        Slider bossSlider = null;
        TextMeshProUGUI bossNameTxt = null;

        if (bossHpTr == null)
        {
            GameObject sliderGo = new GameObject("BossHealthBar");
            sliderGo.transform.SetParent(canvas.transform, false);
            bossSlider = sliderGo.AddComponent<Slider>();

            RectTransform sliderRt = sliderGo.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.5f, 0.92f);
            sliderRt.anchorMax = new Vector2(0.5f, 0.92f);
            sliderRt.pivot = new Vector2(0.5f, 0.5f);
            sliderRt.sizeDelta = new Vector2(500, 26);
            sliderRt.anchoredPosition = Vector2.zero;

            // Background thanh máu
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderGo.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            // Fill Area thanh máu đỏ
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.9f, 0.1f, 0.1f, 1f);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;

            bossSlider.fillRect = fillRt;
            bossSlider.targetGraphic = fillImg;

            // Text Tên Boss
            GameObject nameGo = new GameObject("BossNameText");
            nameGo.transform.SetParent(sliderGo.transform, false);
            bossNameTxt = nameGo.AddComponent<TextMeshProUGUI>();
            bossNameTxt.text = "TRÙM ĐỘT BIẾN";
            bossNameTxt.fontSize = 20;
            bossNameTxt.fontStyle = FontStyles.Bold;
            bossNameTxt.alignment = TextAlignmentOptions.Center;
            bossNameTxt.color = Color.white;

            RectTransform nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.5f, 1.5f);
            nameRt.anchorMax = new Vector2(0.5f, 1.5f);
            nameRt.pivot = new Vector2(0.5f, 0.5f);
            nameRt.sizeDelta = new Vector2(400, 30);
            nameRt.anchoredPosition = Vector2.zero;

            sliderGo.SetActive(false);
        }
        else
        {
            bossSlider = bossHpTr.GetComponent<Slider>();
            bossNameTxt = bossHpTr.GetComponentInChildren<TextMeshProUGUI>();
        }

        // 3. Tìm hoặc tạo Nhân Vật Đi Bộ (OnFootPlayer) trong Scene
        GameObject onFootGo = GameObject.Find("OnFootPlayer");
        if (onFootGo == null)
        {
            OnFootPlayerController existingCtrl = Object.FindAnyObjectByType<OnFootPlayerController>(FindObjectsInactive.Include);
            if (existingCtrl != null) onFootGo = existingCtrl.gameObject;
        }

        if (onFootGo == null)
        {
            onFootGo = new GameObject("OnFootPlayer");
            onFootGo.tag = "Untagged"; // Không đặt là Player để tránh nhầm với xe
            onFootGo.transform.position = new Vector3(0, 0, 0);

            // SpriteRenderer cho nhân vật
            SpriteRenderer sr = onFootGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 15;
            sr.color = new Color(0.2f, 0.8f, 1f); // Xanh chiến binh

            Player3LaneMovement car = Object.FindAnyObjectByType<Player3LaneMovement>();
            if (car != null)
            {
                SpriteRenderer carSr = car.GetComponent<SpriteRenderer>();
                if (carSr != null) sr.sprite = carSr.sprite;
            }

            // Rigidbody2D & Collider2D
            Rigidbody2D rb = onFootGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            CircleCollider2D col = onFootGo.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            onFootGo.AddComponent<OnFootPlayerController>();

            // Tạo GunHolder và FirePoint cho người đi bộ
            GameObject gunHolderGo = new GameObject("GunHolder");
            gunHolderGo.transform.SetParent(onFootGo.transform, false);
            gunHolderGo.transform.localPosition = new Vector3(0.3f, 0f, 0f);

            SpriteRenderer gunSr = gunHolderGo.AddComponent<SpriteRenderer>();
            gunSr.sortingOrder = 16;
            Sprite akSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Guns_V1.01 - Commission - Copy/01 - Individual sprites/Guns/AK 47 [96x48].png");
            if (akSprite != null) gunSr.sprite = akSprite;

            GameObject firePointGo = new GameObject("FirePoint");
            firePointGo.transform.SetParent(gunHolderGo.transform, false);
            firePointGo.transform.localPosition = new Vector3(1.0f, 0.1f, 0f);

            // Gắn PlayerShooting cho người đi bộ
            PlayerShooting shooting = onFootGo.AddComponent<PlayerShooting>();
            WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Weapon/AR_Data.asset");
            if (weapon != null)
            {
                SerializedObject shootSo = new SerializedObject(shooting);
                shootSo.FindProperty("currentWeapon").objectReferenceValue = weapon;
                shootSo.FindProperty("gunHolder").objectReferenceValue = gunHolderGo.transform;
                shootSo.FindProperty("firePoint").objectReferenceValue = firePointGo.transform;
                shootSo.FindProperty("gunSpriteRenderer").objectReferenceValue = gunSr;
                shootSo.ApplyModifiedProperties();
            }

            onFootGo.SetActive(false); // Ẩn ban đầu, chỉ kích hoạt khi xe dừng đánh Boss
        }

        // 4. Gắn StageLevelManager vào GameManagers trong Scene
        GameObject gameManagers = GameObject.Find("GameManagers");
        if (gameManagers == null)
        {
            gameManagers = new GameObject("GameManagers");
        }

        StageLevelManager stageMgr = gameManagers.GetComponent<StageLevelManager>();
        if (stageMgr == null)
        {
            stageMgr = gameManagers.AddComponent<StageLevelManager>();
        }

        // Gán tham chiếu bằng SerializedObject
        SerializedObject so = new SerializedObject(stageMgr);
        so.Update();

        Player3LaneMovement carMovement = Object.FindAnyObjectByType<Player3LaneMovement>();
        if (carMovement != null)
        {
            so.FindProperty("playerTransform").objectReferenceValue = carMovement.transform;
        }

        Light2D light = Object.FindAnyObjectByType<Light2D>();
        if (light != null)
        {
            so.FindProperty("globalLight2D").objectReferenceValue = light;
        }

        ZombieSpawner spawner = Object.FindAnyObjectByType<ZombieSpawner>();
        if (spawner != null)
        {
            so.FindProperty("zombieSpawner").objectReferenceValue = spawner;
        }

        if (onFootGo != null)
        {
            so.FindProperty("onFootPlayer").objectReferenceValue = onFootGo.GetComponent<OnFootPlayerController>();
        }

        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
        {
            so.FindProperty("cameraFollow").objectReferenceValue = cam;
        }

        EndlessMapManager map = Object.FindAnyObjectByType<EndlessMapManager>();
        if (map != null)
        {
            so.FindProperty("mapManager").objectReferenceValue = map;
        }

        ObstacleSpawner obs = Object.FindAnyObjectByType<ObstacleSpawner>();
        if (obs != null)
        {
            so.FindProperty("obstacleSpawner").objectReferenceValue = obs;
        }

        if (announceText != null)
        {
            so.FindProperty("stageAnnouncementText").objectReferenceValue = announceText;
        }

        if (warningGo != null)
        {
            so.FindProperty("bossWarningBanner").objectReferenceValue = warningGo;
        }

        if (bossSlider != null)
        {
            so.FindProperty("bossHealthBar").objectReferenceValue = bossSlider;
        }

        if (bossNameTxt != null)
        {
            so.FindProperty("bossNameText").objectReferenceValue = bossNameTxt;
        }

        // Cài đặt Prefab Boss vào các chặng và chỉnh mốc khoảng cách xuất hiện
        SerializedProperty stagesProp = so.FindProperty("stages");
        if (stagesProp != null && stagesProp.arraySize >= 3)
        {
            stagesProp.GetArrayElementAtIndex(0).FindPropertyRelative("bossSpawnDistance").floatValue = 100f; // Chặng 1: 100m gặp Boss
            stagesProp.GetArrayElementAtIndex(1).FindPropertyRelative("bossSpawnDistance").floatValue = 250f; // Chặng 2: 250m gặp Boss
            stagesProp.GetArrayElementAtIndex(2).FindPropertyRelative("bossSpawnDistance").floatValue = 500f; // Chặng 3: 500m gặp Boss cuối

            if (bossPrefab != null)
            {
                stagesProp.GetArrayElementAtIndex(0).FindPropertyRelative("bossPrefab").objectReferenceValue = bossPrefab;
                stagesProp.GetArrayElementAtIndex(1).FindPropertyRelative("bossPrefab").objectReferenceValue = bossPrefab;
                stagesProp.GetArrayElementAtIndex(2).FindPropertyRelative("bossPrefab").objectReferenceValue = bossPrefab;
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(gameManagers);

        // Lưu scene tự động để không bị mất khi reload
        EditorSceneManager.MarkSceneDirty(gameManagers.scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        if (!silent)
        {
            EditorUtility.DisplayDialog("Cài Đặt Hệ Thống Xuống Xe Đánh Boss Thành Công!",
                "Đã tự động thiết lập xong 100%:\n" +
                "• Xe chạy đến mốc 100m -> Tấp lề góc trên bên trái\n" +
                "• Người chơi bước xuống xe (WASD 8 hướng, ngắm bắn chuột)\n" +
                "• Khóa Đấu Trường (Arena Lock-In), Trùm Zombie xuất hiện\n" +
                "• Tiêu diệt Trùm -> Lên xe -> Tăng tốc chuyển sang Chặng tiếp theo!\n\n" +
                "Bấm Play ▶️ để trải nghiệm ngay!",
                "Tuyệt Vời!");
        }
    }
}
#endif
