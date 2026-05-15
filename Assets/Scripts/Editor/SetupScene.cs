using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// One-shot scene wiring: countdown UI, game-over UI, animation controllers,
/// and all GameManager references.  Run via Tools/Setup Scene References.
/// </summary>
public class SetupScene
{
    [MenuItem("Tools/Setup Scene References")]
    public static void Setup()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();

        // ── Countdown text ─────────────────────────────────────────────────
        GameObject ctGO = GetOrCreateUIText(canvas, "CountdownText");
        if (ctGO != null)
        {
            ApplyUITextLayout(ctGO, "5", 120, Color.yellow, new Vector2(0f, 50f), new Vector2(500f, 250f));
            ctGO.SetActive(true);
        }

        // ── Game Over text ─────────────────────────────────────────────────
        GameObject goGO = GetOrCreateUIText(canvas, "GameOverText");
        if (goGO != null)
        {
            ApplyUITextLayout(goGO, "GAME OVER", 100, Color.red, new Vector2(0f, 0f), new Vector2(700f, 200f));
            goGO.SetActive(false);   // hidden until triggered
        }

        // ── PlayerAnimationController ─────────────────────────────────────
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            if (player.GetComponent<PlayerAnimationController>() == null)
                player.AddComponent<PlayerAnimationController>();
            EditorUtility.SetDirty(player);
            Debug.Log("[SetupScene] PlayerAnimationController ensured on Player.");
        }

        // ── EnemyAnimationController ──────────────────────────────────────
        GameObject enemy = GameObject.Find("Enemy");
        if (enemy != null)
        {
            if (enemy.GetComponent<EnemyAnimationController>() == null)
                enemy.AddComponent<EnemyAnimationController>();
            EditorUtility.SetDirty(enemy);
            Debug.Log("[SetupScene] EnemyAnimationController ensured on Enemy.");
        }

        // ── Wire GameManager ──────────────────────────────────────────────
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning("[SetupScene] GameManager not found in scene.");
        }
        else
        {
            if (ctGO != null)  gm.countdownText = ctGO.GetComponent<TextMeshProUGUI>();
            if (goGO != null)  gm.gameOverText  = goGO.GetComponent<TextMeshProUGUI>();

            if (player != null)
            {
                if (gm.playerMovement == null)
                    gm.playerMovement = player.GetComponent<PlayerMovement>();

                if (gm.playerAnimator == null)
                {
                    Transform ch = player.transform.Find("Ch03_nonPBR");
                    gm.playerAnimator = ch != null
                        ? ch.GetComponent<Animator>()
                        : player.GetComponentInChildren<Animator>();
                }
            }

            if (enemy != null)
            {
                if (gm.enemyBase == null)
                    gm.enemyBase = enemy.GetComponent<EnemyBase>();

                if (gm.enemyAnimator == null)
                {
                    Transform ch = enemy.transform.Find("Standing Aim Idle 02 Looking");
                    gm.enemyAnimator = ch != null
                        ? ch.GetComponent<Animator>()
                        : enemy.GetComponentInChildren<Animator>();
                }
            }

            EditorUtility.SetDirty(gm);
            Debug.Log($"[SetupScene] GameManager wired — " +
                      $"countdown:{gm.countdownText != null} " +
                      $"gameOver:{gm.gameOverText != null} " +
                      $"playerMovement:{gm.playerMovement != null} " +
                      $"enemyBase:{gm.enemyBase != null} " +
                      $"playerAnim:{gm.playerAnimator != null} " +
                      $"enemyAnim:{gm.enemyAnimator != null}");
        }

        // ── Save ──────────────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SetupScene] Scene saved.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static GameObject GetOrCreateUIText(Canvas canvas, string name)
    {
        // Try to find existing child first
        if (canvas != null)
        {
            Transform existing = canvas.transform.Find(name);
            if (existing != null) return existing.gameObject;
        }

        // Try searching the scene
        GameObject found = GameObject.Find("Canvas/" + name);
        if (found != null) return found;

        // Create new under Canvas
        if (canvas == null) { Debug.LogWarning($"[SetupScene] Canvas not found, can't create {name}."); return null; }

        GameObject go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);
        go.AddComponent<TMPro.TextMeshProUGUI>();
        EditorUtility.SetDirty(go);
        Debug.Log($"[SetupScene] Created {name} under Canvas.");
        return go;
    }

    static void ApplyUITextLayout(GameObject go, string defaultText, float fontSize, Color color,
                                   Vector2 anchoredPos, Vector2 sizeDelta)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
        }

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text         = defaultText;
            tmp.fontSize     = fontSize;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.color        = color;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Color.black;
            tmp.fontStyle    = FontStyles.Bold;
        }

        EditorUtility.SetDirty(go);
    }
}
