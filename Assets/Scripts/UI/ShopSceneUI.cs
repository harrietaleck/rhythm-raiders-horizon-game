using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives ShopScene: spend banked notes on upgrades and trails, and shows today's challenge.
/// </summary>
public class ShopSceneUI : MonoBehaviour
{
    public const string SceneName = "ShopScene";

    public TextMeshProUGUI walletText;
    public TextMeshProUGUI dailyText;
    public TextMeshProUGUI statusText;

    public TextMeshProUGUI[] upgradeInfo;
    public Button[]          upgradeButtons;
    public TextMeshProUGUI[] upgradeButtonLabels;

    public Button[]          trailButtons;
    public TextMeshProUGUI[] trailButtonLabels;

    public Button menuButton;
    public string menuSceneName = "StartScene";

    float _statusUntil;

    void Start()
    {
        UICursor.UnlockForMenu();
        Time.timeScale = 1f;
        UIInputFix.EnsureEventSystem();

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            var u = (ShopData.Upgrade)i;
            Wire(upgradeButtons[i], () => BuyUpgrade(u));
        }
        for (int i = 0; i < trailButtons.Length; i++)
        {
            int index = i;
            Wire(trailButtons[i], () => SelectTrail(index));
        }
        Wire(menuButton, OnMainMenu);
        Refresh();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) OnMainMenu();
        if (statusText != null && _statusUntil > 0f && Time.time > _statusUntil)
        {
            statusText.text = "";
            _statusUntil = 0f;
        }
    }

    void BuyUpgrade(ShopData.Upgrade u)
    {
        var info = ShopData.Upgrades[(int)u];
        if (ShopData.NextCost(u) < 0) Status(info.name + " is maxed out");
        else if (ShopData.BuyUpgrade(u)) Status(info.name + " upgraded!");
        else Status("Not enough notes");
        Refresh();
    }

    void SelectTrail(int i)
    {
        bool owned = ShopData.OwnsTrail(i);
        if (ShopData.SelectTrail(i)) Status((owned ? "Equipped " : "Bought ") + ShopData.Trails[i].name);
        else Status("Not enough notes");
        Refresh();
    }

    void Refresh()
    {
        if (walletText != null) walletText.text = $"NOTES  <color=#FFD54A>{ShopData.Notes}</color>";

        for (int i = 0; i < upgradeInfo.Length && i < ShopData.Upgrades.Length; i++)
        {
            var u = (ShopData.Upgrade)i;
            var info = ShopData.Upgrades[i];
            int lvl = ShopData.Level(u);
            string pips = $"<color=#FFD54A>LV {lvl}/{ShopData.MaxLevel}</color>";
            upgradeInfo[i].text = $"<color=#5AD9FF>{info.name}</color>  {pips}\n<size=75%><color=#9FB8D1>{info.description} (+{info.perLevel}{info.unit} per level)</color></size>";

            int cost = ShopData.NextCost(u);
            upgradeButtonLabels[i].text = cost < 0 ? "MAX" : cost.ToString();
            upgradeButtons[i].interactable = cost >= 0;
        }

        for (int i = 0; i < trailButtonLabels.Length && i < ShopData.Trails.Length; i++)
        {
            var t = ShopData.Trails[i];
            string state = ShopData.EquippedTrail == i ? "EQUIPPED" : ShopData.OwnsTrail(i) ? "EQUIP" : t.cost.ToString();
            trailButtonLabels[i].text = $"{t.name}\n<size=70%>{state}</size>";
        }

        if (dailyText != null)
        {
            string streak = DailyChallenge.Streak > 0 ? $"   STREAK {DailyChallenge.Streak}" : "";
            dailyText.text = DailyChallenge.CompletedToday
                ? $"<color=#5AD9FF>DAILY CHALLENGE</color>  Complete! Come back tomorrow.{streak}"
                : $"<color=#5AD9FF>DAILY CHALLENGE</color>  {DailyChallenge.Description} on Level 1   <color=#FFD54A>+{DailyChallenge.RewardNotes}</color>{streak}";
        }
    }

    void Status(string message)
    {
        if (statusText == null) return;
        statusText.text = message;
        _statusUntil = Time.time + 2f;
    }

    public void OnMainMenu()
    {
        StartSceneUI.OpenDirectlyToMenu = true;
        SceneFader.LoadScene(menuSceneName);
    }

    static void Wire(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        UIButtonRaycastFix.Apply(btn);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }
}
