using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives GuideScene: a How To Play page and a Credits page.
/// </summary>
public class GuideSceneUI : MonoBehaviour
{
    public const string SceneName = "GuideScene";

    /// <summary>Set before loading GuideScene to open on the Credits page.</summary>
    public static bool OpenCredits;

    public GameObject guidePanel;
    public GameObject creditsPanel;
    public Button     creditsButton;
    public Button     guideButton;
    public Button     guideMenuButton;
    public Button     creditsMenuButton;
    public string     menuSceneName = "StartScene";

    [Header("How To Play pages")]
    public GameObject[] pages;
    public Button[]     tabButtons;
    int _page;

    private void Start()
    {
        UICursor.UnlockForMenu();
        Time.timeScale = 1f;
        UIInputFix.EnsureEventSystem();

        Wire(creditsButton,     ShowCredits);
        Wire(guideButton,       ShowGuide);
        Wire(guideMenuButton,   OnMainMenu);
        Wire(creditsMenuButton, OnMainMenu);
        if (tabButtons != null)
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int index = i;
                Wire(tabButtons[i], () => ShowPage(index));
            }
        ShowPage(0);

        if (OpenCredits) ShowCredits();
        else             ShowGuide();
        OpenCredits = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            OnMainMenu();
        if (pages != null && pages.Length > 0 && guidePanel != null && guidePanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) ShowPage((_page + 1) % pages.Length);
            if (Input.GetKeyDown(KeyCode.LeftArrow))  ShowPage((_page + pages.Length - 1) % pages.Length);
        }
    }

    public void ShowGuide()
    {
        if (guidePanel != null)   guidePanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    /// <summary>Shows one How To Play page and highlights its tab.</summary>
    public void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0) return;
        _page = Mathf.Clamp(index, 0, pages.Length - 1);
        for (int i = 0; i < pages.Length; i++)
            if (pages[i] != null) pages[i].SetActive(i == _page);
        if (tabButtons == null) return;
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;
            var img = tabButtons[i].GetComponent<Image>();
            if (img != null) img.color = i == _page ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }
    }

    public void ShowCredits()
    {
        if (guidePanel != null)   guidePanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void OnMainMenu()
    {
        StartSceneUI.OpenDirectlyToMenu = true;
        SceneFader.LoadScene(menuSceneName);
    }

    private static void Wire(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        UIButtonRaycastFix.Apply(btn);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }
}
