using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Fixes pink character models by creating URP Lit materials with diffuse/normal maps
/// and applying them to embedded .dae materials and scene renderers.
/// </summary>
public static class FixCharacterURPMaterials
{
    const string MatFolder = "Assets/Materials/Characters";

    const string Ch03Dae = "Assets/Animations/PlayerAnimations/idle/Ch03_nonPBR.dae";
    const string Ch15Dae = "Assets/Animations/SecurityGuardAnimations/Standing Aim Idle 02 Looking.dae";

    [MenuItem("Tools/Dump Character Renderer Materials")]
    public static void DumpRendererMaterials()
    {
        foreach (string rootName in new[] { "Player", "Enemy" })
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null) continue;

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                Mesh mesh = GetSharedMesh(r);
                string matList = string.Join(" | ", System.Array.ConvertAll(
                    r.sharedMaterials,
                    m => m == null ? "NULL" : $"{m.name} ({m.shader?.name})"));
                Debug.Log($"[Dump] {GetPath(r.transform)} — enabled={r.enabled}, subMeshes={mesh?.subMeshCount ?? 0}, materials={r.sharedMaterials.Length}: {matList}");
            }
        }
    }

    [MenuItem("Tools/Fix Character Pink Materials")]
    public static void FixAll()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[FixCharacterURP] Universal Render Pipeline/Lit shader not found.");
            return;
        }

        EnsureFolder();

        Material ch03 = SaveMaterial("Ch03_Body",
            "Assets/Animations/PlayerAnimations/idle/textures/Ch03_1001_Diffuse.png",
            urpLit);

        Material ch15Body = SaveMaterial("Ch15_body",
            "Assets/Animations/SecurityGuardAnimations/textures/Ch15_1001_Diffuse.png",
            urpLit);

        Material ch15Body1 = SaveMaterial("Ch15_body1",
            "Assets/Animations/SecurityGuardAnimations/textures/Ch15_1002_Diffuse.png",
            urpLit);

        int embedded = ApplyToEmbedded(Ch03Dae, ch03, urpLit);
        embedded += ApplyToEmbedded(Ch15Dae, ch15Body, ch15Body1, urpLit);

        int sceneFixed = FixSceneCharacterRenderers(ch03, ch15Body, ch15Body1, urpLit);
        int appliers = AttachRuntimeAppliers(ch03, ch15Body, ch15Body1);
        int disabled = DisablePlaceholderMeshRenderers();

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(Ch03Dae, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(Ch15Dae, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[FixCharacterURP] Done — upgraded {embedded} embedded material(s), fixed {sceneFixed} renderer(s), attached {appliers} runtime applier(s), disabled {disabled} placeholder renderer(s).");
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MatFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "Characters");
    }

    static Material SaveMaterial(string name, string diffusePath, Shader urpLit)
    {
        string matPath = $"{MatFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(urpLit) { name = name };
            AssetDatabase.CreateAsset(mat, matPath);
        }

        ApplyTextures(mat, urpLit, AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath));
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void ApplyTextures(Material mat, Shader urpLit, Texture2D diffuse)
    {
        mat.shader = urpLit;

        if (diffuse != null)
        {
            mat.SetTexture("_BaseMap", diffuse);
            mat.SetColor("_BaseColor", Color.white);
        }

        // Mixamo normal maps contain magenta placeholder texels on hair/gear UV islands.
        mat.DisableKeyword("_NORMALMAP");
        mat.DisableKeyword("_EMISSION");
        if (mat.HasProperty("_BumpMap"))
            mat.SetTexture("_BumpMap", null);
        if (mat.HasProperty("_EmissionMap"))
            mat.SetTexture("_EmissionMap", null);

        mat.SetFloat("_Smoothness", 0.35f);
        mat.SetFloat("_Metallic", 0f);
    }

    static int ApplyToEmbedded(string daePath, Material singleMat, Shader urpLit)
    {
        int count = 0;
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(daePath))
        {
            if (obj is not Material mat) continue;
            ApplyTextures(mat, urpLit, singleMat.GetTexture("_BaseMap") as Texture2D);
            EditorUtility.SetDirty(mat);
            count++;
        }
        return count;
    }

    static int ApplyToEmbedded(string daePath, Material bodyMat, Material body1Mat, Shader urpLit)
    {
        int count = 0;
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(daePath))
        {
            if (obj is not Material mat) continue;

            bool second = mat.name.Contains("1") || mat.name.Contains("1002");
            Material source = second ? body1Mat : bodyMat;
            ApplyTextures(mat, urpLit, source.GetTexture("_BaseMap") as Texture2D);
            EditorUtility.SetDirty(mat);
            count++;
        }
        return count;
    }

    static int AttachRuntimeAppliers(Material ch03, Material ch15Body, Material ch15Body1)
    {
        int count = 0;
        Transform playerModel = GameObject.Find("Player")?.transform.Find("Ch03_nonPBR");
        if (playerModel != null && AttachApplier(playerModel.gameObject, ch03, null))
            count++;

        Transform guardModel = GameObject.Find("Enemy")?.transform.Find("Standing Aim Idle 02 Looking");
        if (guardModel != null && AttachApplier(guardModel.gameObject, ch15Body, ch15Body1))
            count++;

        return count;
    }

    static bool AttachApplier(GameObject target, Material primary, Material secondary)
    {
        if (primary == null) return false;

        CharacterMaterialApplier applier = target.GetComponent<CharacterMaterialApplier>();
        if (applier == null)
            applier = target.AddComponent<CharacterMaterialApplier>();

        SerializedObject so = new SerializedObject(applier);
        so.FindProperty("primary").objectReferenceValue = primary;
        so.FindProperty("secondary").objectReferenceValue = secondary;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(applier);
        return true;
    }

    static int DisablePlaceholderMeshRenderers()
    {
        int count = 0;
        foreach (string rootName in new[] { "Player", "Enemy" })
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null) continue;

            MeshFilter mf = root.GetComponent<MeshFilter>();
            MeshRenderer mr = root.GetComponent<MeshRenderer>();
            if (mr == null || mf == null) continue;
            if (mf.sharedMesh == null || mf.sharedMesh.name != "Capsule") continue;

            if (mr.enabled)
            {
                mr.enabled = false;
                EditorUtility.SetDirty(mr);
                count++;
            }
        }
        return count;
    }

    static int FixSceneCharacterRenderers(Material ch03, Material ch15Body, Material ch15Body1, Shader urpLit)
    {
        int count = 0;

        GameObject player = GameObject.Find("Player");
        if (player != null)
            count += FixRenderersUnder(player.transform, ch03, ch03, urpLit);

        GameObject enemy = GameObject.Find("Enemy");
        if (enemy != null)
            count += FixRenderersUnder(enemy.transform, ch15Body, ch15Body1, urpLit);

        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            string n = r.gameObject.name;
            if (!n.Contains("Ch03") && !n.Contains("Ch15") && !n.Contains("nonPBR") && !n.Contains("Standing Aim"))
                continue;
            if (IsUnderCharacterRoot(r.transform)) continue;
            if (AssignCharacterMaterials(r, ch03, ch15Body, ch15Body1, urpLit))
                count++;
        }

        return count;
    }

    static bool IsUnderCharacterRoot(Transform t)
    {
        while (t != null)
        {
            if (t.name == "Player" || t.name == "Enemy") return true;
            t = t.parent;
        }
        return false;
    }

    static int FixRenderersUnder(Transform root, Material slot0, Material slot1, Shader urpLit)
    {
        int count = 0;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (AssignSlots(r, slot0, slot1, urpLit))
                count++;
        }
        return count;
    }

    static bool AssignCharacterMaterials(Renderer r, Material ch03, Material ch15Body, Material ch15Body1, Shader urpLit)
    {
        string n = r.gameObject.name;
        if (n.Contains("Ch03") || n.Contains("nonPBR"))
            return AssignSlots(r, ch03, ch03, urpLit);
        if (n.Contains("Ch15") || n.Contains("Standing Aim"))
            return AssignSlots(r, ch15Body, ch15Body1, urpLit);
        return false;
    }

    static bool AssignSlots(Renderer r, Material slot0, Material slot1, Shader urpLit)
    {
        if (slot0 == null) return false;

        int subMeshCount = 1;
        Mesh sharedMesh = GetSharedMesh(r);
        if (sharedMesh != null)
            subMeshCount = sharedMesh.subMeshCount;

        Material[] mats = r.sharedMaterials;
        int needed = Mathf.Max(mats.Length, subMeshCount, 1);
        if (mats.Length < needed)
        {
            var resized = new Material[needed];
            for (int i = 0; i < needed; i++)
                resized[i] = i < mats.Length ? mats[i] : null;
            mats = resized;
        }

        bool changed = mats.Length != r.sharedMaterials.Length;
        for (int i = 0; i < mats.Length; i++)
        {
            Material target = (i == 0) ? slot0 : (slot1 != null ? slot1 : slot0);
            if (mats[i] == target && !NeedsMaterialFix(mats[i], urpLit)) continue;
            mats[i] = target;
            changed = true;
        }

        if (changed)
        {
            r.sharedMaterials = mats;
            EditorUtility.SetDirty(r);
        }

        return changed;
    }

    static Mesh GetSharedMesh(Renderer r)
    {
        if (r is SkinnedMeshRenderer smr) return smr.sharedMesh;
        return r.GetComponent<MeshFilter>()?.sharedMesh;
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    static bool NeedsMaterialFix(Material mat, Shader urpLit)
    {
        if (mat == null) return true;
        if (mat.shader == null) return true;
        string name = mat.shader.name;
        if (name.Contains("InternalError") || name.Contains("Hidden/InternalError")) return true;
        return mat.shader != urpLit;
    }
}
