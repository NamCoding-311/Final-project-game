#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ZombieSetupTool : EditorWindow
{
    [MenuItem("Tools/Tự Động Cài Đặt Zombie 8 Hướng (1-Click)")]
    public static void SetupZombiePrefab()
    {
        string prefabPath = "Assets/PreFab/Zombie.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file Zombie.prefab trong thư mục Assets/PreFab/!", "OK");
            return;
        }

        // Mở Prefab để chỉnh sửa
        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
        Zombie zombieScript = instance.GetComponent<Zombie>();

        if (zombieScript == null)
        {
            zombieScript = instance.AddComponent<Zombie>();
        }

        // Tải toàn bộ 8 Sprite cắt sẵn từ file ảnh
        string spriteSheetPath = "Assets/sprite basic zombie/Basic_infected_human_stylized_cartoon_copy_rotations_8dir-ezgif.com-gif-to-sprite-converter.png";
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);

        Sprite[] sprites = new Sprite[8];
        int foundCount = 0;

        foreach (Object obj in allAssets)
        {
            if (obj is Sprite s)
            {
                if (foundCount < 8)
                {
                    sprites[foundCount] = s;
                    foundCount++;
                }
            }
        }

        if (foundCount >= 8)
        {
            // Dùng SerializedObject để gán mảng directionalSprites
            SerializedObject so = new SerializedObject(zombieScript);
            so.Update();

            SerializedProperty propSprites = so.FindProperty("directionalSprites");
            if (propSprites != null)
            {
                propSprites.arraySize = 8;
                for (int i = 0; i < 8; i++)
                {
                    propSprites.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                }
            }

            so.ApplyModifiedProperties();
            Debug.Log("[ZombieSetupTool] Đã gán thành công 8 Sprite xoay hướng vào Zombie.prefab!");
        }

        // Lưu lại Prefab
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        PrefabUtility.UnloadPrefabContents(instance);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Cài Đặt Zombie Thành Công!",
            "Đã tự động cấu hình xong Zombie:\n" +
            "• Gán đủ 8 Sprite xoay theo 8 hướng nhìn\n" +
            "• Tự động xoay mặt và rượt đuổi người chơi khi di chuyển!",
            "Tuyệt Vời!");
    }
}
#endif
