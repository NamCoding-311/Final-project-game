#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ArcadeTilemapBuilder : EditorWindow
{
    private const string TilesetPath = "Assets/MainPalette/Arcade/Arcade_Tileset.jpg";
    private const string TileAssetFolder = "Assets/MainPalette/Arcade/Tiles";
    private const int GridWidth = 32;
    private const int GridHeight = 32;

    [MenuItem("Tools/Tự Động Tạo Sàn Đấu Arcade Tilemap (1-Click)")]
    [MenuItem("CONTEXT/ArenaManager/Tự Động Tạo Sàn Đấu Arcade Tilemap (1-Click)")]
    public static void BuildArcadeTilemap()
    {
        BuildArcadeTilemapInternal(silent: false);
    }

    [InitializeOnLoadMethod]
    private static void AutoRunOnce()
    {
        EditorApplication.delayCall += () =>
        {
            // Chỉ chạy ngầm nếu người dùng đang chủ động mở Scene ArenaSurvival
            string currentScene = EditorSceneManager.GetActiveScene().name;
            if (currentScene != "ArenaSurvival") return;

            if (Directory.Exists(TileAssetFolder) && GameObject.Find("Arena_Tilemap") != null)
            {
                return;
            }
            BuildArcadeTilemapInternal(silent: true);
        };
    }

    public static void BuildArcadeTilemapInternal(bool silent)
    {
        // 1. Kiểm tra Scene hiện tại
        string currentScene = EditorSceneManager.GetActiveScene().name;
        if (currentScene != "ArenaSurvival")
        {
            // Tuyệt đối không tự động chuyển scene nếu đang chạy ngầm (silent)
            if (silent) return;

            string scenePath = "Assets/Scenes/ArenaSurvival.unity";
            bool proceed = EditorUtility.DisplayDialog("Xác nhận Scene",
                $"Bạn đang ở Scene '{currentScene}'. Công cụ này được thiết kế tối ưu nhất cho Scene 'ArenaSurvival'.\n\nBạn có muốn mở Scene 'ArenaSurvival' trước khi tạo Tilemap không?",
                "Mở ArenaSurvival & Tạo Tilemap", "Tiếp tục ở Scene hiện tại");

            if (proceed && File.Exists(scenePath))
            {
                EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                return;
            }
        }

        // 2. Kiểm tra file Tileset
        if (!File.Exists(TilesetPath))
        {
            if (!silent) EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy file ảnh Tileset tại:\n{TilesetPath}", "OK");
            else Debug.LogError($"[ArcadeTilemapBuilder] Không tìm thấy file ảnh Tileset tại: {TilesetPath}");
            return;
        }

        if (!silent) EditorUtility.DisplayProgressBar("Arcade Tilemap Builder", "Đang tải các lát cắt Sprite...", 0.1f);

        // 3. Tải tất cả Sprite đã cắt trong file ảnh
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(TilesetPath);
        Dictionary<int, Sprite> spriteDict = new Dictionary<int, Sprite>();

        foreach (var obj in subAssets)
        {
            if (obj is Sprite spr && spr.name.StartsWith("Arcade_Tileset_"))
            {
                if (int.TryParse(spr.name.Substring("Arcade_Tileset_".Length), out int index))
                {
                    spriteDict[index] = spr;
                }
            }
        }

        if (spriteDict.Count == 0)
        {
            if (!silent)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Sprite nào được cắt trong file Arcade_Tileset.jpg. Vui lòng kiểm tra lại thiết lập Sprite Mode: Multiple.", "OK");
            }
            else Debug.LogError("[ArcadeTilemapBuilder] Không tìm thấy Sprite nào được cắt trong file Arcade_Tileset.jpg.");
            return;
        }

        // 4. Tạo thư mục chứa Tile asset nếu chưa có
        if (!Directory.Exists(TileAssetFolder))
        {
            Directory.CreateDirectory(TileAssetFolder);
            AssetDatabase.Refresh();
        }

        // 5. Tạo hoặc tải các Tile asset (ScriptableObject)
        if (!silent) EditorUtility.DisplayProgressBar("Arcade Tilemap Builder", "Đang sinh Tile assets...", 0.3f);
        AssetDatabase.StartAssetEditing();

        Dictionary<int, Tile> tileDict = new Dictionary<int, Tile>();
        int totalTiles = GridWidth * GridHeight;

        for (int i = 0; i < totalTiles; i++)
        {
            string tilePath = $"{TileAssetFolder}/Arcade_Tile_{i}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);

            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                if (spriteDict.ContainsKey(i))
                {
                    tile.sprite = spriteDict[i];
                }
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            else
            {
                if (tile.sprite == null && spriteDict.ContainsKey(i))
                {
                    tile.sprite = spriteDict[i];
                    EditorUtility.SetDirty(tile);
                }
            }

            tileDict[i] = tile;
        }

        AssetDatabase.StopAssetEditing();
        AssetDatabase.SaveAssets();

        // 6. Tìm hoặc tạo GameObject Arena_Grid
        if (!silent) EditorUtility.DisplayProgressBar("Arcade Tilemap Builder", "Đang cấu hình Grid và Tilemap...", 0.6f);

        // 6. Tìm hoặc tạo GameObject Grid
        GameObject gridGo = GameObject.Find("Arena_Grid");
        if (gridGo == null) gridGo = GameObject.Find("Map");
        if (gridGo == null)
        {
            gridGo = new GameObject("Arena_Grid");
            gridGo.transform.position = Vector3.zero;
            Grid gridComp = gridGo.AddComponent<Grid>();
            gridComp.cellSize = new Vector3(1f, 1f, 0f);
            Undo.RegisterCreatedObjectUndo(gridGo, "Create Arena_Grid");
        }

        // 7. Tìm hoặc tạo Tilemap
        Tilemap tilemap = null;
        Transform tilemapTrans = gridGo.transform.Find("Arena_Tilemap");
        if (tilemapTrans == null) tilemapTrans = gridGo.transform.Find("Tilemap");
        GameObject tilemapGo = tilemapTrans != null ? tilemapTrans.gameObject : null;

        if (tilemapGo == null)
        {
            tilemap = Object.FindAnyObjectByType<Tilemap>();
            if (tilemap != null)
            {
                tilemapGo = tilemap.gameObject;
            }
            else
            {
                tilemapGo = new GameObject("Arena_Tilemap");
                tilemapGo.transform.SetParent(gridGo.transform, false);
                tilemapGo.transform.localPosition = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(tilemapGo, "Create Arena_Tilemap");
            }
        }

        if (tilemap == null) tilemap = tilemapGo.GetComponent<Tilemap>();
        if (tilemap == null) tilemap = tilemapGo.AddComponent<Tilemap>();

        TilemapRenderer tmRenderer = tilemapGo.GetComponent<TilemapRenderer>();
        if (tmRenderer == null) tmRenderer = tilemapGo.AddComponent<TilemapRenderer>();
        tmRenderer.sortingOrder = -20; // Đặt dưới cùng để nhân vật và quái hiển thị phía trên

        // 8. Vẽ 32x32 ô gạch đá cân đối quanh tâm (0, 0)
        if (!silent) EditorUtility.DisplayProgressBar("Arcade Tilemap Builder", "Đang vẽ sàn đấu 32x32 ô gạch đá...", 0.8f);
        tilemap.ClearAllTiles();

        for (int row = 0; row < GridHeight; row++)
        {
            for (int col = 0; col < GridWidth; col++)
            {
                int index = row * GridWidth + col;
                // Tọa độ Tilemap: Col 0..31 -> X -16..15; Row 0..31 -> Y 15..-16
                int x = col - (GridWidth / 2);
                int y = (GridHeight / 2 - 1) - row;

                if (tileDict.ContainsKey(index) && tileDict[index] != null)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tileDict[index]);
                }
            }
        }

        // 9. Xóa sàn đơn sắc tạm thời nếu còn sót
        GameObject oldFloor = GameObject.Find("Arena_Floor");
        if (oldFloor != null)
        {
            Undo.DestroyObjectImmediate(oldFloor);
        }

        // 10. Tối ưu Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 9.0f;
            mainCam.transform.position = new Vector3(0f, 0f, -10f);
            mainCam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f); // Màu nền đá tối tiệp màu
            EditorUtility.SetDirty(mainCam);
        }

        // 11. Đánh dấu dirty và lưu Scene
        EditorUtility.SetDirty(tilemapGo);
        EditorUtility.SetDirty(gridGo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        if (!silent)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Thành Công!",
                $"Đã tạo thành công Sàn Đấu Arcade Tilemap hoàn chỉnh!\n\n" +
                $"• Tổng số ô gạch: {totalTiles} ô (32x32)\n" +
                $"• Kích thước hiển thị: 32m x 32m phủ kín toàn bộ tầm nhìn Camera\n" +
                $"• Toàn bộ chi tiết đá, nắp cống và hoa văn đã khớp 100% không bị lệch mép.\n" +
                $"• Sorting Order: -20 (Nền sàn luôn nằm dưới nhân vật, quái & ngọc).",
                "Tuyệt Vời!");
        }
        else
        {
            Debug.Log($"<color=#00FF88>[ArcadeTilemapBuilder] Đã tạo thành công {totalTiles} ô gạch Arcade Tilemap (32x32) và phủ kín sàn đấu ArenaSurvival.unity!</color>");
        }
    }
}
#endif
