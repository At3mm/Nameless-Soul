using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyAwareness), typeof(EnemyCombat), typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBehavior : MonoBehaviour
{
    public enum OrcState { Patrol, Alert, Chase, Strafe, Attack, Retreat, Return }

    [Header("Trạng thái hiện tại")]
    public OrcState currentState = OrcState.Patrol;

    public bool IsInCombatWith(Transform target)
    {
        if (!isActiveAndEnabled || target == null || (player != null && player != target)) return false;
        var perception = awareness != null ? awareness : GetComponent<EnemyAwareness>();
        // Include instant aggro before the next AI Update, so E cannot beat enemy detection.
        bool immediateThreat = perception != null && Vector2.Distance(transform.position,target.position) <= perception.instantAggroDistance &&
            perception.HasLineOfSight(target);
        if (player == null) return immediateThreat;
        var broken = enemyBreak != null ? enemyBreak : GetComponent<EnemyBreak>();
        return immediateThreat || (broken != null && broken.IsBroken) ||
            (currentState != OrcState.Patrol && currentState != OrcState.Return);
    }

    [Header("Cài đặt Di chuyển")]
    public float patrolRadius = 3f;
    public float waitTimeAtEdge = 2f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float retreatSpeed = 2.5f;
    public float safeDistance = 3f;
    [Tooltip("Continuously pursue and attack instead of patrol, retreat and territory return. Used by summoned bats.")]
    public bool relentlessPursuit;

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

    [Header("Cài đặt Tránh Vật Cản & Chống Kẹt Góc")]
    [Tooltip("Layer chứa các vật cản (Đá, Tường, Cây, Vực nước...)")]
    public LayerMask obstacleLayer = 1 << 0; // Layer Default
    [Tooltip("Khoảng cách quét phát hiện vật cản phía trước")]
    public float obstacleCheckDistance = 0.8f;
    [Tooltip("Bán kính vùng quét (tương đương nửa chiều rộng quái)")]
    public float obstacleCheckRadius = 0.20f;
    [Min(0.16f), Tooltip("Giới hạn bán kính tự căn theo collider. Tăng cho quái có thân lớn hơn Orc thường.")]
    public float maxAutoObstacleRadius = 0.22f;
    [Tooltip("Tốc độ bẻ lái mượt mà khi né vật cản")]
    public float steerDamping = 10f;
    [Tooltip("Thời gian đứng yên để kích hoạt phản xạ thoát góc kẹt")]
    public float stuckThresholdTime = 0.25f;

    private Vector2 currentSteeredDirection = Vector2.zero;
    private Vector2 lastPositionCheck;
    private float universalStuckTimer = 0f;
    private float unstickTimer = 0f;
    private Vector2 unstickVelocity = Vector2.zero;
    private float lastDirectionChangeTime = -1f;

    // Quản lý tìm đường vượt vật cản khi đuổi theo hoặc về nhà (2D Local Pathfinding)
    private List<Vector2> currentNavPath = new List<Vector2>();
    private int currentNavPathIndex = 0;
    private float nextNavRecalcTime = 0f;
    private Vector2 lastNavTargetPosition;

    private WaterPresence waterPresence;
    private EnemyBreak enemyBreak;
    private float WaterMovementMultiplier => waterPresence != null ? waterPresence.EnemySpeedMultiplier : 1f;
    private Rigidbody2D rb;
    private AnimationEventHandler animHandler;
    private EnemyAwareness awareness;
    private EnemyCombat combat;
    private Animator animator;

    private Transform player;
    private CharacterHealth playerHealth;
    private bool canSeePlayer;
    private Vector2 lastSeenPlayerPosition;
    private float lastSeenPlayerTime = float.NegativeInfinity;

    private bool isWaiting = false;
    private float waitTimer = 0f;
    private Vector2 patrolTarget;

    private void Start()
    {
        awareness = GetComponent<EnemyAwareness>();
        combat = GetComponent<EnemyCombat>();
        animator = GetComponent<Animator>();
        animHandler = GetComponent<AnimationEventHandler>();
        rb = GetComponent<Rigidbody2D>();
        waterPresence = GetComponent<WaterPresence>();
        enemyBreak = GetComponent<EnemyBreak>();
        patrolTarget = transform.position;
        lastPositionCheck = transform.position;

        // Tự động kiểm tra và giới hạn bán kính quét theo kích thước collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            float halfWidth = col.bounds.extents.x;
            if (halfWidth > 0.05f)
            {
                obstacleCheckRadius = Mathf.Clamp(halfWidth * 0.9f, 0.16f, Mathf.Max(0.16f, maxAutoObstacleRadius));
            }
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<CharacterHealth>();
        }
    }

    private void Update()
    {
        if(BossPhaseController.Protects(gameObject)){rb.linearVelocity=Vector2.zero;return;}
        if (enemyBreak != null && enemyBreak.IsBroken)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsWalking", false);
            return;
        }
        if (player == null) return;

        var bossDirector = GetComponent<BossMovesetDirector>();
        if (bossDirector != null && bossDirector.IsControlling)
        {
            if (!combat.IsLunging) rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsWalking", false);
            return;
        }

        // Bị ăn đòn (Stun/Knockback) -> Ngắt di chuyển AI
        if (animHandler != null && animHandler.isActionLocked)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsWalking", false);
            return;
        }

        // Đang lao tới tấn công (Lunge) -> Nhường quyền kiểm soát vận tốc cho EnemyCombat
        if (combat.IsLunging)
        {
            return;
        }

        // Đang thực hiện phản xạ thoát kẹt góc (Unstick Reflex)
        if (unstickTimer > 0f)
        {
            unstickTimer -= Time.deltaTime;
            rb.linearVelocity = unstickVelocity * WaterMovementMultiplier;
            if (Mathf.Abs(unstickVelocity.x) > 0.05f)
            {
                transform.localScale = new Vector3(unstickVelocity.x > 0 ? 1 : -1, 1, 1);
            }
            animator.SetBool("IsWalking", true);
            lastPositionCheck = transform.position;
            universalStuckTimer = 0f;
            return;
        }

        if (playerHealth != null && playerHealth.isDead)
        {
            if (currentState == OrcState.Return && awareness.IsHome())
            {
                GetComponent<EnemyClassification>()?.OnReturnedHome();
                currentState = OrcState.Patrol;
            }
            ExecuteState();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        canSeePlayer = awareness.HasLineOfSight(player);
        if(relentlessPursuit)
        {
            lastSeenPlayerPosition=player.position;
            lastSeenPlayerTime=Time.time;
            currentState=canSeePlayer&&combat.IsInAttackRange(player)?OrcState.Attack:OrcState.Chase;
            ExecuteState();
            return;
        }
        if (canSeePlayer)
        {
            lastSeenPlayerPosition = player.position;
            lastSeenPlayerTime = Time.time;
        }

        if (currentState != OrcState.Return || distanceToPlayer <= awareness.instantAggroDistance)
        {
            awareness.ProcessAlert(distanceToPlayer, canSeePlayer);
        }

        CheckState(distanceToPlayer);
        ExecuteState();

        // Kiểm tra chống kẹt góc toàn diện (Universal Stuck Check)
        CheckStuckCondition();
    }

    /// <summary>
    /// Kiểm tra nếu quái bị kẹt góc ở bất kỳ trạng thái di chuyển nào
    /// </summary>
    private void CheckStuckCondition()
    {
        bool shouldBeMoving = (currentState == OrcState.Chase) ||
                              (currentState == OrcState.Return) ||
                              (currentState == OrcState.Patrol && !isWaiting) ||
                              (currentState == OrcState.Retreat) ||
                              (currentState == OrcState.Strafe);

        if (!shouldBeMoving)
        {
            universalStuckTimer = 0f;
            lastPositionCheck = transform.position;
            return;
        }

        float distMoved = Vector2.Distance(transform.position, lastPositionCheck);
        if (distMoved < 0.035f)
        {
            universalStuckTimer += Time.deltaTime;
            if (universalStuckTimer >= stuckThresholdTime)
            {
                TriggerCornerUnstick();
                universalStuckTimer = 0f;
            }
        }
        else
        {
            universalStuckTimer = 0f;
            lastPositionCheck = transform.position;
        }
    }

    /// <summary>
    /// Phản xạ thoát kẹt góc: Quét tìm tất cả các bề mặt vật cản xung quanh và đẩy quái bật ra vùng trống
    /// </summary>
    private void TriggerCornerUnstick()
    {
        Vector2 currentPos = transform.position;

        Collider2D[] nearObstacles = Physics2D.OverlapCircleAll(currentPos, 0.45f, obstacleLayer);
        Vector2 pushAwayDir = Vector2.zero;

        foreach (var obs in nearObstacles)
        {
            if (obs.gameObject == gameObject) continue;
            Vector2 closestPoint = obs.ClosestPoint(currentPos);
            Vector2 away = currentPos - closestPoint;
            if (away.sqrMagnitude > 0.0001f)
            {
                pushAwayDir += away.normalized / Mathf.Max(away.magnitude, 0.05f);
            }
        }

        if (pushAwayDir.sqrMagnitude > 0.01f)
        {
            pushAwayDir.Normalize();
        }
        else
        {
            pushAwayDir = currentSteeredDirection.sqrMagnitude > 0.01f ? -currentSteeredDirection.normalized : Random.insideUnitCircle.normalized;
        }

        unstickVelocity = pushAwayDir * (patrolSpeed * 1.35f);
        unstickTimer = 0.25f;
        currentSteeredDirection = pushAwayDir;

        if (currentState == OrcState.Patrol)
        {
            ChangePatrolDirection(pushAwayDir);
        }
        else if (currentState == OrcState.Chase || currentState == OrcState.Return)
        {
            currentNavPath.Clear();
            currentNavPathIndex = 0;
            nextNavRecalcTime = Time.time + 0.2f;
        }
        else if (currentState == OrcState.Retreat || currentState == OrcState.Strafe)
        {
            strafeDirection *= -1;
        }
    }

    private void CheckState(float distanceToPlayer)
    {
        bool visible = awareness.HasLineOfSight(player);
        bool inAttackRange = visible && combat.IsInAttackRange(player);
        bool inRedZone = visible && distanceToPlayer <= awareness.instantAggroDistance;

        if (!visible && currentState != OrcState.Patrol && currentState != OrcState.Return)
        {
            // Remember the last observation, never the player's movement behind cover.
            bool memoryExpired = Time.time - lastSeenPlayerTime >= awareness.lostSightMemory;
            bool reachedLastPosition = Vector2.Distance(transform.position, lastSeenPlayerPosition) <= .35f;
            currentState = memoryExpired || reachedLastPosition || awareness.IsTooFar() ? OrcState.Return : OrcState.Chase;
            if (currentState == OrcState.Return) awareness.ResetAlert();
            return;
        }

        float distToHome = Vector2.Distance(transform.position, awareness.startPosition);
        bool isSafelyInsideTerritory = distToHome < (awareness.maxChaseDistance * 0.8f);

        switch (currentState)
        {
            case OrcState.Patrol:
                if (inAttackRange) { currentState = OrcState.Attack; awareness.MaxAlert(); isWaiting = false; }
                else if (inRedZone || (visible && awareness.ShouldChase())) { currentState = OrcState.Chase; isWaiting = false; }
                else if (awareness.currentAlertTime > 0) { currentState = OrcState.Alert; isWaiting = false; }
                break;

            case OrcState.Alert:
                if (inAttackRange) { currentState = OrcState.Attack; awareness.MaxAlert(); }
                else if (inRedZone || (visible && awareness.ShouldChase())) { currentState = OrcState.Chase; }
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
                else if (awareness.IsHome()) { GetComponent<EnemyClassification>()?.OnReturnedHome(); currentState = OrcState.Patrol; awareness.ResetAlert(); }
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
                ChaseBehavior();
                isMoving = true;
                break;

            case OrcState.Attack:
                FacePlayer();
                if (combat.TriggerAttack(player, awareness.startPosition, awareness.maxChaseDistance))
                {
                    isSprinting = false;
                    if(!relentlessPursuit)StartRetreatAndStrafe();
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
                ReturnBehavior();
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
    /// Logic di chuyển Vòng cung (Circling/Strafing) kết hợp Lùi khoảng cách và Né vật cản
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

        Vector2 desiredMoveDir;

        if (currentDist < safeDistance)
        {
            // Vừa lùi lại vừa lượn ngang (Tạo đường cong lùi)
            desiredMoveDir = (-dirToPlayer * 0.6f + perpendicularDir * 0.4f).normalized;
        }
        else if (currentDist > safeDistance + 1f)
        {
            // Vừa tiến lại vừa lượn ngang
            desiredMoveDir = (dirToPlayer * 0.5f + perpendicularDir * 0.5f).normalized;
        }
        else
        {
            // Chỉ lượn quanh (Vờn quan sát)
            desiredMoveDir = perpendicularDir;
        }

        // Kiểm tra xem hướng lùi có bị tường chắn không. Nếu bị vướng tường, bẻ lái trượt ngang và đảo chiều lượn
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, obstacleCheckRadius, desiredMoveDir, obstacleCheckDistance, obstacleLayer);
        if (hit && hit.collider != null)
        {
            strafeDirection *= -1;
            perpendicularDir = new Vector2(-dirToPlayer.y, dirToPlayer.x) * strafeDirection;
            desiredMoveDir = (perpendicularDir * 0.7f + hit.normal * 0.3f).normalized;
        }

        // Áp dụng thuật toán né vật cản khi lùi / vờn
        Vector2 steeredDir = GetSteeredDirection(desiredMoveDir);
        currentSteeredDirection = Vector2.Lerp(currentSteeredDirection, steeredDir, Time.deltaTime * steerDamping);
        if (currentSteeredDirection.sqrMagnitude < 0.01f) currentSteeredDirection = steeredDir;

        rb.linearVelocity = currentSteeredDirection.normalized * retreatSpeed * WaterMovementMultiplier;
    }

    /// <summary>
    /// Hành vi rượt đuổi người chơi: Tự động phát hiện nếu bị vật cản che khuất và tìm đường vòng (Pathfinding)
    /// </summary>
    private void ChaseBehavior()
    {
        Vector2 targetPosition = lastSeenPlayerPosition;
        float distToPlayer = Vector2.Distance(transform.position, targetPosition);
        bool hasClearPathToPlayer = HasClearPath(transform.position, targetPosition);

        // KIỂM TRA BỨT TỐC BẤT NGỜ (SPRINT)
        // Chỉ bứt tốc khi có đường thẳng trực diện thông thoáng đến Player
        if (canSeePlayer && hasClearPathToPlayer && distToPlayer <= sprintTriggerDistance && Time.time >= lastSprintTime + sprintCooldown)
        {
            isSprinting = true;
            lastSprintTime = Time.time;
        }
        else if (!canSeePlayer || !hasClearPathToPlayer)
        {
            isSprinting = false;
        }

        float currentMoveSpeed = isSprinting ? sprintSpeed : chaseSpeed;

        if (hasClearPathToPlayer)
        {
            // Đường đi thẳng đến Player hoàn toàn thông thoáng -> Đuổi trực tiếp
            currentNavPath.Clear();
            currentNavPathIndex = 0;
            MoveTowards(targetPosition, currentMoveSpeed);
        }
        else
        {
            // Bị vật cản chắn giữa đường -> Dùng thuật toán tìm đường vòng qua vật cản
            if (currentNavPath == null || currentNavPath.Count == 0 ||
                Time.time >= nextNavRecalcTime ||
                Vector2.Distance(targetPosition, lastNavTargetPosition) > 1.2f)
            {
                currentNavPath = FindPath(transform.position, targetPosition);
                currentNavPathIndex = 0;
                nextNavRecalcTime = Time.time + 0.35f;
                lastNavTargetPosition = targetPosition;
            }

            if (currentNavPath != null && currentNavPath.Count > 0)
            {
                // Kiểm tra xem có thể nhảy cóc tới điểm xa hơn trên lộ trình không (Shortcut)
                for (int i = currentNavPath.Count - 1; i > currentNavPathIndex; i--)
                {
                    if (HasClearPath(transform.position, currentNavPath[i]))
                    {
                        currentNavPathIndex = i;
                        break;
                    }
                }

                if (currentNavPathIndex < currentNavPath.Count)
                {
                    Vector2 currentWaypoint = currentNavPath[currentNavPathIndex];
                    if (Vector2.Distance(transform.position, currentWaypoint) < 0.35f)
                    {
                        currentNavPathIndex++;
                    }
                }

                Vector2 targetPos = (currentNavPathIndex < currentNavPath.Count) ?
                    currentNavPath[currentNavPathIndex] : targetPosition;

                MoveTowards(targetPos, currentMoveSpeed);
            }
            else
            {
                // Dự phòng né vật cản cục bộ
                MoveTowards(targetPosition, currentMoveSpeed);
            }
        }
    }

    /// <summary>
    /// Hành vi quay trở về vị trí ban đầu (nhà): Tìm đường vượt vật cản nếu bị chắn
    /// </summary>
    private void ReturnBehavior()
    {
        Vector2 home = awareness.startPosition;
        if (HasClearPath(transform.position, home))
        {
            currentNavPath.Clear();
            currentNavPathIndex = 0;
            MoveTowards(home, patrolSpeed);
        }
        else
        {
            if (currentNavPath == null || currentNavPath.Count == 0 || Time.time >= nextNavRecalcTime)
            {
                currentNavPath = FindPath(transform.position, home);
                currentNavPathIndex = 0;
                nextNavRecalcTime = Time.time + 0.5f;
            }

            if (currentNavPath != null && currentNavPath.Count > 0)
            {
                for (int i = currentNavPath.Count - 1; i > currentNavPathIndex; i--)
                {
                    if (HasClearPath(transform.position, currentNavPath[i]))
                    {
                        currentNavPathIndex = i;
                        break;
                    }
                }

                if (currentNavPathIndex < currentNavPath.Count)
                {
                    Vector2 currentWaypoint = currentNavPath[currentNavPathIndex];
                    if (Vector2.Distance(transform.position, currentWaypoint) < 0.35f)
                    {
                        currentNavPathIndex++;
                    }
                }

                Vector2 targetPos = (currentNavPathIndex < currentNavPath.Count) ?
                    currentNavPath[currentNavPathIndex] : home;

                MoveTowards(targetPos, patrolSpeed);
            }
            else
            {
                MoveTowards(home, patrolSpeed);
            }
        }
    }

    /// <summary>
    /// Kiểm tra xem giữa 2 điểm có đường thẳng thông thoáng không bị vật cản cắt ngang không
    /// </summary>
    public bool HasClearPath(Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0.05f) return true;

        // Dùng bán kính quét an toàn 0.22f (đảm bảo thân quái không bị quẹt góc vật cản)
        RaycastHit2D hit = Physics2D.CircleCast(from, Mathf.Max(obstacleCheckRadius, 0.22f), dir.normalized, dist, obstacleLayer);
        return !hit || hit.collider == null || (player != null && hit.collider.gameObject == player.gameObject);
    }

    private class PathNode
    {
        public int x, y;
        public float g, h;
        public PathNode parent;
        public float f => g + h;
    }

    /// <summary>
    /// Thuật toán tìm đường A* 2D cục bộ kết hợp tối ưu hóa làm mượt góc (String pulling)
    /// </summary>
    private List<Vector2> FindPath(Vector2 start, Vector2 goal)
    {
        float cellSize = 0.35f;
        float margin = 2.5f;

        float minX = Mathf.Min(start.x, goal.x) - margin;
        float maxX = Mathf.Max(start.x, goal.x) + margin;
        float minY = Mathf.Min(start.y, goal.y) - margin;
        float maxY = Mathf.Max(start.y, goal.y) + margin;

        int cols = Mathf.Clamp(Mathf.CeilToInt((maxX - minX) / cellSize), 5, 45);
        int rows = Mathf.Clamp(Mathf.CeilToInt((maxY - minY) / cellSize), 5, 45);

        int startX = Mathf.Clamp(Mathf.FloorToInt((start.x - minX) / cellSize), 0, cols - 1);
        int startY = Mathf.Clamp(Mathf.FloorToInt((start.y - minY) / cellSize), 0, rows - 1);
        int goalX = Mathf.Clamp(Mathf.FloorToInt((goal.x - minX) / cellSize), 0, cols - 1);
        int goalY = Mathf.Clamp(Mathf.FloorToInt((goal.y - minY) / cellSize), 0, rows - 1);

        bool[,] walkable = new bool[cols, rows];
        float pathClearance = Mathf.Max(obstacleCheckRadius, 0.22f);
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                Vector2 pos = new Vector2(minX + (c + 0.5f) * cellSize, minY + (r + 0.5f) * cellSize);
                walkable[c, r] = !Physics2D.OverlapCircle(pos, pathClearance, obstacleLayer);
            }
        }

        // Nếu ô đích bị chạm collider (người chơi đứng sát tường), tìm ô gần nhất đi được
        if (!walkable[goalX, goalY])
        {
            float bestDist = float.MaxValue;
            int bestGx = goalX, bestGy = goalY;
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (walkable[c, r])
                    {
                        float d = (c - goalX) * (c - goalX) + (r - goalY) * (r - goalY);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestGx = c;
                            bestGy = r;
                        }
                    }
                }
            }
            goalX = bestGx;
            goalY = bestGy;
        }

        if (!walkable[startX, startY])
        {
            walkable[startX, startY] = true;
        }

        var open = new List<PathNode>();
        var closed = new bool[cols, rows];
        var nodeGrid = new PathNode[cols, rows];

        var startNode = new PathNode { x = startX, y = startY, g = 0, h = Vector2.Distance(new Vector2(startX, startY), new Vector2(goalX, goalY)) };
        open.Add(startNode);
        nodeGrid[startX, startY] = startNode;

        int[] dx = { 1, -1, 0, 0, 1, -1, 1, -1 };
        int[] dy = { 0, 0, 1, -1, 1, 1, -1, -1 };
        float[] costs = { 1f, 1f, 1f, 1f, 1.414f, 1.414f, 1.414f, 1.414f };

        PathNode current = null;
        PathNode closestToGoal = startNode;
        float minH = startNode.h;

        while (open.Count > 0)
        {
            int bestIdx = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].f < open[bestIdx].f) bestIdx = i;
            }
            current = open[bestIdx];
            open.RemoveAt(bestIdx);

            if (current.h < minH)
            {
                minH = current.h;
                closestToGoal = current;
            }

            if (current.x == goalX && current.y == goalY) break;
            closed[current.x, current.y] = true;

            for (int dir = 0; dir < 8; dir++)
            {
                int nx = current.x + dx[dir];
                int ny = current.y + dy[dir];
                if (nx < 0 || nx >= cols || ny < 0 || ny >= rows) continue;
                if (!walkable[nx, ny] || closed[nx, ny]) continue;
                if (dir >= 4 && (!walkable[current.x, ny] || !walkable[nx, current.y])) continue;

                float newG = current.g + costs[dir];
                PathNode neighbor = nodeGrid[nx, ny];
                if (neighbor == null)
                {
                    neighbor = new PathNode
                    {
                        x = nx, y = ny, g = newG,
                        h = Vector2.Distance(new Vector2(nx, ny), new Vector2(goalX, goalY)),
                        parent = current
                    };
                    nodeGrid[nx, ny] = neighbor;
                    open.Add(neighbor);
                }
                else if (newG < neighbor.g)
                {
                    neighbor.g = newG;
                    neighbor.parent = current;
                }
            }
        }

        PathNode endNode = (current != null && current.x == goalX && current.y == goalY) ? current : closestToGoal;
        if (endNode == null || endNode == startNode) return new List<Vector2>();

        var rawPath = new List<Vector2>();
        var trace = endNode;
        while (trace != null)
        {
            rawPath.Add(new Vector2(minX + (trace.x + 0.5f) * cellSize, minY + (trace.y + 0.5f) * cellSize));
            trace = trace.parent;
        }
        rawPath.Reverse();

        // Tối ưu hóa làm mượt góc (String pulling)
        var smoothPath = new List<Vector2>();
        smoothPath.Add(start);
        int curIndex = 0;
        while (curIndex < rawPath.Count - 1)
        {
            int furthest = curIndex + 1;
            for (int next = rawPath.Count - 1; next > curIndex + 1; next--)
            {
                Vector2 from = smoothPath[smoothPath.Count - 1];
                Vector2 to = rawPath[next];
                if (HasClearPath(from, to))
                {
                    furthest = next;
                    break;
                }
            }
            smoothPath.Add(rawPath[furthest]);
            curIndex = furthest;
        }
        smoothPath.Add(goal);

        return smoothPath;
    }

    private void PatrolBehavior()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                PickNewPatrolTarget();
            }
            return;
        }

        Vector2 currentPos = transform.position;
        Vector2 toTarget = patrolTarget - currentPos;
        float distToTarget = toTarget.magnitude;

        // Nếu đã đến gần mục tiêu tuần tra -> Dừng nghỉ một chút rồi tìm điểm mới
        if (distToTarget < 0.35f)
        {
            StartWaiting();
            return;
        }

        Vector2 desiredDir = toTarget.normalized;

        // Quét kiểm tra vật cản trực diện theo hướng đang đi tuần
        RaycastHit2D frontHit = Physics2D.CircleCast(currentPos, obstacleCheckRadius, desiredDir, obstacleCheckDistance, obstacleLayer);
        if (frontHit && frontHit.collider != null)
        {
            ChangePatrolDirection(frontHit.normal);
            return;
        }

        MoveTowards(patrolTarget, patrolSpeed);
    }

    /// <summary>
    /// Thay đổi hướng đi khi phát hiện hoặc va chạm vật cản.
    /// Tìm hướng mở thông thoáng nhất ra xa bề mặt vật cản và chọn điểm tuần tra mới.
    /// </summary>
    public void ChangePatrolDirection(Vector2? obstacleNormal = null)
    {
        if (Time.time < lastDirectionChangeTime + 0.15f) return;
        lastDirectionChangeTime = Time.time;

        Vector2 currentPos = transform.position;
        Vector2 home = awareness != null ? awareness.startPosition : currentPos;
        Vector2 avoidNormal = Vector2.zero;

        if (obstacleNormal.HasValue && obstacleNormal.Value.sqrMagnitude > 0.01f)
        {
            avoidNormal = obstacleNormal.Value.normalized;
        }
        else if (currentSteeredDirection.sqrMagnitude > 0.01f)
        {
            avoidNormal = -currentSteeredDirection.normalized;
        }
        else
        {
            avoidNormal = Random.insideUnitCircle.normalized;
        }

        Vector2 bestDir = avoidNormal;
        float bestScore = -9999f;
        float bestClearDist = 1f;

        // Quét 16 góc xung quanh để tìm lối thoát thông thoáng nhất
        for (int i = 0; i < 16; i++)
        {
            float angle = i * 22.5f;
            Vector2 dir = RotateVector(avoidNormal, angle);

            RaycastHit2D hit = Physics2D.CircleCast(currentPos, obstacleCheckRadius, dir, patrolRadius, obstacleLayer);
            float clearDist = (hit && hit.collider != null) ? hit.distance : patrolRadius;

            // Bỏ qua nếu góc này bị cản ngay trước mặt (< 0.45m)
            if (clearDist < 0.45f) continue;

            Vector2 candidateEnd = currentPos + dir * clearDist;
            float distFromHome = Vector2.Distance(candidateEnd, home);

            // Đánh giá: ưu tiên đường thoáng dài, hướng xa góc cản
            float normalBonus = Vector2.Dot(dir, avoidNormal) * 2.5f;
            float homePenalty = distFromHome > patrolRadius * 1.5f ? (distFromHome - patrolRadius) * 1.5f : 0f;
            float score = clearDist * 2f + normalBonus - homePenalty;

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
                bestClearDist = clearDist;
            }
        }

        float travelDist = Mathf.Clamp(bestClearDist * 0.85f, 1.2f, patrolRadius);
        patrolTarget = currentPos + bestDir * travelDist;
        currentSteeredDirection = bestDir;
        rb.linearVelocity = bestDir * patrolSpeed * WaterMovementMultiplier;

        if (Mathf.Abs(bestDir.x) > 0.05f)
        {
            transform.localScale = new Vector3(bestDir.x > 0 ? 1 : -1, 1, 1);
        }

        isWaiting = false;
        waitTimer = 0f;
        universalStuckTimer = 0f;
        lastPositionCheck = currentPos;
    }

    private void PickNewPatrolTarget()
    {
        // Thử tìm điểm đến ngẫu nhiên có đường đi thông thoáng
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * patrolRadius;
            if (randomOffset.magnitude < 0.8f) randomOffset = randomOffset.normalized * 0.8f;
            Vector2 candidate = awareness.startPosition + randomOffset;

            // Kiểm tra điểm đích không nằm trong vật cản
            if (Physics2D.OverlapCircle(candidate, obstacleCheckRadius, obstacleLayer)) continue;

            // Kiểm tra đường đi từ vị trí hiện tại đến điểm đích không bị cản
            Vector2 toCandidate = candidate - (Vector2)transform.position;
            float dist = toCandidate.magnitude;
            if (dist > 0.1f)
            {
                RaycastHit2D hit = Physics2D.CircleCast(transform.position, obstacleCheckRadius, toCandidate.normalized, dist, obstacleLayer);
                if (!hit || hit.collider == null)
                {
                    patrolTarget = candidate;
                    return;
                }
            }
        }

        // Nếu các điểm ngẫu nhiên đều bị cản hoặc không có đường thông, đổi hướng theo hướng mở nhất
        ChangePatrolDirection();
    }

    private void StartWaiting() { isWaiting = true; waitTimer = waitTimeAtEdge; }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleObstacleCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleObstacleCollision(collision);
    }

    private void HandleObstacleCollision(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) == 0) return;

        // Tính pháp tuyến tổng hợp của tất cả các điểm va chạm (xử lý góc vuông 2 bức tường)
        Vector2 compositeNormal = Vector2.zero;
        int count = collision.contactCount;
        for (int i = 0; i < count; i++)
        {
            compositeNormal += collision.GetContact(i).normal;
        }

        if (compositeNormal.sqrMagnitude > 0.001f)
        {
            compositeNormal.Normalize();
        }
        else
        {
            compositeNormal = count > 0 ? collision.GetContact(0).normal : Vector2.zero;
        }

        if (currentState == OrcState.Patrol)
        {
            ChangePatrolDirection(compositeNormal);
        }
        else if (currentState == OrcState.Chase || currentState == OrcState.Return)
        {
            if (compositeNormal != Vector2.zero)
            {
                Vector2 targetPos = currentState == OrcState.Chase ? lastSeenPlayerPosition : awareness.startPosition;
                Vector2 dirToTarget = (targetPos - (Vector2)transform.position).normalized;

                Vector2 tangent1 = new Vector2(-compositeNormal.y, compositeNormal.x);
                Vector2 tangent2 = -tangent1;
                Vector2 bestTangent = Vector2.Dot(tangent1, dirToTarget) > Vector2.Dot(tangent2, dirToTarget) ? tangent1 : tangent2;

                // Kiểm tra xem hướng tiếp tuyến có bị bức tường liền kề chặn không (ví dụ trong góc vuông)
                RaycastHit2D tangentHit = Physics2D.CircleCast(transform.position, obstacleCheckRadius, bestTangent, 0.35f, obstacleLayer);
                if (tangentHit && tangentHit.collider != null)
                {
                    // Bị kẹt trong góc tường -> Đẩy hướng ra ngoài góc thay vì ép vào tường tiếp theo
                    currentSteeredDirection = compositeNormal;
                    nextNavRecalcTime = 0f;
                    currentNavPath.Clear();
                }
                else
                {
                    currentSteeredDirection = (bestTangent * 0.75f + compositeNormal * 0.25f).normalized;
                }

                float speed = currentState == OrcState.Chase ? (isSprinting ? sprintSpeed : chaseSpeed) : patrolSpeed;
                rb.linearVelocity = currentSteeredDirection * speed * WaterMovementMultiplier;
            }
        }
        else if (currentState == OrcState.Retreat || currentState == OrcState.Strafe)
        {
            strafeDirection *= -1;
        }
    }

    /// <summary>
    /// Di chuyển đến mục tiêu kết hợp thuật toán né vật cản thông minh (Obstacle Avoidance).
    /// </summary>
    private void MoveTowards(Vector2 targetPosition, float speed)
    {
        Vector2 desiredDirection = (targetPosition - (Vector2)transform.position).normalized;
        Vector2 steeredDir = GetSteeredDirection(desiredDirection);

        // Nếu cần rẽ góc gắt (> 75 độ), tăng tốc độ bẻ lái để không bị trôi đâm vào góc
        float turnSharpness = Vector2.Dot(currentSteeredDirection, steeredDir);
        float effectiveDamping = turnSharpness < 0.25f ? steerDamping * 2.5f : steerDamping;

        currentSteeredDirection = Vector2.Lerp(currentSteeredDirection, steeredDir, Time.deltaTime * effectiveDamping);
        if (currentSteeredDirection.sqrMagnitude < 0.01f) currentSteeredDirection = steeredDir;

        rb.linearVelocity = currentSteeredDirection.normalized * speed * WaterMovementMultiplier;

        // Xoay mặt theo hướng di chuyển thực tế nếu không có Player hoặc đang không đối mặt
        if (currentState != OrcState.Alert && currentState != OrcState.Attack && currentState != OrcState.Retreat && currentState != OrcState.Strafe)
        {
            if (Mathf.Abs(currentSteeredDirection.x) > 0.05f)
            {
                if (currentSteeredDirection.x > 0) transform.localScale = Vector3.one;
                else transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }

    /// <summary>
    /// Thuật toán quét và bẻ lái thông minh đa điểm (Multi-Whisker Context Steering).
    /// Sử dụng tia giữa và 2 tia râu ở hai vai để phát hiện góc tường trước khi bị va quẹt.
    /// </summary>
    private Vector2 GetSteeredDirection(Vector2 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude < 0.001f) return Vector2.zero;

        Vector2 currentPos = transform.position;
        Vector2 perp = new Vector2(-desiredDirection.y, desiredDirection.x) * (obstacleCheckRadius * 0.85f);

        // 1. Quét 3 điểm: Tâm, Vai trái, Vai phải
        RaycastHit2D centerHit = Physics2D.CircleCast(currentPos, obstacleCheckRadius, desiredDirection, obstacleCheckDistance, obstacleLayer);
        RaycastHit2D leftWhisker = Physics2D.Raycast(currentPos + perp, desiredDirection, obstacleCheckDistance, obstacleLayer);
        RaycastHit2D rightWhisker = Physics2D.Raycast(currentPos - perp, desiredDirection, obstacleCheckDistance, obstacleLayer);

        bool hasObstacle = (centerHit && centerHit.collider != null) ||
                           (leftWhisker && leftWhisker.collider != null) ||
                           (rightWhisker && rightWhisker.collider != null);

        // Nếu cả 3 điểm đều thông thoáng -> Đi thẳng
        if (!hasObstacle)
        {
            return desiredDirection;
        }

        // 2. Phân tích hướng cản để ưu tiên bẻ lái ra xa góc
        // Nếu vai trái chạm góc -> Quái phải bẻ lái sang PHẢI
        // Nếu vai phải chạm góc -> Quái phải bẻ lái sang TRÁI
        bool leftBlocked = leftWhisker && leftWhisker.collider != null;
        bool rightBlocked = rightWhisker && rightWhisker.collider != null;

        float[] anglesToTest;
        if (leftBlocked && !rightBlocked)
        {
            anglesToTest = new float[] { -25f, -50f, -75f, -100f, -130f, 25f, 50f, 75f, 100f, 130f, 180f };
        }
        else if (rightBlocked && !leftBlocked)
        {
            anglesToTest = new float[] { 25f, 50f, 75f, 100f, 130f, -25f, -50f, -75f, -100f, -130f, 180f };
        }
        else
        {
            anglesToTest = new float[] { 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 150f, -150f, 180f };
        }

        Vector2 bestDirection = desiredDirection;
        float bestScore = -9999f;
        Vector2 obsNormal = centerHit ? centerHit.normal : (leftBlocked ? leftWhisker.normal : (rightBlocked ? rightWhisker.normal : -desiredDirection));

        for (int i = 0; i < anglesToTest.Length; i++)
        {
            Vector2 candidateDir = RotateVector(desiredDirection, anglesToTest[i]);
            RaycastHit2D candidateHit = Physics2D.CircleCast(currentPos, obstacleCheckRadius, candidateDir, obstacleCheckDistance, obstacleLayer);

            if (!candidateHit || candidateHit.collider == null)
            {
                // Lối đi hoàn toàn thông thoáng
                float dotDesired = Vector2.Dot(candidateDir, desiredDirection);
                float normalBonus = Vector2.Dot(candidateDir, obsNormal) * 0.6f;
                float angleOrder = (anglesToTest.Length - i) * 0.1f;
                float score = 10f + dotDesired + normalBonus + angleOrder;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = candidateDir;
                }
            }
            else
            {
                // Nếu bị cản sát mặt (< 0.2m), bỏ qua
                if (candidateHit.distance < 0.2f) continue;

                float distRatio = candidateHit.distance / obstacleCheckDistance;
                float normalBonus = Vector2.Dot(candidateDir, obsNormal) * 0.5f;
                float score = distRatio * 2f + normalBonus;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = candidateDir;
                }
            }
        }

        return bestDirection;
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos).normalized;
    }

    private void FacePlayer()
    {
        float dx = (canSeePlayer ? player.position.x : lastSeenPlayerPosition.x) - transform.position.x;
        // Chỉ lật mặt trái phải nếu chênh lệch X đáng kể (tránh đổi mặt liên tục khi đứng thẳng hàng dọc)
        if (Mathf.Abs(dx) > 0.05f)
        {
            if (dx > 0) transform.localScale = Vector3.one;
            else transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ bán kính kiểm tra vật cản
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, obstacleCheckRadius);

        // Vẽ hướng né hiện tại và 2 râu cảm biến vai (Whiskers)
        if (currentSteeredDirection.sqrMagnitude > 0.01f)
        {
            Vector2 dir = currentSteeredDirection.normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * (obstacleCheckRadius * 0.85f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + dir * obstacleCheckDistance);
            Gizmos.DrawWireSphere((Vector2)transform.position + dir * obstacleCheckDistance, obstacleCheckRadius);

            // Vẽ 2 tia râu cảm biến góc vật cản
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine((Vector2)transform.position + perp, (Vector2)transform.position + perp + dir * obstacleCheckDistance);
            Gizmos.DrawLine((Vector2)transform.position - perp, (Vector2)transform.position - perp + dir * obstacleCheckDistance);
        }

        // Vẽ mục tiêu tuần tra hiện tại
        if (currentState == OrcState.Patrol)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, patrolTarget);
            Gizmos.DrawWireSphere(patrolTarget, 0.2f);
        }

        // Vẽ lộ trình tìm đường vượt vật cản hiện tại
        if (currentNavPath != null && currentNavPath.Count > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < currentNavPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentNavPath[i], currentNavPath[i + 1]);
                Gizmos.DrawWireSphere(currentNavPath[i + 1], 0.12f);
            }
        }
    }
}
