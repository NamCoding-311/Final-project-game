using UnityEngine;
using UnityEngine.SceneManagement;

// Quản lý hệ thống lưu trữ tiến trình chơi (Save & Continue System):
// - Tự động ghi nhớ Map và Chặng (Stage Checkpoint) đang chơi dở
// - Khi người chơi thoát game (Pause Menu, đóng cửa sổ, tắt máy) và bấm CONTINUE ở Main Menu:
//   Game sẽ tải lại đúng Map đó và tiếp tục ngay tại Chặng đang chơi dở
// - Khi người chơi bấm NEW GAME: Xóa sạch tiến trình cũ để bắt đầu lại từ đầu
public static class GameSaveManager
{
    public const string PREF_LAST_LEVEL = "LastLevel";
    public const string PREF_HAS_SAVED_PROGRESS = "HasSavedProgress";
    public const string PREF_SAVED_STAGE_PREFIX = "SavedStage_";
    public const string PREF_RESUME_FLAG = "ResumeFromSavedCheckpoint";

    // Lưu vị trí Checkpoint (Map và Chặng)
    public static void SaveCheckpoint(string sceneName, int stageIndex)
    {
        if (string.IsNullOrEmpty(sceneName) || !sceneName.StartsWith("map")) return;

        PlayerPrefs.SetString(PREF_LAST_LEVEL, sceneName);
        PlayerPrefs.SetInt(PREF_SAVED_STAGE_PREFIX + sceneName, stageIndex);
        PlayerPrefs.SetInt(PREF_HAS_SAVED_PROGRESS, 1);
        PlayerPrefs.Save();
        Debug.Log($"[GameSaveManager] Checkpoint Saved: Scene={sceneName}, Stage={stageIndex}");
    }

    // Kiểm tra có dữ liệu chơi dở để Tiếp tục hay không
    public static bool HasSaveData()
    {
        return PlayerPrefs.GetInt(PREF_HAS_SAVED_PROGRESS, 0) == 1 &&
               !string.IsNullOrEmpty(PlayerPrefs.GetString(PREF_LAST_LEVEL, ""));
    }

    // Lấy Scene cần nạp khi bấm Continue
    public static string GetSavedScene()
    {
        string scene = PlayerPrefs.GetString(PREF_LAST_LEVEL, "map1");
        if (Application.isPlaying && !Application.CanStreamedLevelBeLoaded(scene))
        {
            scene = "map1";
        }
        return scene;
    }

    // Lấy Chặng đã lưu của Scene
    public static int GetSavedStage(string sceneName)
    {
        return PlayerPrefs.GetInt(PREF_SAVED_STAGE_PREFIX + sceneName, 0);
    }

    // Đặt cờ đánh dấu cần nạp tiếp từ Checkpoint (để StageLevelManager xử lý)
    public static void SetResumeFlag(bool resume)
    {
        PlayerPrefs.SetInt(PREF_RESUME_FLAG, resume ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool ShouldResume()
    {
        return PlayerPrefs.GetInt(PREF_RESUME_FLAG, 0) == 1;
    }

    // Xóa sạch tiến trình khi người chơi bấm New Game
    public static void ClearSaveData()
    {
        PlayerPrefs.SetInt(PREF_HAS_SAVED_PROGRESS, 0);
        PlayerPrefs.SetInt(PREF_RESUME_FLAG, 0);
        PlayerPrefs.SetInt(PREF_SAVED_STAGE_PREFIX + "map1", 0);
        PlayerPrefs.SetInt(PREF_SAVED_STAGE_PREFIX + "map2", 0);
        PlayerPrefs.SetInt(PREF_SAVED_STAGE_PREFIX + "map3", 0);
        PlayerPrefs.SetString(PREF_LAST_LEVEL, "map1");
        PlayerPrefs.Save();
        Debug.Log("[GameSaveManager] Save progress cleared for New Game.");
    }

    // Chuỗi văn bản mô tả tiến trình hiển thị trên Main Menu
    public static string GetSaveSummaryText()
    {
        if (!HasSaveData()) return "NEW CAMPAIGN";

        string scene = GetSavedScene();
        int stage = GetSavedStage(scene);
        int sectorNum = 1;
        if (scene.Contains("2")) sectorNum = 2;
        else if (scene.Contains("3")) sectorNum = 3;

        return $"CONTINUE // SECTOR 0{sectorNum} (STAGE {stage + 1})";
    }
}
