using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared PlayerPrefs persistence for Campaign on-foot weapons (unlocked + equipped).
/// </summary>
public static class CampaignWeaponProgress
{
    public const string DefaultWeaponId = "Piston_Data";
    private const string EquippedWeaponKey = "CampaignWeapon_Equipped";
    private const string UnlockKeyPrefix = "CampaignWeapon_Unlocked_";

    public static bool IsWeaponUnlocked(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId)) return false;
        if (string.Equals(weaponId, DefaultWeaponId, StringComparison.OrdinalIgnoreCase)) return true;
        return PlayerPrefs.GetInt(UnlockKeyPrefix + weaponId, 0) == 1;
    }

    public static void UnlockWeapon(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId)) return;
        PlayerPrefs.SetInt(UnlockKeyPrefix + weaponId, 1);
        PlayerPrefs.Save();
    }

    public static string GetEquippedWeaponId()
    {
        string equipped = PlayerPrefs.GetString(EquippedWeaponKey, DefaultWeaponId);
        if (string.IsNullOrEmpty(equipped) || !IsWeaponUnlocked(equipped))
        {
            return DefaultWeaponId;
        }
        return equipped;
    }

    public static void EquipWeapon(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId) || !IsWeaponUnlocked(weaponId)) return;
        PlayerPrefs.SetString(EquippedWeaponKey, weaponId);
        PlayerPrefs.Save();
    }
}

/// <summary>
/// Controls the pre-baked Campaign Weapon Armory / Shop UI directly present on the Garage Canvas.
/// </summary>
public class CampaignArmoryController : MonoBehaviour
{
    [Serializable]
    public class ArmoryCardView
    {
        public WeaponData weaponData;
        public string displayName;
        public int price;
        public Outline cardOutline;
        public Button actionButton;
        public Image actionBg;
        public TMP_Text actionLabel;
    }

    [Header("Pre-Baked Canvas References")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private Button openArmoryButton;
    [SerializeField] private TMP_Text openButtonLabel;
    [SerializeField] private Button closeModalButton;
    [SerializeField] private TMP_Text modalCashText;
    [SerializeField] private TMP_FontAsset uiFont;

    [Header("7 Baked Weapon Cards")]
    [SerializeField] private List<ArmoryCardView> bakedCardViews = new List<ArmoryCardView>();

    private void OnEnable()
    {
        GameRewardManager.OnCashChanged += HandleCashChanged;
    }

    private void OnDisable()
    {
        GameRewardManager.OnCashChanged -= HandleCashChanged;
    }

    private void Start()
    {
        if (openArmoryButton != null)
        {
            openArmoryButton.onClick.RemoveAllListeners();
            openArmoryButton.onClick.AddListener(OpenArmory);
        }

        if (closeModalButton != null)
        {
            closeModalButton.onClick.RemoveAllListeners();
            closeModalButton.onClick.AddListener(CloseArmory);
        }

        foreach (ArmoryCardView card in bakedCardViews)
        {
            if (card == null || card.actionButton == null || card.weaponData == null) continue;
            ArmoryCardView captured = card;
            card.actionButton.onClick.RemoveAllListeners();
            card.actionButton.onClick.AddListener(() => OnCardClicked(captured));
        }

        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }

        RefreshUI();
    }

    public void OpenArmory()
    {
        if (modalRoot != null)
        {
            modalRoot.SetActive(true);
            modalRoot.transform.SetAsLastSibling();
            RefreshUI();
        }
    }

