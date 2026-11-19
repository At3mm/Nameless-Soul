using UnityEngine;
using UnityEngine.SceneManagement;

namespace World
{
    [RequireComponent(typeof(Collider2D))]
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [Tooltip("Tên scene cần chuyển đến (phải có trong Build Settings)")]
        [SerializeField] private string targetSceneName;
        [Tooltip("ID điểm xuất hiện ở scene đích. Để trống để dùng vị trí mặc định.")]
        [SerializeField] private string destinationSpawnPoint;

        [Header("Trigger Settings")]
        [Tooltip("Tag của đối tượng kích hoạt chuyển cảnh")]
        [SerializeField] private string triggerTag = "Player";

        [Tooltip("Thời gian hiệu ứng làm mờ màn hình (giây)")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Gizmo Visualizer")]
        [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.4f, 0.4f);

        private bool _isTransitioning;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isTransitioning) return;

            if (other.CompareTag(triggerTag))
            {
                if (string.IsNullOrEmpty(targetSceneName))
                {
                    Debug.LogWarning($"[{name}] Chưa đặt 'Target Scene Name' trong Inspector!", this);
                    return;
                }

                _isTransitioning = true;
                SceneSpawnPoint.Prepare(targetSceneName, destinationSpawnPoint);
                Debug.Log($"[{name}] Người chơi chạm vào trigger! Đang chuyển cảnh sang: {targetSceneName} với hiệu ứng Fade...");

                if (ScreenFader.Instance != null)
                {
                    ScreenFader.Instance.TransitionToScene(targetSceneName, fadeDuration);
                }
                else
                {
                    SceneManager.LoadScene(targetSceneName);
                }
            }
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                Gizmos.color = gizmoColor;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.offset, col.size);
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
                Gizmos.DrawWireCube(col.offset, col.size);
            }
        }
    }
}

