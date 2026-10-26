using UnityEngine;

/// <summary>Spawns copies of the level's guard character as scenery (bouncers, bodyguards).</summary>
public static class GuardModels
{
    static GameObject _source;
    static float _scale = 1f;

    static GameObject Source()
    {
        if (_source != null) return _source;
        var enemy = Object.FindAnyObjectByType<EnemyBase>();
        if (enemy == null) return null;
        var anim = enemy.GetComponentInChildren<Animator>(true);
        if (anim == null) return null;
        _source = anim.gameObject;
        _scale = anim.transform.lossyScale.y;
        return _source;
    }

    /// <summary>A static guard standing with his feet at <paramref name="feet"/>, facing <paramref name="facing"/>.</summary>
    public static Transform Spawn(Transform parent, Vector3 feet, Vector3 facing, string name = "Bouncer")
    {
        // Prefer the imported DJ bodyguard character.
        var bg = DanceFloorCharacters.SpawnBodyguard(parent, feet, facing);
        if (bg != null)
        {
            bg.name = name;
            if (name == "Bouncer") DanceFloorCharacters.AttachSidearm(bg);   // armed like the museum guards
            return bg;
        }

        var root = new GameObject(name).transform;
        root.SetParent(parent, true);
        root.position = feet;
        if (facing.sqrMagnitude > 0.001f) root.rotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.z));

        var src = Source();
        if (src == null)
        {
            // Fallback figure.
            var mat = RooftopProps.Mat(new Color(0.08f, 0.08f, 0.1f));
            RooftopProps.Part(PrimitiveType.Capsule, root, new Vector3(0f, 0.95f, 0f), new Vector3(0.6f, 0.9f, 0.45f), mat, false);
            RooftopProps.Part(PrimitiveType.Sphere, root, new Vector3(0f, 1.95f, 0f), Vector3.one * 0.38f, mat, false);
            return root;
        }

        var model = Object.Instantiate(src, root);
        model.name = "Model";
        foreach (var mb in model.GetComponentsInChildren<MonoBehaviour>(true)) Object.Destroy(mb);
        foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        foreach (var rb in model.GetComponentsInChildren<Rigidbody>(true)) Object.Destroy(rb);
        foreach (var cc in model.GetComponentsInChildren<CharacterController>(true)) Object.Destroy(cc);
        model.SetActive(true);
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * _scale;
        model.transform.localPosition = Vector3.zero;
        var rs = model.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            model.transform.position += Vector3.up * (feet.y - b.min.y);
        }
        var anim = model.GetComponent<Animator>();
        if (anim != null) anim.SetFloat("Speed", 0f);
        return root;
    }
}
