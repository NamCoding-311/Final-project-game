using System;
using System.Collections;
using TMPro;
using UnityEngine;

// =============================================================================
// GameRewardManager: Hệ Thống Trung Tâm Quản Lý Điểm, Tiền Thưởng ($ Cash) & Ví Tiền Người Chơi
// Tự động lưu trữ PlayerPrefs("PlayerCash") và đồng bộ với cả:
// 1. Chế độ Chiến Dịch (Campaign Sectors: Map 1, Map 2, Map 3)
// 2. Chế độ Đấu Trường Sinh Tồn (Arena Survival: 10 Waves)
// =============================================================================
public class GameRewardManager : MonoBehaviour
{
    public static GameRewardManager Instance { get; private set; }

    public const string PREF_PLAYER_CASH = "PlayerCash";
    public const string PREF_TOTAL_COINS = "TotalCoins"; // Để tương thích ngược với CollectibleItem

    public static event Action<int> OnCashChanged;

    [Header("Starting Economy")]
    [SerializeField] private int initialCash = 2500;

    // Chi tiết thưởng Đấu trường (Arena Survival)
    [System.Serializable]
    public struct ArenaRewardResult
    {
        public int waveCount;
        public int killCount;
        public int gemsCount;
        public bool isVictory;

        public int waveCash;      // waveCount * 150
        public int killCash;      // killCount * 2
        public int gemsCash;      // gemsCount * 1
        public int victoryBonus;  // +2,000 nếu thắng 10 Wave
        public int totalCash;     // Tổng số tiền nhận được
    }

    // Chi tiết thưởng Chiến dịch (Campaign Sectors)
    [System.Serializable]
    public struct CampaignRewardResult
    {
        public int sectorIndex;   // 1, 2, 3
        public string rank;       // S, A, B, C
        public float multiplier;  // 1.5, 1.25, 1.0, 0.5
        public int baseBounty;    // 1,500 / 3,500 / 8,000
        public int distanceCash;  // Cự ly hoàn thành
        public int killCash;      // Quái diệt được
        public int totalCash;     // Tổng tiền nhận được
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeWallet();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeWallet()
    {
        if (!PlayerPrefs.HasKey(PREF_PLAYER_CASH))
        {
            PlayerPrefs.SetInt(PREF_PLAYER_CASH, initialCash);
            PlayerPrefs.SetInt(PREF_TOTAL_COINS, initialCash);
            PlayerPrefs.Save();
        }
    }

    // =========================================================================
    // 1. VÍ TIỀN (WALLET MANAGEMENT)
    // =========================================================================

    public int GetCurrentCash()
    {
        return PlayerPrefs.GetInt(PREF_PLAYER_CASH, initialCash);
    }

    public void AddCash(int amount, string reason = "")
    {
        if (amount <= 0) return;

        int current = GetCurrentCash();
        int updated = current + amount;

        PlayerPrefs.SetInt(PREF_PLAYER_CASH, updated);
        PlayerPrefs.SetInt(PREF_TOTAL_COINS, updated);
        PlayerPrefs.Save();

        Debug.Log($"[GameRewardManager] +${amount:N0} Cash added ({reason}). New Balance: ${updated:N0}");
        OnCashChanged?.Invoke(updated);
    }

    public bool SpendCash(int amount, string reason = "")
    {
        if (amount <= 0) return true;

        int current = GetCurrentCash();
        if (current < amount)
        {
            Debug.LogWarning($"[GameRewardManager] Not enough cash! Required: ${amount:N0}, Current: ${current:N0}");
            return false;
        }

        int updated = current - amount;
        PlayerPrefs.SetInt(PREF_PLAYER_CASH, updated);
        PlayerPrefs.SetInt(PREF_TOTAL_COINS, updated);
        PlayerPrefs.Save();

        Debug.Log($"[GameRewardManager] -${amount:N0} Cash spent ({reason}). Remaining: ${updated:N0}");
        OnCashChanged?.Invoke(updated);
        return true;
    }

