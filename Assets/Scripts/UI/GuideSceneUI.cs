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

    private void Start()
    {
        UICursor.UnlockForMenu();
        Time.timeScale = 1f;
        UIInputFix.EnsureEventSystem();

        Wire(creditsButton,     ShowCredits);
        Wire(guideButton,       ShowGuide);
        Wire(guideMenuButton,   OnMainMenu);
        Wire(creditsMenuButton, OnMainMenu);

        if (OpenCredits) ShowCredits();
        else             ShowGuide();
        OpenCredits = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            OnMainMenu();
    }

    public void ShowGuide()
    {
        if (guidePanel != null)   guidePanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
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
