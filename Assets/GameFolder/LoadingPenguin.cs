using UnityEngine;
using UnityEngine.UI;

// Place this UI under the slider Handle so it follows progress automatically.
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public class LoadingPenguin : MonoBehaviour
{
    [Tooltip("Animation frames in playback order. Full textures retain the original canvas alignment.")]
    [SerializeField] private Texture2D[] frames;
    [SerializeField, Min(0.1f)] private float framesPerSecond = 8f;

    private RawImage image;
    private float elapsed;

    private void OnEnable()
    {
        image = GetComponent<RawImage>();
        image.raycastTarget = false;
        elapsed = 0f;
        ShowFrame(0);
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;
        elapsed = Mathf.Repeat(elapsed + Time.unscaledDeltaTime * Mathf.Max(0.1f, framesPerSecond), frames.Length);
        ShowFrame(Mathf.FloorToInt(elapsed));
    }

    private void ShowFrame(int index)
    {
        if (frames != null && frames.Length > index && frames[index] != null)
            image.texture = frames[index];
    }
}