    // =========================================================================
    // 2. TÍNH TIỀN THƯỞNG CHO CHẾ ĐỘ ĐẤU TRƯỜNG SINH TỒN (ARENA SURVIVAL)
    // =========================================================================
    public ArenaRewardResult CalculateAndAwardArenaReward(int wavesCleared, int kills, int gems, bool isVictory)
    {
        ArenaRewardResult result = new ArenaRewardResult
        {
            waveCount = wavesCleared,
            killCount = kills,
            gemsCount = gems,
            isVictory = isVictory,

            waveCash = wavesCleared * 150,     // 150$ mỗi Wave sống sót
            killCash = kills * 2,              // 2$ mỗi quái hạ gục
            gemsCash = gems * 1,               // 1$ mỗi ngọc/phế liệu nhặt được
            victoryBonus = isVictory ? 2000 : 0 // +2,000$ khi phá đảo Wave 10
        };

        result.totalCash = result.waveCash + result.killCash + result.gemsCash + result.victoryBonus;

        AddCash(result.totalCash, $"Arena Survival {(isVictory ? "Victory" : $"Wave {wavesCleared}")}");
        return result;
    }

    // =========================================================================
    // 3. TÍNH TIỀN THƯỞNG CHO CHẾ ĐỘ CHIẾN DỊCH (CAMPAIGN SECTORS)
    // =========================================================================
    public CampaignRewardResult CalculateAndAwardCampaignReward(int sectorIndex, float distance, int kills, float hpPercent, bool isSuccess)
    {
        int baseBounty = 0;
        string rank;
        float multiplier = 1.0f;
        int distCash;
        int killCash;
        int finalTotal;

        if (isSuccess)
        {
            // CHIẾN THẮNG (Về đích thành công)
            baseBounty = sectorIndex switch
            {
                1 => 1500,
                2 => 3500,
                3 => 8000,
                _ => 1500
            };

            if (hpPercent >= 0.80f)
            {
                rank = "S";
                multiplier = 1.50f;
            }
            else if (hpPercent >= 0.50f)
            {
                rank = "A";
                multiplier = 1.25f;
            }
            else
            {
                rank = "B";
                multiplier = 1.0f;
            }

            distCash = Mathf.FloorToInt(distance * 1.5f);
            killCash = kills * 10;
            int subTotal = baseBounty + distCash + killCash;
            finalTotal = Mathf.FloorToInt(subTotal * multiplier);
        }
        else
        {
            // THẤT BẠI / CHẾT GIỮA ĐƯỜNG (Game Over)
            // Không có tiền hoàn thành chiến dịch (baseBounty = 0)
            // Chỉ nhận tiền mót phế liệu thực tế (Salvage Scrap):
            // + 1$ mỗi mét chạy được
            // + 10$ mỗi quái tông trúng
            rank = "KIA";
            baseBounty = 0;
            multiplier = 1.0f;
            distCash = Mathf.FloorToInt(distance * 1.0f);
            killCash = kills * 10;
            finalTotal = distCash + killCash;
        }

        CampaignRewardResult result = new CampaignRewardResult
        {
            sectorIndex = sectorIndex,
            rank = rank,
            multiplier = multiplier,
            baseBounty = baseBounty,
            distanceCash = distCash,
            killCash = killCash,
            totalCash = finalTotal
        };

        AddCash(finalTotal, $"Campaign Sector 0{sectorIndex} [{(isSuccess ? $"Rank {rank}" : "KIA Salvage")}]");
        return result;
    }

    // =========================================================================
    // 4. ANIMATION ĐẾM SỐ TIỀN NHẢY ("TING TING TING") CHO UI
    // =========================================================================
    public static IEnumerator AnimateCashCounter(TextMeshProUGUI targetText, int startValue, int endValue, string prefix = "+$", string suffix = " CASH", float duration = 1.2f, AudioSource audioSource = null, AudioClip tickClip = null)
    {
        if (targetText == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease out cubic
            float ease = 1f - Mathf.Pow(1f - t, 3);
            int current = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, ease));

            targetText.text = $"{prefix}{current:N0}{suffix}";

            if (audioSource != null && tickClip != null && Time.frameCount % 4 == 0)
            {
                audioSource.PlayOneShot(tickClip, 0.4f);
            }

            yield return null;
        }

        targetText.text = $"{prefix}{endValue:N0}{suffix}";
    }
}
