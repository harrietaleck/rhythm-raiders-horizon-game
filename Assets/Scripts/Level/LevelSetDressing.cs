using UnityEngine;

/// <summary>
/// Spawns museum display cases (L2) and neon club strips (L3) along the runner lanes.
/// </summary>
public class LevelSetDressing : MonoBehaviour
{
    Transform _player;
    Transform _root;
    int _appliedLevel;

    public void Apply(LevelDefinition def)
    {
        if (def == null || _appliedLevel == def.levelNumber) return;
        _appliedLevel = def.levelNumber;

        if (_root != null)
            Object.Destroy(_root.gameObject);

        _root = new GameObject("LevelSetDressing").transform;

        var playerGo = GameObject.FindGameObjectWithTag("Player");
        _player = playerGo != null ? playerGo.transform : null;
        float startZ = _player != null ? _player.position.z : 0f;

        switch (def.levelNumber)
        {
            case 2:
                if (FindAnyObjectByType<MuseumHallStream>() == null)
                    BuildMuseumDisplays(startZ);
                break;
            case 3:
                if (FindAnyObjectByType<ClubHallStream>() == null)
                    BuildClubNeon(startZ);
                break;
        }
    }

    void BuildMuseumDisplays(float startZ)
    {
        Material pedestalMat = CreateLitMaterial(new Color(0.35f, 0.32f, 0.28f));
        Material glassMat = CreateLitMaterial(new Color(0.55f, 0.65f, 0.75f, 0.35f));
        Material goldMat = CreateLitMaterial(new Color(0.85f, 0.72f, 0.25f));

        for (int i = 0; i < 12; i++)
        {
            float z = startZ + 12f + i * 16f;
            float side = (i % 2 == 0) ? -1f : 1f;
            float x = side * Random.Range(7.5f, 9.5f);

            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = $"MuseumPedestal_{i}";
            pedestal.transform.SetParent(_root, false);
            pedestal.transform.position = new Vector3(x, 0.55f, z);
            pedestal.transform.localScale = new Vector3(1.8f, 1.1f, 1.4f);
            ApplyMat(pedestal, pedestalMat);
            DestroyCollider(pedestal);

            var glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "DisplayCase";
            glass.transform.SetParent(pedestal.transform, false);
            glass.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            glass.transform.localScale = new Vector3(0.92f, 1.1f, 0.92f);
            ApplyMat(glass, glassMat);
            DestroyCollider(glass);

            var artifact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            artifact.name = "DisplayArtifact";
            artifact.transform.SetParent(pedestal.transform, false);
            artifact.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            artifact.transform.localScale = Vector3.one * 0.45f;
            ApplyMat(artifact, goldMat);
            DestroyCollider(artifact);

            var spotlight = new GameObject("Spot").AddComponent<Light>();
            spotlight.transform.SetParent(pedestal.transform, false);
            spotlight.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            spotlight.type = LightType.Spot;
            spotlight.range = 6f;
            spotlight.spotAngle = 45f;
            spotlight.intensity = 1.8f;
            spotlight.color = new Color(1f, 0.95f, 0.8f);
            spotlight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    void BuildClubNeon(float startZ)
    {
        Color[] neonColors =
        {
            new Color(1f, 0.1f, 0.85f),
            new Color(0.1f, 0.95f, 1f),
            new Color(0.55f, 0.15f, 1f),
            new Color(1f, 0.95f, 0.2f)
        };

        for (int i = 0; i < 16; i++)
        {
            float z = startZ + 8f + i * 14f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * Random.Range(8f, 10.5f);
                Color c = neonColors[i % neonColors.Length];

                var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"NeonStrip_{i}_{side}";
                strip.transform.SetParent(_root, false);
                strip.transform.position = new Vector3(x, Random.Range(2f, 4.5f), z);
                strip.transform.localScale = new Vector3(0.15f, Random.Range(2.5f, 4f), 0.15f);
                ApplyMat(strip, CreateLitMaterial(c, emissive: true));
                DestroyCollider(strip);

                var light = new GameObject("NeonLight").AddComponent<Light>();
                light.transform.SetParent(strip.transform, false);
                light.transform.localPosition = Vector3.zero;
                light.type = LightType.Point;
                light.range = 8f;
                light.intensity = 2.8f;
                light.color = c;
            }
        }
    }

    static Material CreateLitMaterial(Color color, bool emissive = false)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                     ?? Shader.Find("Standard");
        var mat = new Material(shader);
        if (color.a < 1f)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }
        mat.color = color;
        if (emissive)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.2f);
        }
        return mat;
    }

    static void ApplyMat(GameObject go, Material mat)
    {
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    static void DestroyCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
    }
}
