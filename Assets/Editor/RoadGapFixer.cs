#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoadGapFixer : EditorWindow
{
    [MenuItem("Tools/Tự Động Vá Lỗ Hổng 1 Ô Giữa Đoạn Đường (1-Click)")]
    public static void FixRoadGap()
    {
        string prefabPath = "Assets/PreFab/Chunk_Road.prefab";
        GameObject prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefabObj == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file Chunk_Road.prefab trong Assets/PreFab/!", "OK");
            return;
        }

        // 1. Mở Prefab để vá lỗ hổng
        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
        Tilemap tilemap = instance.GetComponentInChildren<Tilemap>();

        if (tilemap == null)
        {
            PrefabUtility.UnloadPrefabContents(instance);
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Tilemap trong Chunk_Road.prefab!", "OK");
            return;
        }

        // Tối ưu hóa Bounds của Tilemap
        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;

        int minX = bounds.xMin;
        int maxX = bounds.xMax;
        int minY = bounds.yMin;
        int maxY = bounds.yMax;

        // Vá cột x = 0 nếu bị thiếu tile (sao chép từ cột liền kề)
        for (int y = minY; y <= maxY; y++)
        {
            Vector3Int sourcePos = new Vector3Int(minX, y, 0);
            TileBase sourceTile = tilemap.GetTile(sourcePos);

            if (sourceTile != null)
            {
                // Điền vào cột biên trái (minX - 1)
                Vector3Int targetPosLeft = new Vector3Int(minX - 1, y, 0);
                if (tilemap.GetTile(targetPosLeft) == null)
                {
                    tilemap.SetTile(targetPosLeft, sourceTile);
                }
            }

            Vector3Int sourcePosRight = new Vector3Int(maxX - 1, y, 0);
            TileBase sourceTileRight = tilemap.GetTile(sourcePosRight);
            if (sourceTileRight != null)
            {
                // Điền vào cột biên phải
                Vector3Int targetPosRight = new Vector3Int(maxX, y, 0);
                if (tilemap.GetTile(targetPosRight) == null)
                {
                    tilemap.SetTile(targetPosRight, sourceTileRight);
                }
            }
        }

        tilemap.CompressBounds();

        // 2. Chuẩn hóa Transform của Prefab về (0,0,0)
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        if (tilemap.transform != instance.transform)
        {
            tilemap.transform.localPosition = Vector3.zero;
            tilemap.transform.localRotation = Quaternion.identity;
            tilemap.transform.localScale = Vector3.one;
        }

        // Lưu lại Prefab
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        PrefabUtility.UnloadPrefabContents(instance);

        // 3. Cập nhật các đoạn đường đang có trong Scene (nếu có)
        EndlessMapManager mapManager = Object.FindAnyObjectByType<EndlessMapManager>();
        if (mapManager != null)
        {
            int exactWidth = tilemap.cellBounds.size.x;
            SerializedObject so = new SerializedObject(mapManager);
            so.Update();

            SerializedProperty propWidth = so.FindProperty("chunkWidth");
            if (propWidth != null)
            {
                propWidth.floatValue = exactWidth;
            }

            so.ApplyModifiedProperties();
            mapManager.ClearPreviewMap();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Vá Lỗ Hổng Thành Công!",
            "Đã tự động xử lý xong 100%:\n" +
            "• Điền đầy đủ Tile vào các cột biên x=0 của Chunk_Road.prefab\n" +
            "• Chuẩn hóa tọa độ tâm Prefab về (0,0,0)\n" +
            "• Cập nhật chính xác chiều rộng chunkWidth trong EndlessMapManager\n" +
            "Mặt đường sẽ nối liền mạch không còn kẽ hở!",
            "Tuyệt Vời!");
    }
}
#endif
