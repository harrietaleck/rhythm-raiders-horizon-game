using UnityEditor;
using UnityEngine;

/// <summary>Play-mode helpers for testing the trip/fall/dash stumble.</summary>
public static class StumbleDebugMenu
{
    [MenuItem("Tools/Debug/Drop Trip Obstacle Ahead")]
    static void DropLow() => Drop(1.0f);

    [MenuItem("Tools/Debug/Drop Tall Obstacle Ahead")]
    static void DropTall() => Drop(3.0f);

    static void Drop(float height)
    {
        if (!Application.isPlaying) { Debug.LogWarning("[StumbleTest] enter Play mode first"); return; }
        var pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) return;
        var cc = pm.GetComponent<CharacterController>();
        float feet = pm.transform.position.y - (cc.height * 0.5f - cc.center.y);
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = height > 2f ? "Test Crate Stack" : "Test Crate";
        go.transform.position = new Vector3(pm.transform.position.x, feet + height * 0.5f,
            pm.transform.position.z + Mathf.Max(6f, pm.forwardSpeed * 0.8f));
        go.transform.localScale = new Vector3(1.6f, height, 0.8f);
        go.tag = "HealthDEC";
        go.AddComponent<HealthDecreaseObstacle>();
        Debug.Log($"[StumbleTest] {go.name} placed at z={go.transform.position.z:F1}");
    }

    [MenuItem("Tools/Debug/Log Stumble State")]
    static void LogState()
    {
        var pm = Object.FindAnyObjectByType<PlayerMovement>();
        var g = Object.FindAnyObjectByType<EnemyBase>();
        var pb = pm != null ? pm.GetComponent<PickupBase>() : null;
        var anim = pm != null ? pm.GetComponentInChildren<Animator>() : null;
        string st = anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("Stumble") ? "Stumble" : "other";
        Debug.Log($"[StumbleTest] z={pm?.transform.position.z:F1} stumbling={pm?.IsStumbling} dashing={pm?.IsDashing} " +
                  $"gap={(g != null ? g.Gap : -1):F1} shield={pb?.hitCounter} vibe={pb?.currentHealth} anim={st} started={GameManager.GameStarted}");
    }
}

public static class StumbleDebugHazards
{
    static Vector3 Ahead(out bool ok)
    {
        ok = Application.isPlaying;
        var pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (!ok || pm == null) { ok = false; return Vector3.zero; }
        var cc = pm.GetComponent<CharacterController>();
        float feet = pm.transform.position.y - (cc.height * 0.5f - cc.center.y);
        return new Vector3(pm.transform.position.x, feet, pm.transform.position.z + Mathf.Max(6f, pm.forwardSpeed * 0.8f));
    }

    [MenuItem("Tools/Debug/Drop Low Laser Ahead")]
    static void LowLaser() { var p = Ahead(out bool ok); if (ok) MuseumProps.LowLaser(null, p); }

    [MenuItem("Tools/Debug/Drop Laser Grid Ahead")]
    static void Grid() { var p = Ahead(out bool ok); if (ok) MuseumProps.LaserGrid(null, p); }

    [MenuItem("Tools/Debug/Drop Laser Fence Ahead")]
    static void Fence() { var p = Ahead(out bool ok); if (ok) ClubProps.LaserFence(null, p); }
}


public static class GrenadeDebugMenu
{
    [MenuItem("Tools/Debug/Drop Planted Grenade Ahead")]
    static void Planted()
    {
        if (!Application.isPlaying) return;
        var pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) return;
        var cc = pm.GetComponent<CharacterController>();
        float feet = pm.transform.position.y - (cc.height * 0.5f - cc.center.y);
        MuseumProps.PlantedGrenade(null, new Vector3(pm.transform.position.x, feet, pm.transform.position.z + Mathf.Max(8f, pm.forwardSpeed)));
    }

    [MenuItem("Tools/Debug/Throw Long-Fuse Grenade (visual test)")]
    static void ThrowLong()
    {
        if (!Application.isPlaying) return;
        var pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) return;
        var cc = pm.GetComponent<CharacterController>();
        var p = pm.transform.position;
        float feet = p.y - (cc.height * 0.5f - cc.center.y);
        float landZ = p.z + pm.forwardSpeed * 3.2f;
        GuardGrenade.Throw(new Vector3(8.4f, feet + 1.4f, landZ + 8f), new Vector3(p.x, feet, landZ), 0.9f, 4f);
    }

    [MenuItem("Tools/Debug/Preview Grenade Ring (pauses)")]
    static void PreviewRing()
    {
        if (!Application.isPlaying) return;
        var pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) return;
        var cc = pm.GetComponent<CharacterController>();
        var p = pm.transform.position;
        float feet = p.y - (cc.height * 0.5f - cc.center.y);
        var at = new Vector3(p.x, feet, p.z + 7f);
        BlastRing.Create(at, Grenade.BlastRadius).Fill(0.55f);
        var holder = new GameObject("Preview Grenade");
        holder.transform.position = at;
        var led = Grenade.Build(holder.transform, Vector3.zero, 1f);
        holder.transform.GetChild(0).localRotation = Quaternion.Euler(0f, 30f, 80f);
        holder.transform.GetChild(0).localPosition = new Vector3(0f, 0.12f, 0f);
        MuseumProps.PlantedGrenade(null, new Vector3(p.x + 7f, feet, p.z + 9f));
        EditorApplication.isPaused = true;
    }

    [MenuItem("Tools/Debug/Throw Grenade At Player")]
    static void Throw()
    {
        if (!Application.isPlaying) return;
        var pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) return;
        var cc = pm.GetComponent<CharacterController>();
        var p = pm.transform.position;
        float feet = p.y - (cc.height * 0.5f - cc.center.y);
        float landZ = p.z + pm.forwardSpeed * 2.0f * 0.92f;
        GuardGrenade.Throw(new Vector3(8.4f, feet + 1.4f, landZ + 8f), new Vector3(p.x, feet, landZ), 0.9f, 1.1f);
    }
}

