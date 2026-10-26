using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DanceFloorDebugMenu
{
    [MenuItem("Tools/Debug/Log Dance Floor Assets")]
    static void LogAssets()
    {
        var sb = new System.Text.StringBuilder("[DanceFloor] ");
        foreach (var p in Directory.GetFiles("Assets/Animations/characters", "*.fbx")
                     .Concat(Directory.GetFiles("Assets/Animations/dancefloorcharacterAnimations", "*.fbx")))
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(p);
            var av = all.OfType<Avatar>().FirstOrDefault();
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            var shaders = go == null ? "" : string.Join(",", go.GetComponentsInChildren<Renderer>(true)
                .SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.shader.name).Distinct());
            var clip = all.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
            sb.AppendLine($"{Path.GetFileName(p)} human={(av != null && av.isValid && av.isHuman)} clip={(clip != null ? clip.name + " " + clip.length.ToString("0.0") + "s" : "-")} shaders={shaders}");
        }
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Debug/Spawn Dancers Ahead")]
    static void SpawnAhead()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) { Debug.LogWarning("Play mode only"); return; }
        var z = p.transform.position.z + 9f;
        for (int i = 0; i < 6; i++)
        {
            int side = i % 2 == 0 ? -1 : 1;
            DanceFloorCharacters.SpawnDancer(null, new Vector3(side * (3f + i * 0.4f), p.transform.position.y - 0.05f, z + i), new Vector3(-side, 0, -1));
        }
        var bg = DanceFloorCharacters.SpawnBodyguard(null, new Vector3(0f, p.transform.position.y, z + 7f), Vector3.back);
        var dj = DanceFloorCharacters.SpawnDJ(null, new Vector3(1.5f, p.transform.position.y, z + 7f), Vector3.back);
        Debug.Log($"[DanceFloor] spawned dancers, bodyguard={(bg != null)} dj={(dj != null)}");
    }

    [MenuItem("Tools/Debug/Log Spawned Dancers")]
    static void LogSpawned()
    {
        var sb = new System.Text.StringBuilder("[DanceFloor] spawned:\n");
        int n = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name != "Dancer" && t.name != "DJ Character" && t.name != "Bodyguard Character" && t.name != "Bouncer" && t.name != "Bodyguard") continue;
            if (n++ > 12) break;
            var a = t.GetComponentInChildren<Animator>();
            var rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds();
            foreach (var r in rs) b.Encapsulate(r.bounds);
            string st = a == null ? "no animator" :
                $"ctrl={(a.runtimeAnimatorController ? a.runtimeAnimatorController.name : "null")} avatar={(a.avatar ? a.avatar.name + (a.avatar.isHuman ? "(H)" : "(G)") : "null")} enabled={a.enabled} state={a.GetCurrentAnimatorStateInfo(0).normalizedTime:0.00} clips={a.GetCurrentAnimatorClipInfoCount(0)}";
            sb.AppendLine($"{t.name} pos={t.position} size={b.size} renderers={rs.Length} {st}");
        }
        Debug.Log(sb.ToString());
    }

    static int _frameIndex;
    static bool _crewOnly, _statueOnly;
    [MenuItem("Tools/Debug/Frame Next Statue")]
    static void FrameStatue() { _statueOnly = true; FrameNext(); _statueOnly = false; }
    [MenuItem("Tools/Debug/Frame Next DJ Or Guard")]
    static void FrameCrew() { _crewOnly = true; FrameNext(); _crewOnly = false; }

    [MenuItem("Tools/Debug/Frame Next Character")]
    static void FrameNext()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        var cam = Camera.main;
        if (p == null || cam == null) return;
        var list = new System.Collections.Generic.List<Transform>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (((t.name == "Dancer" && !_crewOnly) || t.name == "DJ Character" || t.name == "Bodyguard Character" || t.name == "Bouncer" || t.name == "Bodyguard"
                 || (!_crewOnly && (t.name.StartsWith("Statue (") || t.name == "Painting"))) && t.position.z > p.transform.position.z)
                list.Add(t);
        if (list.Count == 0) { Debug.Log("[DanceFloor] nothing ahead"); return; }
        if (_statueOnly) list.RemoveAll(t => !t.name.StartsWith("Statue ("));
        if (list.Count == 0) return;
        list.Sort((a, b) => a.position.z.CompareTo(b.position.z));
        var target = list[_frameIndex++ % list.Count];
        float k = target.name.StartsWith("Painting") || target.name.StartsWith("Statue") ? 1f : DanceFloorCharacters.RunnerHeight() / 1.75f;
        var look = target.position + Vector3.up * 1.0f * k;
        var side = target.position.x > 0 ? -1f : 1f;
        cam.transform.position = look + new Vector3(side * 3.2f, 0.5f, -2.2f) * k * 1.3f;
        cam.transform.LookAt(look);
        Debug.Log($"[DanceFloor] framed {target.name} at {target.position}");
    }

    [MenuItem("Tools/Debug/Drop Roof Gap Ahead")]
    static void DropGap()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        var c = new Vector3(0f, 0f, p.transform.position.z + 16f);
        var gap = RooftopProps.Gap(null, c, 5f, true, 4f);
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(5f, 4.5f, c.z - 9f);
            cam.transform.LookAt(c + Vector3.up * 0.5f);
        }
        Debug.Log("[Debug] roof gap at " + c);
    }

    [MenuItem("Tools/Debug/Drop Statue Ahead")]
    static void DropStatue()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        var pos = new Vector3(p.transform.position.x, 0f, p.transform.position.z + 14f);
        var go = MuseumProps.Statue(null, pos);
        Debug.Log("[Debug] dropped " + go.name + " at " + pos);
    }

    [MenuItem("Tools/Debug/Frame Runner Backpack")]
    static void FrameBag()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        var cam = Camera.main;
        if (p == null || cam == null) return;
        Transform bag = null;
        foreach (var t in p.GetComponentsInChildren<Transform>(true)) if (t.name == "Runner Backpack") bag = t;
        if (bag == null) { Debug.Log("[Debug] no backpack"); return; }
        var anim = p.GetComponentInChildren<Animator>();
        var fwd = anim != null ? anim.transform.forward : p.transform.forward;
        var look = bag.position;
        cam.transform.position = look - fwd * 2.4f + Vector3.up * 0.6f + Vector3.Cross(Vector3.up, fwd) * 1.2f;
        cam.transform.LookAt(look);
    }

    [MenuItem("Tools/Debug/Log Cameras In Imported Models")]
    static void LogModelCameras()
    {
        var lib = FxLibrary.Get();
        var sb = new System.Text.StringBuilder("[Models] cameras/lights:\n");
        void Check(GameObject g)
        {
            if (g == null) return;
            sb.AppendLine($"{g.name}: cameras={g.GetComponentsInChildren<Camera>(true).Length} lights={g.GetComponentsInChildren<Light>(true).Length} listeners={g.GetComponentsInChildren<AudioListener>(true).Length}");
        }
        if (lib != null)
        {
            if (lib.dancers != null) foreach (var d in lib.dancers) Check(d);
            Check(lib.dj); Check(lib.bodyguard); Check(lib.backpack);
            if (lib.statues != null) foreach (var d in lib.statues) Check(d);
            if (lib.paintings != null) foreach (var d in lib.paintings) Check(d);
            if (lib.discoBalls != null) foreach (var d in lib.discoBalls) Check(d);
            Check(lib.fogPrefab);
        }
        sb.AppendLine("Scene cameras: ");
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sb.AppendLine($"  {c.name} enabled={c.enabled} active={c.gameObject.activeInHierarchy} depth={c.depth} target={(c.targetTexture != null)}");
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Debug/Line Up Runner With Bodyguard And DJ")]
    static void LineUp()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        UnityEditor.EditorApplication.isPaused = true;
        var anim = p.GetComponentInChildren<Animator>();
        var fwd = anim != null ? anim.transform.forward : Vector3.forward;
        var right = Vector3.Cross(Vector3.up, fwd);
        float floor = p.GetComponent<CharacterController>() != null ? p.GetComponent<CharacterController>().bounds.min.y : 0f;
        var basePos = new Vector3(p.transform.position.x, floor, p.transform.position.z);
        DanceFloorCharacters.SpawnBodyguard(null, basePos + right * 1.6f, -right);
        DanceFloorCharacters.SpawnDJ(null, basePos + right * 3.2f, -right);
        DanceFloorCharacters.SpawnDancer(null, basePos + right * 4.8f, -right);
        var cam = Camera.main;
        float h = DanceFloorCharacters.RunnerHeight();
        cam.transform.position = basePos + right * 2.4f - fwd * (h * 2.6f) + Vector3.up * (h * 0.6f);
        cam.transform.LookAt(basePos + right * 2.4f + Vector3.up * (h * 0.5f));
        Debug.Log($"[Debug] runner height {h:0.00}");
    }

    [MenuItem("Tools/Debug/Drop Dancer In Lane Ahead")]
    static void DancerAhead()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        var cc = p.GetComponent<CharacterController>();
        float floor = cc != null ? cc.bounds.min.y : 0f;
        DanceFloorCharacters.SpawnDancer(null, new Vector3(p.transform.position.x, floor, p.transform.position.z + 12f), Vector3.back);
    }

    [MenuItem("Tools/Debug/Drop Bodyguard In Lane Ahead")]
    static void GuardAhead()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        var cc = p.GetComponent<CharacterController>();
        float floor = cc != null ? cc.bounds.min.y : 0f;
        DanceFloorCharacters.SpawnBodyguard(null, new Vector3(p.transform.position.x, floor, p.transform.position.z + 12f), Vector3.back);
    }

    [MenuItem("Tools/Debug/Preview Game Over Card")]
    static void PreviewLose() => GameOverOverlay.ShowResult("GAME OVER", "You were caught by the guard!", "Distance: 367m   |   Fragments: 1   |   $500", false);

    [MenuItem("Tools/Debug/Preview Win Card")]
    static void PreviewWin() => GameOverOverlay.ShowResult("LEVEL COMPLETE!", "YOU ESCAPED WITH THE ARTIFACTS", "Distance: 812m   |   Fragments: 2   |   $1,200", true);

    [MenuItem("Tools/Debug/Hide Result Card")]
    static void HideCard() => GameOverOverlay.Hide();
}
