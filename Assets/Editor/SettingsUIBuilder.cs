using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// The generated prefab contains ordinary editable uGUI objects, not runtime-drawn UI.
public static class SettingsUIBuilder
{
    public const string PrefabPath = "Assets/GameFolder/SettingsUI.prefab";
    private static readonly Color Panel = new Color(.14f, .16f, .19f);
    private static readonly Color Control = new Color(.25f, .28f, .32f);
    private static readonly Color Accent = new Color(.40f, .72f, .79f);

    [MenuItem("Tools/Game Settings/Create or Rebuild Settings Prefab")]
    public static void Build()
    {
        var root = new GameObject("SettingsUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = .5f;
        // Expand keeps the complete panel visible at narrow and ultrawide aspect ratios.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var menu = root.AddComponent<SettingsMenu>();
        menu.openButton = Button(root.transform, "OpenSettingsButton", "SETTINGS", 0, 0, 150, 48);
        var triggerRect = (RectTransform)menu.openButton.transform;
        triggerRect.anchorMin = triggerRect.anchorMax = new Vector2(1, 1);
        triggerRect.pivot = new Vector2(1, 1); triggerRect.anchoredPosition = new Vector2(-24, -24);
        var overlay = Box(root.transform, "SettingsModal", new Color(0, 0, 0, .65f), 0, 0, 0, 0);
        Stretch((RectTransform)overlay.transform); menu.modal = overlay.gameObject;
        var panel = Box(overlay.transform, "SettingsPanel", Panel, 0, 0, 500, 650);
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(.5f, .5f); panelRect.anchoredPosition = Vector2.zero;
        Label(panel.transform, "Title", "SETTINGS", 28, 18, 350, 46, 30);
        menu.closeButton = Button(panel.transform, "CloseButton", "X", 424, 20, 48, 44);
        Box(panel.transform, "HeaderLine", Accent, 28, 76, 444, 2).raycastTarget = false;
        Label(panel.transform, "AudioHeading", "AUDIO", 28, 89, 400, 30, 21);
        menu.master = Slider(panel.transform, "Master", "Master", 128, 0, 1, out menu.masterValue);
        menu.bgm = Slider(panel.transform, "BGM", "BGM", 178, 0, 1, out menu.bgmValue);
        menu.sfx = Slider(panel.transform, "SFX", "SFX", 228, 0, 1, out menu.sfxValue);
        Label(panel.transform, "DisplayHeading", "DISPLAY", 28, 292, 400, 30, 21);
        Label(panel.transform, "ResolutionTitle", "Resolution", 28, 334, 145, 40);
        menu.previousResolution = Button(panel.transform, "PreviousResolution", "<", 175, 334, 40, 40);
        menu.resolutionLabel = Label(panel.transform, "ResolutionValue", "1920 x 1080", 217, 334, 190, 40);
        menu.resolutionLabel.alignment = TextAlignmentOptions.Center;
        menu.nextResolution = Button(panel.transform, "NextResolution", ">", 409, 334, 40, 40);
        menu.fullscreenButton = Button(panel.transform, "FullscreenToggle", "[  ]  Windowed", 175, 386, 274, 40);
        menu.fullscreenLabel = menu.fullscreenButton.GetComponentInChildren<TMP_Text>();
        Label(panel.transform, "FullscreenTitle", "Screen mode", 28, 386, 145, 40);
        menu.applyDisplayButton = Button(panel.transform, "ApplyDisplayButton", "Apply display", 175, 438, 274, 40);
        Box(panel.transform, "FooterLine", Control, 28, 506, 444, 2).raycastTarget = false;
        menu.resetButton = Button(panel.transform, "ResetAudio", "Reset audio", 28, 522, 150, 40);
        menu.status = Label(panel.transform, "Status", "Esc: close", 28, 576, 444, 52, 17);
        menu.status.textWrappingMode = TextWrappingModes.Normal;
        overlay.gameObject.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("SETTINGS_PREFAB_BUILT");
    }

    public static void Install()
    {
        Build();
        foreach (string name in new[] { "SampleScene", "MainGameScene" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            var existing = Object.FindFirstObjectByType<SettingsMenu>();
            if (name == "SampleScene")
            {
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
            }
            else if (existing == null)
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            EditorSceneManager.SaveScene(scene);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Debug.Log("SETTINGS_INSTALL_PASS");
    }

    private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static Image Box(Transform parent, string name, Color color, float x, float y, float w, float h)
    {
        var rect = Rect(parent, name, x, y, w, h); var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image;
    }
    private static TMP_Text Label(Transform parent, string name, string text, float x, float y, float w, float h, float size = 20)
    {
        var rect = Rect(parent, name, x, y, w, h); var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.text = text; label.fontSize = size; label.color = new Color(.93f, .94f, .96f); label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.NoWrap; return label;
    }
    private static Button Button(Transform parent, string name, string text, float x, float y, float w, float h)
    {
        var box = Box(parent, name, Control, x, y, w, h); var button = box.gameObject.AddComponent<Button>(); button.targetGraphic = box;
        var colors = button.colors; colors.highlightedColor = new Color(.7f, .9f, 1f); colors.pressedColor = Accent; button.colors = colors;
        var label = Label(box.transform, "Label", text, 6, 0, w - 12, h, 18); label.alignment = TextAlignmentOptions.Center;
        return button;
    }
    private static Slider Slider(Transform parent, string name, string title, float y, float min, float max, out TMP_Text value)
    {
        Label(parent, name + "Label", title, 28, y, 140, 36);
        var rect = Rect(parent, name + "Slider", 175, y, 205, 36);
        var background = Box(rect, "Track", Control, 0, 14, 205, 8);
        var fillArea = Rect(rect, "FillArea", 8, 14, 189, 8);
        // Slider drives the anchors, so extra width/height would extend beyond the track.
        var fill = Box(fillArea, "Fill", Accent, 0, 0, 0, 0);
        var handleArea = Rect(rect, "HandleArea", 8, 3, 189, 30);
        var handle = Box(handleArea, "Handle", Color.white, 0, 0, 16, 0);
        var handleRect = (RectTransform)handle.transform; handleRect.pivot = new Vector2(.5f, .5f);
        var slider = rect.gameObject.AddComponent<Slider>(); slider.fillRect = (RectTransform)fill.transform; slider.handleRect = handleRect; slider.targetGraphic = handle;
        slider.minValue = min; slider.maxValue = max; slider.value = 1;
        // Hit target includes the whole slider row, not just the thin track.
        var hit = rect.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        background.raycastTarget = fill.raycastTarget = false;
        value = Label(parent, name + "Value", "100%", 388, y, 75, 36, 18); value.alignment = TextAlignmentOptions.MidlineRight;
        return slider;
    }
}
