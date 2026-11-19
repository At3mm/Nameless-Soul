using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

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
    [Min(0f), Tooltip("Seconds spent searching the last visible position after losing sight.")]
    public float lostSightMemory = 2f;
    private readonly List<RaycastHit2D> sightHits = new List<RaycastHit2D>();

    public bool HasLineOfSight(Transform target)
    {
        if (target == null) return false;
        var filter = new ContactFilter2D { useTriggers = false };
        sightHits.Clear();
        Physics2D.Linecast(transform.position, target.position, filter, sightHits);
        foreach (var hit in sightHits)
        {
            var collider = hit.collider;
            if (collider == null) continue;
            var map = collider.GetComponentInParent<Tilemap>();
            if (map == null) continue;
            if (map.name == "GroundObstacles" || map.name == "Walls_Cliffs") return false;
        }
        return true;
    }

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
        ProcessAlert(distanceToPlayer, true);
    }

    public void ProcessAlert(float distanceToPlayer, bool canSeePlayer)
    {
        if (!canSeePlayer)
        {
            currentAlertTime -= Time.deltaTime;
        }
        else if (distanceToPlayer <= instantAggroDistance)
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