    public void CloseArmory()
    {
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    private void HandleCashChanged(int _) => RefreshUI();

    private void OnCardClicked(ArmoryCardView card)
    {
        if (card == null || card.weaponData == null) return;
        string weaponId = card.weaponData.name;

        if (CampaignWeaponProgress.IsWeaponUnlocked(weaponId))
        {
            CampaignWeaponProgress.EquipWeapon(weaponId);
            RefreshUI();
            return;
        }

        if (!TrySpendCash(card.price, $"Armory {card.displayName}"))
        {
            RefreshUI();
            return;
        }

        CampaignWeaponProgress.UnlockWeapon(weaponId);
        CampaignWeaponProgress.EquipWeapon(weaponId);
        RefreshUI();

        GarageUpgradeController garage = GetComponent<GarageUpgradeController>() ?? FindAnyObjectByType<GarageUpgradeController>();
        if (garage != null) garage.Refresh();
    }

    public void RefreshUI()
    {
        int cash = GetCash();
        string equippedId = CampaignWeaponProgress.GetEquippedWeaponId();

        if (modalCashText != null) modalCashText.text = $"${cash:N0}";
        if (openButtonLabel != null)
        {
            string equippedName = equippedId.Replace("_Data", "").Replace("Piston", "PISTOL").Replace("AR", "AK47").ToUpperInvariant();
            openButtonLabel.text = $"ARMORY\n<size=14><color=#B8E994>EQUIPPED: {equippedName}</color></size>";
        }

        foreach (ArmoryCardView card in bakedCardViews)
        {
            if (card == null || card.weaponData == null || card.actionButton == null) continue;
            string weaponId = card.weaponData.name;
            bool unlocked = CampaignWeaponProgress.IsWeaponUnlocked(weaponId);
            bool equipped = string.Equals(weaponId, equippedId, StringComparison.OrdinalIgnoreCase);

            if (equipped)
            {
                if (card.actionLabel != null)
                {
                    card.actionLabel.text = "EQUIPPED";
                    card.actionLabel.color = new Color(0.75f, 1f, 0.78f);
                }
                if (card.actionBg != null) card.actionBg.color = new Color(0.14f, 0.42f, 0.22f, 1f);
                card.actionButton.interactable = false;
                if (card.cardOutline != null)
                {
                    card.cardOutline.effectColor = new Color(0.32f, 0.94f, 0.48f, 1f);
                    card.cardOutline.effectDistance = new Vector2(3f, -3f);
                }
            }
            else if (unlocked)
            {
                if (card.actionLabel != null)
                {
                    card.actionLabel.text = "EQUIP";
                    card.actionLabel.color = Color.white;
                }
                if (card.actionBg != null) card.actionBg.color = new Color(0.24f, 0.46f, 0.68f, 1f);
                card.actionButton.interactable = true;
                if (card.cardOutline != null)
                {
                    card.cardOutline.effectColor = new Color(0.42f, 0.56f, 0.72f, 1f);
                    card.cardOutline.effectDistance = new Vector2(2f, -2f);
                }
            }
            else
            {
                bool canAfford = cash >= card.price;
                if (card.actionLabel != null)
                {
                    card.actionLabel.text = $"BUY ${card.price:N0}";
                    card.actionLabel.color = canAfford ? new Color(0.1f, 0.07f, 0.02f) : new Color(0.65f, 0.65f, 0.68f);
                }
                if (card.actionBg != null) card.actionBg.color = canAfford ? new Color(0.95f, 0.68f, 0.16f, 1f) : new Color(0.26f, 0.27f, 0.29f, 1f);
                card.actionButton.interactable = canAfford;
                if (card.cardOutline != null)
                {
                    card.cardOutline.effectColor = new Color(0.35f, 0.38f, 0.42f, 0.9f);
                    card.cardOutline.effectDistance = new Vector2(2f, -2f);
                }
            }
        }
    }

    private static int GetCash()
    {
        return GameRewardManager.Instance != null
            ? GameRewardManager.Instance.GetCurrentCash()
            : PlayerPrefs.GetInt(GameRewardManager.PREF_PLAYER_CASH, 2500);
    }

    private static bool TrySpendCash(int amount, string reason)
    {
        if (GameRewardManager.Instance != null)
            return GameRewardManager.Instance.SpendCash(amount, reason);

        int cash = PlayerPrefs.GetInt(GameRewardManager.PREF_PLAYER_CASH, 2500);
        if (cash < amount) return false;
        cash -= amount;
        PlayerPrefs.SetInt(GameRewardManager.PREF_PLAYER_CASH, cash);
        PlayerPrefs.SetInt(GameRewardManager.PREF_TOTAL_COINS, cash);
        PlayerPrefs.Save();
        return true;
    }
}
