using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates SplashScene with the Rhythm Raiders splash image and SceneFader.
/// Run via Tools → Setup Splash Scene, then Tools → Setup Build Scenes.
/// </summary>
public class SetupSplashScene
{
    private const string ScenePath = "Assets/Scenes/SplashScene.unity";
    private const string LogoPath = "Assets/UI/Splash/logo_horizon.jpg";
    private const string TitlePath = "Assets/UI/Splash/title_horizon.jpg";

    [MenuItem("Tools/Rebuild Splash Scene")]
    public static void Rebuild()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            AssetDatabase.DeleteAsset(ScenePath);

        Setup();
    }

    [MenuItem("Tools/Setup Splash Scene")]
    public static void Setup()
    {
        EnsureSplashTextureImport(LogoPath);
        EnsureSplashTextureImport(TitlePath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "SplashScene";

        EnsureSplashCamera();

        var canvas = EnsureCanvas("SplashCanvas");
        EnsureFader(canvas);

        var splashRoot = new GameObject("SplashRoot");
        splashRoot.transform.SetParent(canvas.transform, false);
        var rootRt = splashRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        var logo = CreateFullImage(splashRoot.transform, "LogoImage", LogoPath);
        var title = CreateFullImage(splashRoot.transform, "TitleImage", TitlePath);
        title.gameObject.SetActive(false);

        var controller = canvas.AddComponent<SplashScreenController>();
        controller.splashRoot = rootRt;
        controller.logoImage = logo;
        controller.titleImage = title;
        controller.splashImage = title;
        controller.displayDuration = 2.4f;
        controller.nextSceneName = "StartScene";

        EditorSceneManager.SaveScene(scene, ScenePath);

        var canvasRt = canvas.GetComponent<RectTransform>();
        if (canvasRt != null)
        {
            canvasRt.localScale = Vector3.one;
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.offsetMin = Vector2.zero;
            canvasRt.offsetMax = Vector2.zero;
            canvasRt.pivot = new Vector2(0.5f, 0.5f);
        }

        EditorUtility.SetDirty(canvas);
        Debug.Log($"[SetupSplash] Saved {ScenePath}");
    }

    private static Image CreateFullImage(Transform parent, string name, string spritePath)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = name == "LogoImage";
        img.color = Color.white;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite != null) img.sprite = sprite;
        else Debug.LogWarning("[SetupSplash] Missing sprite " + spritePath);
        return img;
    }

    private static void EnsureSplashTextureImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[SetupSplash] Texture missing: {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }

    private static GameObject EnsureCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;

        return go;
    }

    private static void EnsureSplashCamera()
    {
        var existing = GameObject.Find("Main Camera");
        if (existing != null)
        {
            var cam = existing.GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
            return;
        }

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var camera = camGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();
    }

    private static void EnsureFader(GameObject canvas)
    {
        if (canvas.transform.Find("SceneFader") != null) return;

        var faderGO = new GameObject("SceneFader");
        faderGO.transform.SetParent(canvas.transform, false);
        faderGO.AddComponent<SceneFader>();
    }
}
