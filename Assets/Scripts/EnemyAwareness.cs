using UnityEngine;

/// <summary>
/// Component quản lý tầm nhìn và độ cảnh giác của kẻ địch.
/// Thiết kế độc lập để có thể gắn vào bất kỳ quái vật nào mà không cần code lại.
/// </summary>
public class EnemyAwareness : MonoBehaviour
{
    [Header("Cài đặt Cảnh giác")]
    public float chaseTriggerDistance = 5f;
    public float instantAggroDistance = 2.5f;
    public float alertTimeTrigger = 2f;
    public float maxChaseDistance = 8f;

    public float currentAlertTime { get; private set; }
    public Vector2 startPosition { get; private set; }

    private void Start()
    {
        startPosition = transform.position;
    }

    /// <summary>
    /// Tính toán và cập nhật độ cảnh giác dựa trên khoảng cách của người chơi.
    /// </summary>
    /// <param name="distanceToPlayer">Khoảng cách từ quái đến người chơi</param>
    public void ProcessAlert(float distanceToPlayer)
    {
        if (distanceToPlayer <= instantAggroDistance)
        {
            // Bước vào vùng nguy hiểm -> Max cảnh giác ngay lập tức
            currentAlertTime = alertTimeTrigger;
        }
        else if (distanceToPlayer <= chaseTriggerDistance)
        {
            // Nhấp nhổm ở vùng ngoài -> Cảnh giác tăng dần
            currentAlertTime += Time.deltaTime;
        }
        else
        {
            // Đi ra xa khỏi vùng -> Cảnh giác giảm dần
            currentAlertTime -= Time.deltaTime;
        }

        // Đảm bảo thời gian không bị âm hoặc vượt quá giới hạn
        currentAlertTime = Mathf.Clamp(currentAlertTime, 0, alertTimeTrigger);
    }

    /// <summary> Đưa mức độ cảnh giác về 0 </summary>
    public void ResetAlert() => currentAlertTime = 0f;

    /// <summary> Đẩy mức độ cảnh giác lên tối đa </summary>
    public void MaxAlert() => currentAlertTime = alertTimeTrigger;

    /// <summary> Kiểm tra xem đã đủ độ cảnh giác để rượt đuổi chưa </summary>
    public bool ShouldChase() => currentAlertTime >= alertTimeTrigger;

    /// <summary> Kiểm tra xem quái có đi quá xa khỏi vị trí ban đầu không </summary>
    public bool IsTooFar() => Vector2.Distance(transform.position, startPosition) >= maxChaseDistance;

    /// <summary> Kiểm tra xem quái đã về đến vị trí ban đầu (nhà) chưa </summary>
    public bool IsHome() => Vector2.Distance(transform.position, startPosition) < 0.1f;

    private void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying ? startPosition : (Vector2)transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseTriggerDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, instantAggroDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f); // Màu cam
        Gizmos.DrawWireSphere(center, maxChaseDistance);
    }
}