using System;
using UnityEngine;

// Only audio and display settings are stored under this project-specific key.
public static class GameSettings
{
    public const string PreferenceKey = "Game.Settings.v1";
    [Serializable]
    public class Values
    {
        public float master = 1f, bgm = 1f, sfx = 1f;
        public int width, height;
        public bool fullscreen;
    }

    public static Values Current { get; private set; }
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearStatics() { Current = null; Changed = null; }

    public static void Initialize()
    {
        if (Current != null) return;
        Current = Defaults();
        try
        {
            if (PlayerPrefs.HasKey(PreferenceKey))
                Current = JsonUtility.FromJson<Values>(PlayerPrefs.GetString(PreferenceKey)) ?? Defaults();
        }
        catch (Exception) { Current = Defaults(); }
        Current.master = Safe(Current.master, 0f, 1f, 1f);
        Current.bgm = Safe(Current.bgm, 0f, 1f, 1f);
        Current.sfx = Safe(Current.sfx, 0f, 1f, 1f);
        if (Current.width < 640 || Current.height < 480) { Current.width = Screen.width; Current.height = Screen.height; }
        ApplyLive();
        ApplyDisplay();
    }

    private static float Safe(float value, float min, float max, float fallback) =>
        float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);

    private static Values Defaults() => new Values { width = Screen.width, height = Screen.height, fullscreen = Screen.fullScreen };

    public static void ApplyLive()
    {
        AudioListener.volume = Current.master;
        Changed?.Invoke();
    }

    public static void ApplyDisplay()
    {
        // The editor Game view cannot change the desktop's display mode.
        if (!Application.isEditor)
            Screen.SetResolution(Current.width, Current.height,
                Current.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    public static void Save()
    {
        PlayerPrefs.SetString(PreferenceKey, JsonUtility.ToJson(Current));
        PlayerPrefs.Save();
    }

    public static void ResetAudio()
    {
        Current.master = Current.bgm = Current.sfx = 1f;
        ApplyLive();
        Save();
    }
}
