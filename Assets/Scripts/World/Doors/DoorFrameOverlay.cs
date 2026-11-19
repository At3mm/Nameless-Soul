using UnityEngine;

/// <summary>Keeps the original animated door edges above the surrounding wall tiles.</summary>
[DefaultExecutionOrder(100)]
public sealed class DoorFrameOverlay : MonoBehaviour
{
    public SpriteRenderer source, left, right;
    public Sprite[] sourceFrames, leftFrames, rightFrames;
    private void LateUpdate()
    {
        if (source == null || left == null || right == null || sourceFrames == null) return;
        for (int i = 0; i < sourceFrames.Length; i++)
        {
            if (sourceFrames[i] != source.sprite) continue;
            left.sprite = leftFrames[i];
            right.sprite = rightFrames[i];
            left.color = right.color = source.color;
            left.enabled = right.enabled = source.enabled;
            return;
        }
    }
}
