using UnityEngine;

[RequireComponent(typeof(EnemyAwareness), typeof(OrcCombat), typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class OrcBehavior : MonoBehaviour
{
    public enum OrcState { Patrol, Alert, Chase, Strafe, Attack, Retreat, Return }

    [Header("Trạng thái hiện tại")]
    public OrcState currentState = OrcState.Patrol;

    [Header("Cài đặt Di chuyển")]
    public float patrolRadius = 3f;
    public float waitTimeAtEdge = 2f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float retreatSpeed = 2.5f;
    public float safeDistance = 3f;

    [Header("Cài đặt Nhịp độ Chiến đấu (Smart AI Pacing)")]
    [Tooltip("Thời gian Orc vờn quanh Player trước khi quyết định lao vào đánh tiếp")]
    public float minHesitationTime = 0.4f;
    public float maxHesitationTime = 1.2f;
    private float hesitationTimer = 0f;

    [Tooltip("Tỷ lệ đổi hướng vờn (Trái/Phải)")]
    private int strafeDirection = 1; // 1: Phải, -1: Trái
    private float strafeChangeTimer = 0f;

    [Header("Cài đặt Bứt Tốc Bất Ngờ (Sprint/Lunge Attack)")]
    public float sprintTriggerDistance = 4f;
    public float sprintSpeed = 6.5f;
    public float sprintCooldown = 4f;
    private float lastSprintTime = -10f;
    private bool isSprinting = false;

    private Rigidbody2D rb;
    private AnimationEventHandler animHandler;
    private EnemyAwareness awareness;
    private OrcCombat combat;
    private Animator animator;

    private Transform player;
    private CharacterHealth playerHealth;

    private bool isWaiting = false;
    private float waitTimer = 0f;
    private Vector2 patrolTarget;

    private void Start()
    {
        awareness = GetComponent<EnemyAwareness>();
        combat = GetComponent<OrcCombat>();
        animator = GetComponent<Animator>();
        animHandler = GetComponent<AnimationEventHandler>();
        rb = GetComponent<Rigidbody2D>();
        patrolTarget = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<CharacterHealth>();
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Bị ăn đòn (Stun/Knockback) -> Ngắt di chuyển AI
        if (animHandler != null && animHandler.isActionLocked)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsWalking", false);
            return;
        }

        // Đang lao tới tấn công (Lunge) -> Nhường quyền kiểm soát vận tốc cho OrcCombat
        if (combat.IsLunging)
        {
            return;
        }

        if (playerHealth != null && playerHealth.isDead)
        {
            if (currentState == OrcState.Return && awareness.IsHome())
            {
                currentState = OrcState.Patrol;
            }
            ExecuteState();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (currentState != OrcState.Return || distanceToPlayer <= awareness.instantAggroDistance)
        {
            awareness.ProcessAlert(distanceToPlayer);
        }

        CheckState(distanceToPlayer);
        ExecuteState();
    }

    private void CheckState(float distanceToPlayer)
    {
        bool inAttackRange = combat.IsInAttackRange(player);
        bool inRedZone = distanceToPlayer <= awareness.instantAggroDistance;

        float distToHome = Vector2.Distance(transform.position, awareness.startPosition);
        bool isSafelyInsideTerritory = distToHome < (awareness.maxChaseDistance * 0.8f);

        switch (currentState)
        {
            case OrcState.Patrol:
                if (inAttackRange) { currentState = OrcState.Attack; awareness.MaxAlert(); isWaiting = false; }
                else if (inRedZone || awareness.ShouldChase()) { currentState = OrcState.Chase; isWaiting = false; }
                else if (awareness.currentAlertTime > 0) { currentState = OrcState.Alert; isWaiting = false; }
                break;

            case OrcState.Alert:
                if (inAttackRange) { currentState = OrcState.Attack; awareness.MaxAlert(); }
                else if (inRedZone || awareness.ShouldChase()) { currentState = OrcState.Chase; }
                else if (awareness.currentAlertTime <= 0) { currentState = OrcState.Patrol; }
                break;

            case OrcState.Chase:
                if (inAttackRange)
                {
                    currentState = OrcState.Attack;
                }
                else if (awareness.IsTooFar())
                {
                    currentState = OrcState.Return;
                }
                break;

            case OrcState.Attack:
                if (!inAttackRange && !combat.IsLunging)
                {
                    // Sau khi đánh xong -> Rút lui vờn quanh thay vì đâm đầu lại ngay
                    StartRetreatAndStrafe();
                }
                break;

            case OrcState.Retreat:
            case OrcState.Strafe:
                // Nếu đã đủ thời gian nhấp nhổm và chiêu đã hồi -> Chuyển sang Chase để tấn công
                if (combat.CanChaseAgain())
                {
                    hesitationTimer -= Time.deltaTime;
                    if (hesitationTimer <= 0)
                    {
                        currentState = OrcState.Chase;
                    }
                }

                if (awareness.IsTooFar())
                {
                    currentState = OrcState.Return;
                }
                break;

            case OrcState.Return:
                if (inAttackRange) { currentState = OrcState.Attack; awareness.MaxAlert(); }
                else if (inRedZone && isSafelyInsideTerritory) { currentState = OrcState.Chase; awareness.MaxAlert(); }
                else if (awareness.IsHome()) { currentState = OrcState.Patrol; awareness.ResetAlert(); }
                break;
        }
    }

    private void ExecuteState()
    {
        bool isMoving = false;

        switch (currentState)
        {
            case OrcState.Patrol:
                PatrolBehavior();
                isMoving = !isWaiting;
                isSprinting = false;
                break;

            case OrcState.Alert:
                FacePlayer();
                isMoving = false;
                isSprinting = false;
                break;

            case OrcState.Chase:
                float distToPlayer = Vector2.Distance(transform.position, player.position);

                // KIỂM TRA BỨT TỐC BẤT NGỜ (SPRINT)
                if (distToPlayer <= sprintTriggerDistance && Time.time >= lastSprintTime + sprintCooldown)
                {
                    isSprinting = true;
                    lastSprintTime = Time.time;
                }

                float currentMoveSpeed = isSprinting ? sprintSpeed : chaseSpeed;
                MoveTowards(player.position, currentMoveSpeed);
                isMoving = true;
                break;

            case OrcState.Attack:
                FacePlayer();
                if (combat.TriggerAttack(player, awareness.startPosition, awareness.maxChaseDistance))
                {
                    isSprinting = false;
                    StartRetreatAndStrafe();
                }
                isMoving = false;
                break;

            case OrcState.Retreat:
            case OrcState.Strafe:
                isSprinting = false;
                StrafeAndRetreatBehavior();
                isMoving = true;
                FacePlayer();
                break;

            case OrcState.Return:
                isSprinting = false;
                MoveTowards(awareness.startPosition, patrolSpeed);
                isMoving = true;
                break;
        }

        animator.SetBool("IsWalking", isMoving);

        if (!isMoving && !combat.IsLunging)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Kích hoạt trạng thái Rút lui & Vờn xung quanh Player
    /// </summary>
    private void StartRetreatAndStrafe()
    {
        currentState = OrcState.Retreat;
        hesitationTimer = Random.Range(minHesitationTime, maxHesitationTime);
        strafeDirection = Random.value > 0.5f ? 1 : -1; // Ngẫu nhiên lượn trái hoặc phải
        strafeChangeTimer = Random.Range(1f, 2f);
    }

    /// <summary>
    /// Logic di chuyển Vòng cung (Circling/Strafing) kết hợp Lùi khoảng cách
    /// </summary>
    private void StrafeAndRetreatBehavior()
    {
        float currentDist = Vector2.Distance(transform.position, player.position);
        Vector2 dirToPlayer = (player.position - transform.position).normalized;

        // Đổi hướng lượn ngẫu nhiên theo thời gian
        strafeChangeTimer -= Time.deltaTime;
        if (strafeChangeTimer <= 0)
        {
            strafeDirection *= -1;
            strafeChangeTimer = Random.Range(1f, 2f);
        }

        // Tạo Vector vuông góc với hướng Player (Di chuyển ngang)
        Vector2 perpendicularDir = new Vector2(-dirToPlayer.y, dirToPlayer.x) * strafeDirection;

        Vector2 finalMoveDir;

        if (currentDist < safeDistance)
        {
            // Vừa lùi lại vừa lượn ngang (Tạo đường cong lùi)
            finalMoveDir = (-dirToPlayer * 0.6f + perpendicularDir * 0.4f).normalized;
        }
        else if (currentDist > safeDistance + 1f)
        {
            // Vừa tiến lại vừa lượn ngang
            finalMoveDir = (dirToPlayer * 0.5f + perpendicularDir * 0.5f).normalized;
        }
        else
        {
            // Chỉ lượn quanh (Vờn quan sát)
            finalMoveDir = perpendicularDir;
        }

        rb.linearVelocity = finalMoveDir * retreatSpeed;
    }

    private void PatrolBehavior()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                Vector2 randomDirection = Random.insideUnitCircle * patrolRadius;
                patrolTarget = awareness.startPosition + randomDirection;
            }
            return;
        }

        MoveTowards(patrolTarget, patrolSpeed);
        if (Vector2.Distance(transform.position, patrolTarget) < 0.1f) StartWaiting();
    }

    private void StartWaiting() { isWaiting = true; waitTimer = waitTimeAtEdge; }

    private void MoveTowards(Vector2 targetPosition, float speed)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * speed;

        if (targetPosition.x > transform.position.x) transform.localScale = Vector3.one;
        else if (targetPosition.x < transform.position.x) transform.localScale = new Vector3(-1, 1, 1);
    }

    private void FacePlayer()
    {
        if (player.position.x > transform.position.x) transform.localScale = Vector3.one;
        else if (player.position.x < transform.position.x) transform.localScale = new Vector3(-1, 1, 1);
    }
}