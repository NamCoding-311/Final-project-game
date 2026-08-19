#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EndlessMapManager))]
public class EndlessMapManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Vẽ toàn bộ giao diện mặc định
        DrawDefaultInspector();

        EndlessMapManager manager = (EndlessMapManager)target;

        GUILayout.Space(15);
        GUILayout.Label("CÔNG CỤ XEM TRƯỚC BẢN ĐỒ (EDITOR PREVIEW)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("👀 TẠO PREVIEW MAP", GUILayout.Height(35)))
        {
            manager.GeneratePreviewMap();
            EditorUtility.SetDirty(manager);
        }

        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ XÓA PREVIEW MAP", GUILayout.Height(35)))
        {
            manager.ClearPreviewMap();
            EditorUtility.SetDirty(manager);
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("• Bấm 'TẠO PREVIEW MAP' để trải sẵn 4 đoạn đường ra Scene ngay khi chưa ấn Play.\n• Bạn có thể dùng Tile Palette để vẽ/chỉnh sửa trực tiếp lên các đoạn này.\n• Khi ấn Play, game sẽ tự động tiếp tục chạy từ các đoạn này!", MessageType.Info);
    }
}
#endif
