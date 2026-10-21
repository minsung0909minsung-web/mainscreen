using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class SettingsMenu : MonoBehaviour
{
    [Header("Editable UI references")]
    public Button openButton, closeButton, applyDisplayButton, resetButton;
    public GameObject modal;
    public Slider master, bgm, sfx;
    public TMP_Text masterValue, bgmValue, sfxValue, status, resolutionLabel, fullscreenLabel;
    public Button previousResolution, nextResolution, fullscreenButton;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private readonly List<InputAction> suspendedActions = new List<InputAction>();
    private int resolutionIndex;
    private bool fullscreen;
    private float previousTimeScale;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    public bool IsOpen => modal != null && modal.activeSelf;

    private void Awake()
    {
        modal.SetActive(false);
        GameSettings.Initialize();
        BuildResolutions();
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);
        master.onValueChanged.AddListener(v => { GameSettings.Current.master = v; LiveChanged(); });
        bgm.onValueChanged.AddListener(v => { GameSettings.Current.bgm = v; LiveChanged(); });
        sfx.onValueChanged.AddListener(v => { GameSettings.Current.sfx = v; LiveChanged(); });
        previousResolution.onClick.AddListener(() => CycleResolution(-1));
        nextResolution.onClick.AddListener(() => CycleResolution(1));
        fullscreenButton.onClick.AddListener(() => { fullscreen = !fullscreen; RefreshDisplay(); });
        applyDisplayButton.onClick.AddListener(ApplyDisplay);
        resetButton.onClick.AddListener(() => { GameSettings.ResetAudio(); Refresh(); status.text = "Audio restored."; });
        Refresh();
    }

    public void Open()
    {
        if (IsOpen) return;
        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        suspendedActions.Clear();
        var map = InputSystem.actions?.FindActionMap("Player");
        if (map != null) foreach (var action in map.actions) if (action.enabled) { suspendedActions.Add(action); action.Disable(); }
        modal.SetActive(true);
        openButton.interactable = false;
        Refresh();
        status.text = "Audio saves automatically. Display: Apply.";
        EventSystem.current?.SetSelectedGameObject(closeButton.gameObject);
    }

    public void Close()
    {
        if (!IsOpen) return;
        GameSettings.Save();
        modal.SetActive(false);
        RestoreGame();
        openButton.interactable = true;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void RestoreGame()
    {
        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        foreach (var action in suspendedActions) action.Enable();
        suspendedActions.Clear();
    }

    private void OnDestroy()
    {
        if (IsOpen) { GameSettings.Save(); RestoreGame(); }
    }

    private void Update()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void BuildResolutions()
    {
        foreach (var r in Screen.resolutions) AddResolution(r.width, r.height);
        AddResolution(1280, 720); AddResolution(1600, 900); AddResolution(1920, 1080);
        AddResolution(Screen.width, Screen.height);
        AddResolution(GameSettings.Current.width, GameSettings.Current.height);
        resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
    }
    private void AddResolution(int w, int h) { var size = new Vector2Int(w, h); if (w >= 640 && h >= 480 && !resolutions.Contains(size)) resolutions.Add(size); }
    private void CycleResolution(int step) { resolutionIndex = (resolutionIndex + step + resolutions.Count) % resolutions.Count; RefreshDisplay(); }
    public void ApplyDisplay()
    {
        var size = resolutions[resolutionIndex];
        GameSettings.Current.width = size.x; GameSettings.Current.height = size.y; GameSettings.Current.fullscreen = fullscreen;
        GameSettings.ApplyDisplay(); GameSettings.Save();
        status.text = Application.isEditor ? "Display saved. Test fullscreen in a standalone build." : "Display settings applied.";
    }
    private void RefreshDisplay()
    {
        var size = resolutions[resolutionIndex];
        resolutionLabel.text = size.x + " x " + size.y;
        fullscreenLabel.text = fullscreen ? "[X]  Fullscreen" : "[  ]  Windowed";
    }
    private void LiveChanged() { GameSettings.ApplyLive(); RefreshValues(); GameSettings.Save(); }
    private void RefreshValues()
    {
        masterValue.text = Mathf.RoundToInt(master.value * 100) + "%";
        bgmValue.text = Mathf.RoundToInt(bgm.value * 100) + "%";
        sfxValue.text = Mathf.RoundToInt(sfx.value * 100) + "%";
    }
    private void Refresh()
    {
        var data = GameSettings.Current;
        master.SetValueWithoutNotify(data.master); bgm.SetValueWithoutNotify(data.bgm); sfx.SetValueWithoutNotify(data.sfx);
        resolutionIndex = Mathf.Max(0, resolutions.IndexOf(new Vector2Int(data.width, data.height)));
        fullscreen = data.fullscreen;
        RefreshDisplay(); RefreshValues();
    }
}
