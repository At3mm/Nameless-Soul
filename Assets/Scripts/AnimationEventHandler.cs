using UnityEngine;
using System.Collections;

public class AnimationEventHandler : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    [Header("Trạng thái Hành động")]
    [HideInInspector] public bool isActionLocked = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    [Tooltip("Khóa di chuyển/hành động (Gọi ở frame đầu của đòn đánh/bị choáng)")]
    public void LockAction()
    {
        isActionLocked = true;
    }

    [Tooltip("Mở khóa di chuyển/hành động (Gọi ở frame cuối cùng)")]
    public void UnlockAction()
    {
        isActionLocked = false;
    }

    [Tooltip("Hàm được gọi từ Animation Event để dọn dẹp xác nhân vật")]
    public void DestroyCharacter()
    {
        StartCoroutine(WaitAndDisappear());
    }

    private IEnumerator WaitAndDisappear()
    {
        yield return new WaitForSeconds(3f);

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            float timePerFrame = 0.1f;

            // Step 1: Alpha 66%
            color.a = 0.66f;
            spriteRenderer.color = color;
            yield return new WaitForSeconds(timePerFrame);

            // Step 2: Alpha 33%
            color.a = 0.33f;
            spriteRenderer.color = color;
            yield return new WaitForSeconds(timePerFrame);

            // Step 3: Alpha 0%
            color.a = 0f;
            spriteRenderer.color = color;
            yield return new WaitForSeconds(timePerFrame);
        }

        Destroy(gameObject);
    }
}