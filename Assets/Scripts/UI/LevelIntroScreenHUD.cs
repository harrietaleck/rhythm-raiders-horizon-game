using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Level 2 / Level 3 intro briefing screens.
/// After Level 1 victory → Level2Intro → Level2Museum.
/// </summary>
public class LevelIntroScreenHUD : MonoBehaviour
{
    public int levelNumber = 2;
    public string gameplaySceneName = "Level2Museum";

    [Header("UI")]
    public Button continueButton;
    public Button nextButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;

    [Header("Narration")]
    public string speakerName = "Narrator";

    private LevelDefinition _def;

    private void Awake()
    {
        GameplayCanvasGuard.EnsureCanvasScale(gameObject);
    }

    private void Start()
    {
        UICursor.UnlockForMenu();
        Time.timeScale = 1f;
        UIInputFix.EnsureEventSystem();

        _def = LevelCatalog.Get(levelNumber);
        if (!string.IsNullOrEmpty(_def.gameplayScene))
            gameplaySceneName = _def.gameplayScene;

        if (titleText != null)
        {
            titleText.text = _def.briefingTitle;
            TextFx.Use(titleText, "wave");
        }
        if (subtitleText != null)
            subtitleText.text = _def.codename;

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.onClick.AddListener(OnContinue);
            UIButtonRaycastFix.Apply(continueButton);
        }

        LevelProgress.SetLevel(levelNumber);
        StartCoroutine(BeginBriefing());
    }

    IEnumerator BeginBriefing()
    {
        yield return NarrationManager.WaitForSceneFade();

        if (_def.introCutscene != LevelCutsceneType.None)
            yield return LevelCutscenePlayer.PlayAndWait(_def.introCutscene);

        var lines = new[]
        {
            new NarrationLine(_def.briefingLine1, speakerName),
            new NarrationLine(_def.briefingLine2, speakerName),
            new NarrationLine($"Objective: collect {_def.artifactsToWin} artifacts and escape.", speakerName)
        };

        if (NarrationManager.Instance != null)
        {
            var nm = NarrationManager.Instance;
            nm.autoAdvance = true;
            nm.autoDelay = 3f;
            nm.advanceButton = nm.GetAdvanceButton(nextButton ?? continueButton);
            nm.Play(lines, OnBriefingComplete);
        }
        else
        {
            OnBriefingComplete();
        }
    }

    void OnBriefingComplete()
    {
        if (continueButton != null)
        {
            UIButtonRaycastFix.BringToFront(continueButton);
            continueButton.gameObject.SetActive(true);
        }
    }

    public void OnContinue()
    {
        LevelProgress.SetLevel(levelNumber);
        SceneFader.LoadScene(gameplaySceneName);
    }
}
