#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class PlayerGunSetupTool : EditorWindow
{
    [MenuItem("Tools/Tự Động Gắn Súng Vào Player (1-Click)")]
    public static void SetupPlayerGun()
    {
        // 1. Tìm GameObject Player trong Scene
        GameObject playerObj = GameObject.Find("Square");
        if (playerObj == null)
        {
            playerObj = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerObj == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy GameObject 'Square' hoặc đối tượng có Tag 'Player' trong Scene!", "OK");
            return;
        }

        // Đăng ký hoàn tác Undo
        Undo.RegisterFullObjectHierarchyUndo(playerObj, "Setup Player Gun");

        // 2. Tìm hoặc tạo GunHolder
        Transform gunHolderTransform = playerObj.transform.Find("GunHolder");
        if (gunHolderTransform == null)
        {
            GameObject gunHolderGo = new GameObject("GunHolder");
            gunHolderGo.transform.SetParent(playerObj.transform);
            gunHolderGo.transform.localPosition = new Vector3(0.2f, 0f, 0f);
            gunHolderGo.transform.localRotation = Quaternion.identity;
            gunHolderGo.transform.localScale = Vector3.one;
            gunHolderTransform = gunHolderGo.transform;
        }

        // 3. Tìm hoặc tạo GunSprite (Layer trên)
        Transform gunSpriteTransform = gunHolderTransform.Find("GunSprite");
        if (gunSpriteTransform == null)
        {
            GameObject gunSpriteGo = new GameObject("GunSprite");
            gunSpriteGo.transform.SetParent(gunHolderTransform);
            // Dời nhẹ ra trước để tâm xoay nằm ở báng súng
            gunSpriteGo.transform.localPosition = new Vector3(0.25f, 0f, 0f);
            gunSpriteGo.transform.localRotation = Quaternion.identity;
            gunSpriteGo.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            gunSpriteTransform = gunSpriteGo.transform;
        }

        SpriteRenderer gunRenderer = gunSpriteTransform.GetComponent<SpriteRenderer>();
        if (gunRenderer == null)
        {
            gunRenderer = gunSpriteTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        // Cài đặt Order in Layer = 1 (để nổi lên trên thân xe/người chơi)
        gunRenderer.sortingOrder = 1;

        // Tải ảnh súng AK-47
        string gunSpritePath = "Assets/Guns_V1.01 - Commission - Copy/01 - Individual sprites/Guns/AK 47 [96x48].png";
        Sprite gunSprite = AssetDatabase.LoadAssetAtPath<Sprite>(gunSpritePath);
        if (gunSprite == null)
        {
            // Nếu là Sprite Sheet Multiple, lấy sub-asset đầu tiên
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(gunSpritePath);
            foreach (Object obj in allAssets)
            {
                if (obj is Sprite s)
                {
                    gunSprite = s;
                    break;
                }
            }
        }

        if (gunSprite != null)
        {
            gunRenderer.sprite = gunSprite;
        }

        // 4. Tìm hoặc tạo FirePoint (nơi đạn bay ra)
        Transform firePointTransform = gunHolderTransform.Find("FirePoint");
        if (firePointTransform == null)
        {
            GameObject firePointGo = new GameObject("FirePoint");
            firePointGo.transform.SetParent(gunHolderTransform);
            firePointGo.transform.localPosition = new Vector3(0.75f, 0.05f, 0f);
            firePointGo.transform.localRotation = Quaternion.identity;
            firePointTransform = firePointGo.transform;
        }

        // 5. Gắn và cấu hình PlayerShooting component trên Player
        PlayerShooting shooting = playerObj.GetComponent<PlayerShooting>();
        if (shooting == null)
        {
            shooting = playerObj.AddComponent<PlayerShooting>();
        }

        // Tải WeaponData (AR_Data)
        WeaponData arData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Weapon/AR_Data.asset");

        // Sử dụng SerializedObject để gán trường private SerializedField
        SerializedObject so = new SerializedObject(shooting);
        so.Update();

        SerializedProperty propWeapon = so.FindProperty("currentWeapon");
        if (propWeapon != null && arData != null) propWeapon.objectReferenceValue = arData;

        SerializedProperty propHolder = so.FindProperty("gunHolder");
        if (propHolder != null) propHolder.objectReferenceValue = gunHolderTransform;

        SerializedProperty propFirePoint = so.FindProperty("firePoint");
        if (propFirePoint != null) propFirePoint.objectReferenceValue = firePointTransform;

        SerializedProperty propRenderer = so.FindProperty("gunSpriteRenderer");
        if (propRenderer != null) propRenderer.objectReferenceValue = gunRenderer;

        so.ApplyModifiedProperties();

        // Đánh dấu Scene đã thay đổi để lưu lại
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(playerObj.scene);

        EditorUtility.DisplayDialog("Gắn Súng Thành Công!",
            "Đã tự động tạo xong cấu trúc súng Layer trên cho Player:\n" +
            "• GunHolder (Tay cầm xoay 360 độ)\n" +
            "• GunSprite (Khẩu AK-47, Order in Layer = 1)\n" +
            "• FirePoint (Đầu nòng bắn đạn)\n" +
            "• Gán sẵn Script PlayerShooting và dữ liệu AR_Data!",
            "Quá Tuyệt!");
    }
}
#endif
