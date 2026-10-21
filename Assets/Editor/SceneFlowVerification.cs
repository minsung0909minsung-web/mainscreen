using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

// Batch verification: -executeMethod SceneFlowVerification.Run
[InitializeOnLoad]
public static class SceneFlowVerification
{
    private const string ActiveKey = "SceneFlowVerification.Active";
    private static int stage;
    private static int ticks;
    private static bool mouseTest;
    private static double deadline;
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static readonly HashSet<string> penguinFramesSeen = new HashSet<string>();

    static SceneFlowVerification()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static void Run()
    {
        try
        {
            string[] names = { "SampleScene", "LoadingScene", "MainGameScene" };
            string[] panels = { "StartPanel", "LoadingPanel", "MainGamePanel" };
            for (int index = 0; index < names.Length; index++)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + names[index] + ".unity");
                int managers = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    managers += root.GetComponentsInChildren<GameManager>(true).Length;
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                            throw new Exception("Missing script: " + transform.name);
                        for (int other = 0; other < panels.Length; other++)
                            if (other != index && transform.name == panels[other])
                                throw new Exception("Unexpected panel: " + panels[other]);
                    }
                }
                if (managers != (index == 2 ? 0 : 1)) throw new Exception("Unexpected GameManager count");
                if (GameObject.Find(panels[index]) == null) throw new Exception("Missing screen content");
                if (index == 1) VerifyPenguinLayout();
                Debug.Log("SCENE_FLOW_STRUCTURE_PASS " + scene.name);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error) { Fail(error); }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ActiveKey, false)) return;
        stage = 0;
        ticks = 0;
        mouseTest = false;
        penguinFramesSeen.Clear();
        deadline = EditorApplication.timeSinceStartup + 60;
        keyboard = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
        // Batch mode has no focused Game view.
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying || keyboard == null) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Scene flow timed out at stage " + stage);
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "LoadingScene")
            {
                var penguin = UnityEngine.Object.FindFirstObjectByType<LoadingPenguin>();
                if (penguin == null) throw new Exception("Missing penguin animation");
                var texture = penguin.GetComponent<RawImage>().texture;
                if (texture == null) throw new Exception("Missing penguin texture");
                penguinFramesSeen.Add(texture.name);
            }
            if (stage == 0 && ++ticks > 10)
            {
                if (scene != "SampleScene") throw new Exception("Unexpected start scene " + scene);
                if (mouseTest) InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                else InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                stage = 1;
            }
            else if (stage == 1 && scene == "LoadingScene")
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState());
                stage = 2;
            }
            else if (stage == 2 && scene == "MainGameScene")
            {
                if (penguinFramesSeen.Count != 4) throw new Exception("Not all four penguin frames animated");
                Debug.Log("PENGUIN_ANIMATION_PASS: all four frames played");
                penguinFramesSeen.Clear();
                Debug.Log("SCENE_FLOW_INPUT_PASS " + (mouseTest ? "MouseLeft" : "Space"));
                if (!mouseTest)
                {
                    mouseTest = true;
                    SceneManager.LoadScene("SampleScene");
                    ticks = 0;
                    stage = 0;
                }
                else
                {
                    SessionState.SetBool(ActiveKey, false);
                    Debug.Log("SCENE_FLOW_ALL_PASS");
                    EditorApplication.Exit(0);
                }
            }
        }
        catch (Exception error) { Fail(error); }
    }

    private static void Fail(Exception error)
    {
        SessionState.SetBool(ActiveKey, false);
        Debug.LogException(error);
        EditorApplication.Exit(1);
    }

    private static void VerifyPenguinLayout()
    {
        var penguin = UnityEngine.Object.FindFirstObjectByType<LoadingPenguin>();
        if (penguin == null) throw new Exception("Penguin missing from LoadingScene");
        var serialized = new SerializedObject(penguin);
        var frames = serialized.FindProperty("frames");
        if (frames.arraySize != 4) throw new Exception("Expected four animation frames");
        for (int index = 0; index < 4; index++)
        {
            var texture = frames.GetArrayElementAtIndex(index).objectReferenceValue as Texture2D;
            if (texture == null || texture.name != "pixil-frame-0 (" + (13 + index) + ")")
                throw new Exception("Incorrect frame order or missing texture");
        }
        var slider = UnityEngine.Object.FindFirstObjectByType<Slider>();
        var rect = (RectTransform)penguin.transform;
        if (rect.parent != slider.handleRect) throw new Exception("Penguin must follow the slider handle");
        float original = slider.normalizedValue;
        float previousX = float.NegativeInfinity;
        foreach (float progress in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
        {
            slider.normalizedValue = progress;
            Canvas.ForceUpdateCanvases();
            if (Mathf.Abs(rect.position.x - slider.handleRect.position.x) > 0.01f || rect.position.y <= slider.handleRect.position.y)
                throw new Exception("Penguin is not above the progress marker");
            if (rect.position.x <= previousX) throw new Exception("Penguin did not follow progress");
            previousX = rect.position.x;
        }
        slider.normalizedValue = original;
        Debug.Log("PENGUIN_LAYOUT_PASS: frame order and progress positions 0/25/50/75/100%");
    }
}