public static class JukeboxDebugMenu
{
    [MenuItem("Tools/Debug/Reset Jukebox And Refund")]
    static void ResetJukebox()
    {
        int refund = 0;
        for (int i = 1; i < ShopData.Music.Length; i++)
        {
            if (!ShopData.OwnsMusic(i)) continue;
            refund += ShopData.Music[i].cost;
            PlayerPrefs.DeleteKey("Shop_MusicOwned_" + i);
        }
        PlayerPrefs.DeleteKey("Shop_Music");
        ShopData.AddNotes(refund);
        Debug.Log($"[Jukebox] Reset - refunded {refund} points, wallet now {ShopData.Notes}");
    }

    [MenuItem("Tools/Debug/Give 200 Points")]
    static void Give() => ShopData.AddNotes(200);

    [MenuItem("Tools/Debug/Log Music State")]
    static void LogState()
    {
        var am = AudioManager.Instance;
        var src = am != null ? am.GetComponent<AudioSource>() : null;
        Debug.Log($"[Jukebox] selected={ShopData.SelectedMusicId} points={ShopData.SpendableNotes} playing={(src != null && src.isPlaying)} clip={(src != null && src.clip != null ? src.clip.name : "none")} rooftop={(am != null && am.rooftopMusic != null ? am.rooftopMusic.name : "none")}");
    }
}

public static class DJStationDebugMenu
{
    [MenuItem("Tools/Debug/Spawn DJ Stations Ahead And Pause")]
    static void Spawn()
    {
        if (!Application.isPlaying) return;
        var pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) return;
        var cc = pm.GetComponent<CharacterController>();
        var p = pm.transform.position;
        float feet = p.y - (cc.height * 0.5f - cc.center.y);
        DJStation.Build(null, new Vector3(7f, feet, p.z + 14f), 1, 7);
        DJStation.Build(null, new Vector3(-7f, feet, p.z + 24f), -1, 11);
        EditorApplication.isPaused = true;
    }

    static bool Ahead(out Vector3 floor)
    {
        floor = Vector3.zero;
        if (!Application.isPlaying) return false;
        var pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) return false;
        var cc = pm.GetComponent<CharacterController>();
        var p = pm.transform.position;
        floor = new Vector3(0f, p.y - (cc.height * 0.5f - cc.center.y), p.z);
        return true;
    }

    [MenuItem("Tools/Debug/Spawn Centre DJ Stage Ahead")]
    static void Centre()
    {
        if (!Ahead(out var f)) return;
        // Guard on the right; the runner is moved into that lane to test the taser.
        DJStation.BuildCenter(null, f + Vector3.forward * 26f, 1, 3);
        var pm = Object.FindAnyObjectByType<PlayerMovement>();
        var cc = pm.GetComponent<CharacterController>();
        cc.enabled = false;
        pm.transform.position = new Vector3(4f, pm.transform.position.y, pm.transform.position.z);
        cc.enabled = true;
    }

    [MenuItem("Tools/Debug/Release Smoke Ahead And Pause")]
    static void Smoke()
    {
        if (!Ahead(out var f)) return;
        SmokeCloud.Spawn(f + Vector3.forward * 10f, new Vector3(7f, 2.4f, 7f), 2.6f, Color.white, new Vector3(-1f, 0f, -0.6f));
        EditorApplication.delayCall += () => EditorApplication.delayCall += () => { if (Application.isPlaying) EditorApplication.isPaused = true; };
    }

    [MenuItem("Tools/Debug/Drop Disco Ball Ahead")]
    static void Disco()
    {
        if (!Ahead(out var f)) return;
        var pm = Object.FindAnyObjectByType<PlayerMovement>();
        ClubProps.DiscoBallDrop(null, new Vector3(pm.transform.position.x, f.y, f.z + 30f));
    }
}

