#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapChunkSplitter : EditorWindow
{
    [MenuItem("Tools/Tự Động Tách 3 Đoạn Map Thành Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<MapChunkSplitter>("Tách Map Chunks");
    }

    private void OnGUI()
    {
        GUILayout.Label("CÔNG CỤ TÁCH MAP TỰ ĐỘNG", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Công cụ này sẽ tự động đọc các Tilemap trong Scene hiện tại, cắt thành 3 Prefab riêng biệt (Cát, Tuyết, Đất) dài chuẩn và đưa mốc X về 0.", MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("BẮT ĐẦU TÁCH MAP (1-CLICK)", GUILayout.Height(40)))
        {
            ExecuteSplit();
        }
    }

    public static void ExecuteSplit()
    {
        // 1. Tìm Grid trong Scene
        Grid sceneGrid = Object.FindFirstObjectByType<Grid>();
        if (sceneGrid == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy đối tượng Grid nào trong Scene!", "OK");
            return;
        }

        // Lấy tất cả Tilemap dưới Grid
        Tilemap[] sourceTilemaps = sceneGrid.GetComponentsInChildren<Tilemap>();
        if (sourceTilemaps == null || sourceTilemaps.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Tilemap nào bên trong Grid!", "OK");
            return;
        }

        // 2. Tính toán tổng Bounds của toàn bộ các Tilemap
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (Tilemap tm in sourceTilemaps)
        {
            tm.CompressBounds();
            BoundsInt b = tm.cellBounds;
            if (b.size.x > 0 && b.size.y > 0)
            {
                minX = Mathf.Min(minX, b.min.x);
                maxX = Mathf.Max(maxX, b.max.x);
                minY = Mathf.Min(minY, b.min.y);
                maxY = Mathf.Max(maxY, b.max.y);
            }
        }

        if (minX >= maxX)
        {
            EditorUtility.DisplayDialog("Lỗi", "Các Tilemap đang trống hoặc không có dữ liệu!", "OK");
            return;
        }

        int totalWidth = maxX - minX;
        int chunkWidth = Mathf.CeilToInt(totalWidth / 3f);

        Debug.Log($"[MapChunkSplitter] Tổng chiều rộng map: {totalWidth} ô. Mỗi chunk dài: {chunkWidth} ô. Tọa độ X: [{minX} -> {maxX}]");

        // 3. Đảm bảo thư mục Assets/PreFab tồn tại
        if (!AssetDatabase.IsValidFolder("Assets/PreFab"))
        {
            AssetDatabase.CreateFolder("Assets", "PreFab");
        }

        string[] chunkNames = { "Chunk_Sand", "Chunk_Snow", "Chunk_Urban" };

        for (int i = 0; i < 3; i++)
        {
            string chunkName = chunkNames[i];
            int startX = minX + (i * chunkWidth);
            int endX = Mathf.Min(minX + ((i + 1) * chunkWidth), maxX);

            // Tạo GameObject gốc cho Chunk
            GameObject chunkRoot = new GameObject(chunkName);
            chunkRoot.transform.position = Vector3.zero;
            Grid chunkGrid = chunkRoot.AddComponent<Grid>();
            chunkGrid.cellSize = sceneGrid.cellSize;
            chunkGrid.cellGap = sceneGrid.cellGap;
            chunkGrid.cellLayout = sceneGrid.cellLayout;
            chunkGrid.cellSwizzle = sceneGrid.cellSwizzle;

            // Tạo các lớp Tilemap con tương ứng
            foreach (Tilemap srcTm in sourceTilemaps)
            {
                GameObject tmGo = new GameObject(srcTm.gameObject.name);
                tmGo.transform.SetParent(chunkRoot.transform);
                tmGo.transform.localPosition = Vector3.zero;

                Tilemap dstTm = tmGo.AddComponent<Tilemap>();
                TilemapRenderer dstRenderer = tmGo.AddComponent<TilemapRenderer>();
                TilemapRenderer srcRenderer = srcTm.GetComponent<TilemapRenderer>();

                if (srcRenderer != null)
                {
                    dstRenderer.sortingLayerID = srcRenderer.sortingLayerID;
                    dstRenderer.sortingOrder = srcRenderer.sortingOrder;
                    dstRenderer.material = srcRenderer.sharedMaterial;
                }

                // Copy các tile thuộc khoảng X của chunk này
                for (int x = startX; x < endX; x++)
                {
                    for (int y = minY; y < maxY; y++)
                    {
                        Vector3Int srcPos = new Vector3Int(x, y, 0);
                        TileBase tile = srcTm.GetTile(srcPos);
                        if (tile != null)
                        {
                            // Dời tọa độ X về gốc 0 của chunk
                            Vector3Int dstPos = new Vector3Int(x - startX, y, 0);
                            dstTm.SetTile(dstPos, tile);

                            // Giữ nguyên Transform Matrix và Màu sắc tile nếu có
                            dstTm.SetTransformMatrix(dstPos, srcTm.GetTransformMatrix(srcPos));
                            dstTm.SetColor(dstPos, srcTm.GetColor(srcPos));
                        }
                    }
                }

                dstTm.CompressBounds();
            }

            // Lưu thành Prefab trong thư mục Assets/PreFab/
            string prefabPath = $"Assets/PreFab/{chunkName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(chunkRoot, prefabPath);
            Debug.Log($"[MapChunkSplitter] Đã tạo thành công: {prefabPath}");

            // Xóa object tạm trong Scene
            Object.DestroyImmediate(chunkRoot);
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Thành công!", 
            $"Đã tách thành công 3 đoạn map thành Prefab:\n1. Chunk_Sand.prefab\n2. Chunk_Snow.prefab\n3. Chunk_Urban.prefab\n\nĐộ dài mỗi Chunk (Chunk Width) là: {chunkWidth} ô.", 
            "Tuyệt vời!");
    }
}
#endif
