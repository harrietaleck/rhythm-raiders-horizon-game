using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the Rhythm Raiders splash for a fixed duration, then loads StartScene.
/// </summary>
public class SplashScreenController : MonoBehaviour
{
    [Header("Timing")]
    public float displayDuration = 3f;

    [Header("Scene")]
    public string nextSceneName = "StartScene";

    [Header("UI")]
    public RectTransform splashRoot;
    public Image splashImage;
    public Image logoImage;
    public Image titleImage;
    public Image brandImage;

    [Header("Animation")]
    public float pulseScale = 1.02f;
    public float pulseSpeed = 1.2f;

    private bool _waitingForTap;

    private void Awake()
    {
        EnsureCanvasLayout();
    }

    private void Start()
    {
        UICursor.UnlockForMenu();
        Time.timeScale = 1f;
        EnsureCanvasLayout();
        StartCoroutine(RunSplash());
    }

    private void Update()
    {
        if (!_waitingForTap) return;
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            SceneFader.LoadScene(nextSceneName);
    }

    private void EnsureCanvasLayout()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;
        rt.localScale = Vector3.one;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private IEnumerator RunSplash()
    {
        if (splashRoot != null)
            splashRoot.gameObject.SetActive(true);

        if (logoImage != null) logoImage.gameObject.SetActive(true);
        if (brandImage != null) brandImage.gameObject.SetActive(false);
        if (titleImage != null) titleImage.gameObject.SetActive(false);

        yield return Hold(displayDuration);

        if (brandImage != null)
        {
            if (logoImage != null) logoImage.gameObject.SetActive(false);
            brandImage.gameObject.SetActive(true);
            yield return Hold(displayDuration);
            brandImage.gameObject.SetActive(false);
        }
        else if (logoImage != null)
        {
            logoImage.gameObject.SetActive(false);
        }
        if (titleImage != null)
        {
            titleImage.gameObject.SetActive(true);
            if (splashRoot != null) splashRoot.localScale = Vector3.one;
        }
        else if (splashImage != null && titleImage == null)
        {
            _waitingForTap = true;
            yield break;
        }

        _waitingForTap = true;
    }

    private IEnumerator Hold(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            AnimatePulse(elapsed);
            yield return null;
        }
    }

    private void AnimatePulse(float time)
    {
        if (splashRoot == null) return;
        float t = (Mathf.Sin(time * pulseSpeed) + 1f) * 0.5f;
        float scale = Mathf.Lerp(1f, pulseScale, t);
        splashRoot.localScale = new Vector3(scale, scale, 1f);
    }
}
