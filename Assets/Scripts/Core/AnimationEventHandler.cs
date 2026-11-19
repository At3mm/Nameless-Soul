using UnityEngine;
using System.Collections;

public class AnimationEventHandler : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private PlayerStun stun;
    [Header("Xác chết biến mất kiểu pixel")]
    [Min(0f)] public float corpseHoldDuration = 3f;
    [Min(0f)] public float corpseFadeDuration = .5f;
    [Range(1f, 30f)] public float corpseEffectFPS = 8f;
    private bool disappearanceStarted;

    [Header("Trạng thái Hành động")]
    [HideInInspector] public bool isActionLocked = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        stun = GetComponent<PlayerStun>();
    }

    [Tooltip("Khóa di chuyển/hành động (Gọi ở frame đầu của đòn đánh/bị choáng)")]
    public void LockAction()
    {
        isActionLocked = true;
    }

    [Tooltip("Mở khóa di chuyển/hành động (Gọi ở frame cuối cùng)")]
    public void UnlockAction()
    {
        if(BossPhaseController.Protects(gameObject))return;
        if (stun != null && stun.IsStunned) return;
        var enemyBreak = GetComponent<EnemyBreak>();
        if (enemyBreak != null && enemyBreak.IsBroken) return;
        isActionLocked = false;
    }

    [Tooltip("Hàm được gọi từ Animation Event để dọn dẹp xác nhân vật")]
    public void DestroyCharacter()
    {
        if (disappearanceStarted) return;
        disappearanceStarted = true;
        StartCoroutine(WaitAndDisappear());
    }

    private IEnumerator WaitAndDisappear()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, corpseHoldDuration));

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            float initialAlpha = color.a;
            float elapsed = 0f;
            var clock = new PixelEffectClock();
            while (elapsed < corpseFadeDuration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (clock.Tick(Time.deltaTime, corpseEffectFPS) || elapsed >= corpseFadeDuration)
                {
                    color.a = initialAlpha * (1f - Mathf.Clamp01(elapsed / Mathf.Max(.001f, corpseFadeDuration)));
                    spriteRenderer.color = color;
                }
            }
        }

        Destroy(gameObject);
    }
}
