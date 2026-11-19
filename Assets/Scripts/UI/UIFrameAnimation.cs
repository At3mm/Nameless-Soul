using UnityEngine;
using UnityEngine.UI;

/// <summary>Sprite-frame playback for UI Images, including tile-painted pause menus.</summary>
[RequireComponent(typeof(Image))]
public class UIFrameAnimation : MonoBehaviour
{
    public Sprite[] frames;
    [HideInInspector] public float framesPerSecond = 12f;
    public bool loop = true;
    public bool unscaledTime = true;
    public bool IsPlaying { get; private set; }
    private Image image;
    private float elapsed;

    private void Awake() { image = GetComponent<Image>(); }
    private void OnEnable() { Play(); }
    private void OnDisable() { IsPlaying = false; }

    public void Configure(Sprite[] sprites, float fps, bool repeat, bool useUnscaledTime = true)
    {
        frames = sprites;
        framesPerSecond = PixelAnimationSettings.FPS;
        loop = repeat;
        unscaledTime = useUnscaledTime;
        Play();
    }

    public void Play()
    {
        elapsed = 0f;
        IsPlaying = frames != null && frames.Length > 0;
        Sample(0f);
    }

    public void Sample(float seconds)
    {
        if (frames == null || frames.Length == 0) return;
        if (image == null) image = GetComponent<Image>();
        int index = Mathf.FloorToInt(Mathf.Max(0f, seconds) * PixelAnimationSettings.FPS);
        index = loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1);
        image.sprite = frames[index];
    }

    private void Update()
    {
        if (!IsPlaying) return;
        elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        Sample(elapsed);
        if (!loop && elapsed >= frames.Length / PixelAnimationSettings.FPS) IsPlaying = false;
    }
}
