#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ArenaManager))]
public class ArenaManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Vẽ inspector mặc định
        DrawDefaultInspector();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("⚡ CÔNG CỤ NHANH (1-CLICK ARENA TOOLS)", EditorStyles.boldLabel);

        // Nút 1: Tạo sàn đấu Arcade Tilemap
        GUI.backgroundColor = new Color(0.35f, 0.75f, 0.45f, 1f); // Xanh lá nổi bật
        if (GUILayout.Button("🎮 Tự Động Tạo Sàn Đấu Arcade Tilemap (1-Click)", GUILayout.Height(36)))
        {
            ArcadeTilemapBuilder.BuildArcadeTilemap();
        }

        EditorGUILayout.Space(5);

        // Nút 2: Tạo UI Arena Canvas + Shop
        GUI.backgroundColor = new Color(0.35f, 0.60f, 0.95f, 1f); // Xanh dương
        if (GUILayout.Button("🎨 Tự Động Tạo và Gán Giao Diện Arena Canvas (HUD + Level Up + Shop)", GUILayout.Height(36)))
        {
            ArenaCanvasSetupTool.SetupArenaCanvasUI();
        }

        EditorGUILayout.Space(5);

        // Nút 3: Tạo Cửa Hàng Giữa Các Hiệp
        GUI.backgroundColor = new Color(1f, 0.65f, 0.2f, 1f); // Cam vàng nổi bật
        if (GUILayout.Button("🛍️ Tự Động Thiết Lập Cửa Hàng Giữa Các Hiệp (Shop 1-Click)", GUILayout.Height(34)))
        {
            ArenaCanvasSetupTool.SetupArenaCanvasUI();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif
