using UnityEngine;

/// <summary>
/// Ensures skinned character meshes use URP Lit materials for every submesh at runtime.
/// </summary>
public class CharacterMaterialApplier : MonoBehaviour
{
    [SerializeField] Material primary;
    [SerializeField] Material secondary;

    void Awake() => Apply();
    void Start() => Apply();

    void Apply()
    {
        if (primary == null) return;

        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh == null) continue;

            int count = Mathf.Max(smr.sharedMesh.subMeshCount, 1);
            var mats = new Material[count];
            for (int i = 0; i < count; i++)
            {
                Material source = (i == 0) ? primary : (secondary != null ? secondary : primary);
                Material instance = new Material(source);
                instance.DisableKeyword("_NORMALMAP");
                instance.DisableKeyword("_EMISSION");
                if (instance.HasProperty("_BumpMap"))
                    instance.SetTexture("_BumpMap", null);
                if (instance.HasProperty("_EmissionMap"))
                    instance.SetTexture("_EmissionMap", null);
                mats[i] = instance;
            }

            smr.materials = mats;
        }
    }
}
