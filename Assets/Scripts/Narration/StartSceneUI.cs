using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the Start scene.
/// Flow:
///   1. Splash → StartScene fades in → narrative intro (auto typewriter)
///   2. After narration → main menu (Play, Pause, Settings)
///   3. PLAY → MainGameL1 countdown (no narrator between menu and countdown)
/// </summary>
public class StartSceneUI : MonoBehaviour
{
    /// <summary>Set before loading StartScene to skip intro narration and open the main menu.</summary>
    public static bool OpenDirectlyToMenu;

    [Header("Scene to load")]
    public string gameSceneName = "MainGameL1Opera";

    [Header("Menu")]
    public GameObject menuPanel;
    public Button playButton;
    public Button level2Button;
    public Button level3Button;
    public Button narrativeButton;   // NEW — opens the story narration from the menu
    public Button pauseButton;
    public Button settingsButton;

    [Header("Overlays")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public Button pauseResumeButton;
    public Button settingsCloseButton;

    [Header("Settings")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Legacy (editor wiring)")]
    public Button nextButton;
    public PauseMenuHUD pauseMenuHud;

    [Header("Narration lines")]
    [TextArea(2, 5)]
    public string[] narrationLines = new string[]
    {
        "Welcome to Rhythm Raiders: Beat Horizon!",
        "You are Pulse — a rhythm thief infiltrating venues controlled by the Conductors.",
        "Steal two musical artifacts per level. Outrun the guard. Restore the city's beat.",
        "Level 1: The Rooftops. Level 2: The Music Museum. Level 3: The Underground Club. Good luck!"
    };

    public string speakerName = "";

    private bool _menuPaused;

    private void Start()
    {
        UICursor.UnlockForMenu();
        Time.timeScale = 1f;
        AudioListener.pause = false;

        EnsureCanvasLayout();
        UIInputFix.EnsureEventSystem();
        if (pauseMenuHud == null) pauseMenuHud = GetComponent<PauseMenuHUD>();

        if (menuPanel != null) menuPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        WireButton(playButton,     OnPlay);
        WireButton(level2Button,   OnPlayLevel2);
        WireButton(level3Button,   OnPlayLevel3);
        WireButton(narrativeButton, OnNarrative);
        WireButton(pauseButton,    OnPause);
        WireButton(settingsButton, OnSettings);
        WireButton(pauseResumeButton,    OnResume);
        WireButton(settingsCloseButton,  OnCloseSettings);

        if (musicVolumeSlider != null && AudioManager.Instance != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.musicVolume);
            musicVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetMusicVolume(v));
        }

        if (sfxVolumeSlider != null && AudioManager.Instance != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.sfxVolume);
            sfxVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetSfxVolume(v));
        }

        StartCoroutine(BeginIntro());
    }

    IEnumerator BeginIntro()
    {
        yield return NarrationManager.WaitForSceneFade();

        // Always go straight to the main menu.
        // The narrative is accessible via the NARRATIVE button on the menu.
        OpenDirectlyToMenu = false;
        NarrationManager.Instance?.Cancel();
        OnNarrationComplete();
    }

    /// <summary>NARRATIVE button — plays the story intro, then returns to menu.</summary>
    public void OnNarrative()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        AudioManager.Instance?.PlayNarrative();
        var lines = BuildLines();
        if (NarrationManager.Instance != null)
        {
            var nm = NarrationManager.Instance;
            nm.autoAdvance   = true;
            nm.autoDelay     = 2.5f;
            nm.advanceButton = nm.GetAdvanceButton(nextButton);
            nm.Play(lines, OnNarrationComplete);
        }
        else
        {
            OnNarrationComplete();
        }
    }

    public void OnPlay()
    {
        LoadGameplayLevel(1);
    }

    public void OnPlayLevel2()
    {
        LoadGameplayLevel(2);
    }

    public void OnPlayLevel3()
    {
        LoadGameplayLevel(3);
    }

    void LoadGameplayLevel(int level)
    {
        LevelProgress.SetLevel(level);
        if (_menuPaused) OnResume();
        AudioManager.Instance?.StopNarrative();
        SceneFader.LoadScene(LevelCatalog.GetGameplayScene(level));
    }

    public void OnPause()
    {
        if (pauseMenuHud != null)
        {
            pauseMenuHud.Show();
            return;
        }

        _menuPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OnResume()
    {
        if (pauseMenuHud != null)
        {
            pauseMenuHud.Hide();
            return;
        }

        _menuPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void OnSettings()
    {
        UIInputFix.EnsureEventSystem();
        // Hide the menu panel so buttons don't bleed through behind settings
        if (menuPanel != null) menuPanel.SetActive(false);

        if (pauseMenuHud != null && settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
            return;
        }

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
        }
    }

    public void OnCloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        // Restore the menu panel after closing settings
        if (menuPanel != null) menuPanel.SetActive(true);

        if (pauseMenuHud != null)
            pauseMenuHud.OnCloseSettings();
    }

    private void OnNarrationComplete()
    {
        AudioManager.Instance?.StopNarrative();
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    private void EnsureCanvasLayout()
    {
        var rt = GetComponent<RectTransform>();
        if (rt != null)
            rt.localScale = Vector3.one;
    }

    private static void WireButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null || action == null) return;
        UIButtonRaycastFix.Apply(btn);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    private NarrationLine[] BuildLines()
    {
        var result = new NarrationLine[narrationLines.Length];
        for (int i = 0; i < narrationLines.Length; i++)
            result[i] = new NarrationLine(narrationLines[i], speakerName);
        return result;
    }
}
