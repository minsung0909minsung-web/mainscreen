using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// -batchmode -executeMethod SettingsVerification.Run (graphics enabled for preview)
[InitializeOnLoad]
public static class SettingsVerification
{
    private const string Active = "SettingsVerification.Active";
    private static int stage, ticks;
    private static double deadline;
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static SettingsMenu menu;
    private static Vector2 buttonPosition;
    private static Camera previewCamera;
    private static RenderTexture previewTarget;
    static SettingsVerification()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active, false)) return;
            stage = ticks = 0; deadline = EditorApplication.timeSinceStartup + 90;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        };
    }
    public static void Run()
    {
        SessionState.SetBool("SettingsVerification.HadPrefs", PlayerPrefs.HasKey(GameSettings.PreferenceKey));
        SessionState.SetString("SettingsVerification.Prefs", PlayerPrefs.GetString(GameSettings.PreferenceKey, ""));
        PlayerPrefs.DeleteKey(GameSettings.PreferenceKey);
        try
        {
            foreach (string name in new[] { "SampleScene", "LoadingScene", "MainGameScene" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                var settings = UnityEngine.Object.FindFirstObjectByType<SettingsMenu>();
                Require((settings != null) == (name == "MainGameScene"), "Incorrect settings placement in " + name);
                if (settings != null) Require(settings.GetComponentsInChildren<UnityEngine.UI.Slider>(true).Length == 3, "Only three audio sliders should remain");
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var node in root.GetComponentsInChildren<Transform>(true))
                        Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) == 0, "Missing script on " + node.name);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Active, true); EditorApplication.EnterPlaymode();
        }
        catch (Exception error) { Finish(error); }
    }
    public static void RebuildAndRun() { SettingsUIBuilder.Install(); Run(); }
    private static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying || keyboard == null) return;
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Timeout at " + stage);
            if (++ticks < 8) return;
            ticks = 0;
            switch (stage++)
            {
                case 0:
                    Require(UnityEngine.Object.FindFirstObjectByType<SettingsMenu>() == null, "Start screen must not show settings");
                    SceneManager.LoadScene("MainGameScene");
                    break;
                case 1:
                    menu = UnityEngine.Object.FindFirstObjectByType<SettingsMenu>();
                    Require(menu != null && !menu.IsOpen, "Game menu must begin closed");
                    Canvas.ForceUpdateCanvases();
                    buttonPosition = RectTransformUtility.WorldToScreenPoint(null, menu.openButton.transform.TransformPoint(((RectTransform)menu.openButton.transform).rect.center));
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPosition });
                    break;
                case 2:
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPosition });
                    InputSystem.Update();
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPosition }.WithButton(MouseButton.Left));
                    break;
                case 3:
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPosition });
                    break;
                case 4:
                    Require(menu.IsOpen && SceneManager.GetActiveScene().name == "MainGameScene", "Click must open in-game settings");
                    Require(Time.timeScale == 0, "Game must pause");
                    menu.master.value = .35f; menu.bgm.value = .45f; menu.sfx.value = .55f;
                    Require(Mathf.Abs(AudioListener.volume - .35f) < .001f, "Master volume not applied");
                    var audio = new GameObject("TestSFX", typeof(AudioSource), typeof(SettingsAudioChannel));
                    Require(Mathf.Abs(audio.GetComponent<AudioSource>().volume - .55f) < .001f, "SFX volume not applied");
                    UnityEngine.Object.Destroy(audio);
                    menu.nextResolution.onClick.Invoke(); menu.fullscreenButton.onClick.Invoke(); menu.ApplyDisplay();
                    PreparePreview(menu, 1280, 800);
                    break;
                case 5:
                    SavePreview(menu, "Logs/SettingsPreview.png");
                    PreparePreview(menu, 800, 600);
                    break;
                case 6:
                    SavePreview(menu, "Logs/SettingsPreview-800x600.png");
                    menu.Close(); Require(Time.timeScale == 1, "Pause not restored");
                    SceneManager.LoadScene("MainGameScene"); break;
                case 7:
                    menu = UnityEngine.Object.FindFirstObjectByType<SettingsMenu>();
                    menu.Open();
                    Require(Mathf.Abs(menu.master.value - .35f) < .001f, "Values lost on scene reload");
                    string json = PlayerPrefs.GetString(GameSettings.PreferenceKey);
                    var saved = JsonUtility.FromJson<GameSettings.Values>(json);
                    Require(Mathf.Abs(saved.bgm - .45f) < .001f, "Preferences not saved");
                    Require(!json.Contains("sensitivity") && !json.Contains("bindings"), "Removed control settings still saved");
                    menu.resetButton.onClick.Invoke();
                    Require(GameSettings.Current.master == 1f && GameSettings.Current.bgm == 1f && GameSettings.Current.sfx == 1f, "Audio reset failed");
                    break;
                case 8: InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); break;
                case 9:
                    Require(!menu.IsOpen && Time.timeScale == 1, "Esc did not close menu");
                    Debug.Log("SETTINGS_ALL_PASS: game-only placement, three audio sliders, real button click, pause, volume, display, persistence, audio reset, Esc, preview");
                    Finish(null); break;
            }
        }
        catch (Exception error) { Finish(error); }
    }
    private static void PreparePreview(SettingsMenu settings, int width, int height)
    {
        var canvas = settings.GetComponent<Canvas>();
        var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
        previewCamera = cameraObject.GetComponent<Camera>(); previewCamera.clearFlags = CameraClearFlags.SolidColor; previewCamera.backgroundColor = new Color(.08f,.09f,.11f);
        previewTarget = new RenderTexture(width, height, 24); previewCamera.targetTexture = previewTarget;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = previewCamera; canvas.planeDistance = 1;
        Canvas.ForceUpdateCanvases();
    }
    private static void SavePreview(SettingsMenu settings, string path)
    {
        var canvas = settings.GetComponent<Canvas>();
        foreach (var label in settings.modal.GetComponentsInChildren<TMP_Text>())
        {
            label.ForceMeshUpdate();
            Require(!label.isTextOverflowing, "Text overflow: " + label.name);
        }
        foreach (var slider in settings.modal.GetComponentsInChildren<UnityEngine.UI.Slider>())
        {
            var fillParent = (RectTransform)slider.fillRect.parent;
            Require(slider.fillRect.rect.width <= fillParent.rect.width + .01f, "Slider fill exceeds track");
            Require(slider.handleRect.rect.height <= 30.01f, "Slider handle exceeds row");
        }
        previewCamera.Render();
        var previous = RenderTexture.active; RenderTexture.active = previewTarget;
        var texture = new Texture2D(previewTarget.width, previewTarget.height, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, previewTarget.width, previewTarget.height), 0, 0); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG()); RenderTexture.active = previous;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
        previewCamera.targetTexture = null; UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(previewTarget); UnityEngine.Object.DestroyImmediate(previewCamera.gameObject);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Finish(Exception error)
    {
        SessionState.SetBool(Active, false);
        if (menu != null && menu.IsOpen) menu.Close();
        if (SessionState.GetBool("SettingsVerification.HadPrefs", false)) PlayerPrefs.SetString(GameSettings.PreferenceKey, SessionState.GetString("SettingsVerification.Prefs", ""));
        else PlayerPrefs.DeleteKey(GameSettings.PreferenceKey);
        PlayerPrefs.Save();
        if (error != null) Debug.LogException(error);
        EditorApplication.Exit(error == null ? 0 : 1);
    }
}
