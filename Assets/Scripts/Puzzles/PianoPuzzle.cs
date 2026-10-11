using UnityEngine;
using System.Collections.Generic;
using System.Text;
using TMPro;

public class PianoPuzzle : MonoBehaviour
{
    public TMP_Text patternText;
    public TMP_Text inputText;
    public TMP_Text resultText;
    public TMP_Text timerText;

    public GameObject[] doors;
    private int doorIndex = 0;

    private float timeLimit = 5f;
    private float timer = 0f;

    private bool roundActive = false;
    private int count = 3;
    private float roundDelayTimer = 0f;
    private bool waitingForRound = true;

    private KeyCode[] keys =
    {
        KeyCode.T,
        KeyCode.Y,
        KeyCode.U,
        KeyCode.I,
        KeyCode.O
    };

    private List<KeyCode> pattern = new List<KeyCode>();
    private List<KeyCode> input = new List<KeyCode>();
    private int index = 0;

    private void Awake()
    {
        ApplyPuzzleLayout();
    }

    void Start()
    {
        waitingForRound = true;
        roundDelayTimer = 0f;
    }

    void Update()
    {
        if (!GameManager.GameStarted)
            return;

        HandleRoundDelay();
        HandleTimer();

        if (!roundActive) return;

        CheckInput(KeyCode.T);
        CheckInput(KeyCode.Y);
        CheckInput(KeyCode.U);
        CheckInput(KeyCode.I);
        CheckInput(KeyCode.O);
    }

    public void StartRound()
    {
        pattern.Clear();
        input.Clear();

        index = 0;
        timer = 0f;

        roundActive = true;
        waitingForRound = false;

        resultText.text = "";
        inputText.text = "You: ";

        for (int i = 0; i < count; i++)
            pattern.Add(keys[Random.Range(0, keys.Length)]);

        ShowPattern();
    }

    void ShowPattern()
    {
        if (patternText == null) return;
        patternText.text = "Pattern:\n" + FormatKeySequence(pattern);
    }

    void CheckInput(KeyCode key)
    {
        if (!Input.GetKeyDown(key)) return;

        input.Add(key);
        inputText.text = "You: " + FormatKeySequence(input);

        if (key != pattern[index])
        {
            FailRound("ALARM! WRONG KEY");
            return;
        }

        index++;

        if (index >= pattern.Count)
            WinRound();
    }

    void HandleTimer()
    {
        if (!roundActive) return;

        timer += Time.deltaTime;
        timerText.text = "Time: " + Mathf.Max(0f, timeLimit - timer).ToString("F1");

        if (timer >= timeLimit)
            FailRound("TIME UP!");
    }

    void WinRound()
    {
        resultText.text = "DOOR OPENED!";
        roundActive = false;

        OpenNextDoor();
        count++;

        waitingForRound = true;
        roundDelayTimer = 0f;
    }

    void FailRound(string message)
    {
        resultText.text = message;
        roundActive = false;
        ShowPattern();

        Invoke(nameof(StartFailDelay), 2f);
    }

    void StartFailDelay()
    {
        waitingForRound = true;
        roundDelayTimer = 0f;
        resultText.text = "";
        inputText.text = "You: ";
    }

    void OpenNextDoor()
    {
        if (doorIndex >= doors.Length) return;

        doors[doorIndex].SetActive(false);
        doorIndex++;
    }

    void HandleRoundDelay()
    {
        if (!waitingForRound) return;

        roundDelayTimer += Time.deltaTime;
        timerText.text = "Next round: " + Mathf.Max(0f, 10f - roundDelayTimer).ToString("F1");

        if (roundDelayTimer >= 10f)
        {
            waitingForRound = false;
            roundDelayTimer = 0f;
            StartRound();
        }
    }

    private static string FormatKeySequence(IReadOnlyList<KeyCode> sequence)
    {
        if (sequence == null || sequence.Count == 0)
            return "—";

        var sb = new StringBuilder();
        for (int i = 0; i < sequence.Count; i++)
        {
            if (i > 0)
                sb.Append("   ·   ");
            sb.Append(KeyLabel(sequence[i]));
        }

        return sb.ToString();
    }

    private static string KeyLabel(KeyCode key)
    {
        string label = key.ToString();
        return label.Length == 1 ? label : label;
    }

    private void ApplyPuzzleLayout()
    {
        StyleField(patternText, new Vector2(0.03f, 0.76f), new Vector2(0.44f, 0.92f), 34);
        StyleField(inputText, new Vector2(0.03f, 0.60f), new Vector2(0.44f, 0.74f), 30);
        StyleField(resultText, new Vector2(0.03f, 0.46f), new Vector2(0.44f, 0.58f), 28);
        StyleField(timerText, new Vector2(0.03f, 0.34f), new Vector2(0.44f, 0.44f), 26);
    }

    private static void StyleField(TMP_Text field, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
    {
        if (field == null) return;

        var rt = field.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        field.fontSize = fontSize;
        field.fontStyle = FontStyles.Bold;
        field.alignment = TextAlignmentOptions.TopLeft;
        field.textWrappingMode = TextWrappingModes.Normal;
        field.overflowMode = TextOverflowModes.Overflow;
        field.raycastTarget = false;
        field.color = Color.white;
    }
}
