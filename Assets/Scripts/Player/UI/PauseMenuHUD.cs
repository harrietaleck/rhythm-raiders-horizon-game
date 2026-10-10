using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full pause menu with resume, settings, and main menu actions.
/// Used in MainGameL1 (Escape) and StartScene (menu PAUSE button).
/// </summary>
public class PauseMenuHUD : MonoBehaviour
{
    [Header("Pause panel")]
    public GameObject pausePanel;
    public Button     resumeButton;
    public Button     settingsButton;
    public Button     mainMenuButton;
    public Button     restartButton;
    public Button     controlsButton;

    [Header("Level info")]
    public TextMeshProUGUI levelTitleText;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI progressText;

    [Header("Controls overlay")]
    public GameObject controlsPanel;
    public Button     controlsCloseButton;

    [Header("Settings overlay")]
    public GameObject settingsPanel;
    public Button     settingsCloseButton;
    public Slider     musicVolumeSlider;
    public Slider     sfxVolumeSlider;

    [Header("Behaviour")]
    public string mainMenuSceneName = "StartScene";
    public bool pauseTimeOnOpen     = true;
    public bool listenForEscape     = true;
    public bool requireGameStarted  = false;
    public bool showMainMenuButton  = true;

    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Start()
    {
        // GraphicRaycaster is required for button clicks to register.
        // If the editor tool was not re-run after this component was added, add it now.
        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        if (pausePanel != null)    pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        WireButton(resumeButton,        OnResume);
        WireButton(settingsButton,      OnOpenSettings);
        WireButton(mainMenuButton,      OnMainMenu);
        WireButton(restartButton,       OnRestart);
        WireButton(controlsButton,      OnOpenControls);
        WireButton(settingsCloseButton, OnCloseSettings);
        WireButton(controlsCloseButton, OnCloseControls);

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(showMainMenuButton);
        if (restartButton != null)
            restartButton.gameObject.SetActive(showMainMenuButton);

        BindVolumeSliders();
        UIInputFix.EnsureEventSystem();
    }

    private void OnDisable()
    {
        if (_isOpen && pauseTimeOnOpen)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }

    private void Update()
    {
        if (!listenForEscape || !Input.GetKeyDown(KeyCode.Escape))
            return;

        if (requireGameStarted && !GameManager.GameStarted)
            return;

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            OnCloseSettings();
            return;
        }

        if (controlsPanel != null && controlsPanel.activeSelf)
        {
            OnCloseControls();
            return;
        }

        if (_isOpen)
            Hide();
        else if (!requireGameStarted || GameManager.GameStarted)
            Show();
    }

    public void Show()
    {
        if (pausePanel == null) return;

        _isOpen = true;
        UICursor.UnlockForMenu();
        UIInputFix.EnsureEventSystem();

        if (pausePanel != null)
            GameplayCanvasGuard.EnsureCanvasScale(pausePanel);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (pauseTimeOnOpen)
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        RefreshLevelInfo();
        pausePanel.SetActive(true);
        pausePanel.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        _isOpen = false;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (pauseTimeOnOpen)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }

    public void Toggle()
    {
        if (_isOpen) Hide();
        else         Show();
    }

    public void OnResume()
    {
        Hide();
    }

    public void OnOpenSettings()
    {
        if (settingsPanel == null) return;

        UIInputFix.EnsureEventSystem();
        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();
    }

    public void OnCloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OnOpenControls()
    {
        if (controlsPanel == null) return;

        UIInputFix.EnsureEventSystem();
        controlsPanel.SetActive(true);
        controlsPanel.transform.SetAsLastSibling();
    }

    public void OnCloseControls()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
    }

    public void OnRestart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneFader.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void RefreshLevelInfo()
    {
        var def = LevelCatalog.Get(LevelProgress.CurrentLevel);
        if (levelTitleText != null) levelTitleText.text = $"LEVEL {def.levelNumber}";
        if (levelNameText  != null) levelNameText.text  = def.displayName.ToUpperInvariant();
        if (objectiveText  != null) objectiveText.text  = def.startHint1;

        if (progressText != null)
        {
            var pickup = FindAnyObjectByType<PickupBase>();
            int collected = pickup != null ? pickup.artifactAmount : 0;
            progressText.text = $"{collected} / {PickupBase.ArtifactsToWin}";
        }
    }

    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        StartSceneUI.OpenDirectlyToMenu = true;
        SceneFader.LoadScene(mainMenuSceneName);
    }

    private void BindVolumeSliders()
    {
        if (AudioManager.Instance == null) return;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.musicVolume);
            musicVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetMusicVolume(v));
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.sfxVolume);
            sfxVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetSfxVolume(v));
        }
    }

    private static void WireButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null || action == null) return;
        UIButtonRaycastFix.Apply(btn);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }
}
