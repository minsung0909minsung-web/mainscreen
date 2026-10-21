using UnityEngine;

// Add to each future BGM/SFX source and select its channel in the Inspector.
[RequireComponent(typeof(AudioSource))]
public class SettingsAudioChannel : MonoBehaviour
{
    public enum Channel { BGM, SFX }
    [SerializeField] private Channel channel = Channel.SFX;
    [SerializeField, Range(0, 1)] private float baseVolume = 1f;
    private AudioSource source;
    private void OnEnable()
    {
        source = GetComponent<AudioSource>();
        GameSettings.Initialize();
        GameSettings.Changed += Refresh;
        Refresh();
    }
    private void OnDisable() { GameSettings.Changed -= Refresh; }
    private void Refresh() { source.volume = baseVolume * (channel == Channel.BGM ? GameSettings.Current.bgm : GameSettings.Current.sfx); }
}
