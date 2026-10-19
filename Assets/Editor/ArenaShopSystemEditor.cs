#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ArenaShopSystem))]
public class ArenaShopSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("⚡ THIẾT LẬP CỬA HÀNG NHANH", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(1f, 0.65f, 0.2f, 1f); // Màu cam vàng
        if (GUILayout.Button("🛍️ Tự Động Tạo & Gán Giao Diện Shop Canvas (1-Click)", GUILayout.Height(36)))
        {
            ArenaCanvasSetupTool.SetupArenaCanvasUI();
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif
