using System.Collections;
using UnityEngine;

public enum ArmoredGuardState
{
    Idle,
    Patrol,
    Chase,
    AttackWindup,
    Attacking,
    Hit,
    Dead
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(EnemyHealth))]
public class ArmoredGuardAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 2.0f;
    [SerializeField] private float chaseSpeed = 4.2f;
    [SerializeField] private float idleWaitDuration = 1.5f;
    [SerializeField] private float patrolDistance = 5.0f;

    [Header("Detection & Ranges")]
    [SerializeField] private float detectionRadius = 6.5f;
    [SerializeField] private float disengageRadius = 10.0f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform targetPlayer;

    [Header("Ground & Ledge Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float ledgeCheckDistance = 1.2f;
    [SerializeField] private float wallCheckDistance = 0.8f;
    [SerializeField] private float groundCheckOffsetForward = 0.8f;

    [Header("Combat Configuration")]
    [SerializeField] private EnemyAttackHitbox attackHitbox;
    [SerializeField] private int lightAttackDamage = 1;
    [SerializeField] private int heavyAttackDamage = 2;
    [SerializeField] private float attackCooldown = 1.4f;
    [SerializeField] private float attackWindupDuration = 0.25f;
    [SerializeField] private float deathCleanupDelay = 2.5f;

    [Header("Animation Parameter Names")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string lightAttack1Trigger = "LightAttack1";
    [SerializeField] private string lightAttack2Trigger = "LightAttack2";
    [SerializeField] private string heavySlamTrigger = "HeavySlam";
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string deathTrigger = "Death";
    [SerializeField] private string isDeadBool = "IsDead";

    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;
    private EnemyHealth health;

    // State Tracking
    private ArmoredGuardState currentState = ArmoredGuardState.Idle;
    private int facingDirection = 1; // 1 = right, -1 = left
    private float desiredVelocityX = 0f;
    private Vector2 spawnPosition;
    private float stateTimer;
    private float nextAttackTime;
    private Coroutine activeCombatRoutine;
    private bool attackHitboxOpen;
    private int attackCount;

    public ArmoredGuardState CurrentState => currentState;
    public int FacingDirection => facingDirection;
    public bool IsDead => currentState == ArmoredGuardState.Dead;

    private void Awake()
    {
        EnsureComponents();
        spawnPosition = transform.position;
        facingDirection = 1;
        SubscribeToHealth();
    }

    private void OnValidate()
    {
        EnsureComponents();
    }

    private void Start()
    {
        if (targetPlayer == null)
        {
            var knight = FindAnyObjectByType<KnightMovement>();
            if (knight != null)
                targetPlayer = knight.transform;
        }

        UpdateFacing(facingDirection, force: true);
        EnterState(ArmoredGuardState.Idle);
    }

    private void OnEnable()
    {
        EnsureComponents();
        SubscribeToHealth();
    }

    private void OnDisable()
    {
        UnsubscribeFromHealth();
    }

    private void OnDestroy()
    {
        UnsubscribeFromHealth();
    }

    private bool isSubscribed;

    public void SubscribeToHealth()
    {
        EnsureComponents();
        if (!isSubscribed && health != null)
        {
            health.SetAutoDestroy(false); // ArmoredGuardAI handles death animation & destruction
            health.OnDamaged += HandleDamaged;
            health.OnDeath += HandleDeath;
            isSubscribed = true;
        }
    }

    public void UnsubscribeFromHealth()
    {
        if (isSubscribed && health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDeath -= HandleDeath;
            isSubscribed = false;
        }
    }

    private void EnsureComponents()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (health == null) health = GetComponent<EnemyHealth>();
        if (attackHitbox == null) attackHitbox = GetComponentInChildren<EnemyAttackHitbox>();
    }

    private void Update()
    {
        if (currentState == ArmoredGuardState.Dead)
            return;

        LocatePlayerIfMissing();

        switch (currentState)
        {
            case ArmoredGuardState.Idle:
                UpdateIdle();
                break;
            case ArmoredGuardState.Patrol:
                UpdatePatrol();
                break;
            case ArmoredGuardState.Chase:
                UpdateChase();
                break;
            case ArmoredGuardState.AttackWindup:
                UpdateAttackWindup();
                break;
            case ArmoredGuardState.Attacking:
                // Driven by animation & coroutine
                desiredVelocityX = 0f;
                break;
            case ArmoredGuardState.Hit:
                // Stagger timeout
                desiredVelocityX = 0f;
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    EnterState(ArmoredGuardState.Chase);
                }
                break;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (currentState == ArmoredGuardState.Dead)
            return;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(desiredVelocityX, rb.linearVelocity.y);
        }
    }

    private void LocatePlayerIfMissing()
    {
        if (targetPlayer == null)
        {
            var knight = FindAnyObjectByType<KnightMovement>();
            if (knight != null)
                targetPlayer = knight.transform;
        }
    }

    // --- STATE MACHINE LOGIC ---

    private void EnterState(ArmoredGuardState newState)
    {
        if (currentState == ArmoredGuardState.Dead)
            return;

        currentState = newState;

        switch (newState)
        {
            case ArmoredGuardState.Idle:
                stateTimer = idleWaitDuration;
                desiredVelocityX = 0f;
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;

            case ArmoredGuardState.Patrol:
                break;

            case ArmoredGuardState.Chase:
                break;

            case ArmoredGuardState.AttackWindup:
                desiredVelocityX = 0f;
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                stateTimer = attackWindupDuration;
                if (targetPlayer != null)
                {
                    float diffX = targetPlayer.position.x - transform.position.x;
                    if (Mathf.Abs(diffX) > 0.1f)
                    {
                        UpdateFacing(diffX > 0 ? 1 : -1, force: true);
                    }
                }
                break;

            case ArmoredGuardState.Attacking:
                desiredVelocityX = 0f;
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;

            case ArmoredGuardState.Hit:
                stateTimer = 0.45f;
                desiredVelocityX = 0f;
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (animator != null && !string.IsNullOrEmpty(hitTrigger))
                    animator.SetTrigger(hitTrigger);
                break;

            case ArmoredGuardState.Dead:
                desiredVelocityX = 0f;
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }
                if (bodyCollider != null) bodyCollider.enabled = false;
                if (attackHitbox != null) attackHitbox.DisableHitbox();

                if (animator != null)
                {
                    if (!string.IsNullOrEmpty(isDeadBool)) animator.SetBool(isDeadBool, true);
                    if (!string.IsNullOrEmpty(deathTrigger)) animator.SetTrigger(deathTrigger);
                }

                Destroy(gameObject, deathCleanupDelay);
                break;
        }
    }

    private void UpdateIdle()
    {
        // 1. Check player detection
        if (IsPlayerInDetectionRange())
        {
            EnterState(ArmoredGuardState.Chase);
            return;
        }

        // 2. Idle timer to patrol
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            UpdateFacing(-facingDirection);
            EnterState(ArmoredGuardState.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        // 1. Check player detection
        if (IsPlayerInDetectionRange())
        {
            EnterState(ArmoredGuardState.Chase);
            return;
        }

        // 2. Ledge & Wall checks
        if (IsLedgeAhead() || IsWallAhead())
        {
            desiredVelocityX = 0f;
            EnterState(ArmoredGuardState.Idle);
            return;
        }

        // 3. Patrol distance limit from spawn
        float distanceFromSpawn = transform.position.x - spawnPosition.x;
        if ((facingDirection > 0 && distanceFromSpawn >= patrolDistance) ||
            (facingDirection < 0 && distanceFromSpawn <= -patrolDistance))
        {
            desiredVelocityX = 0f;
            EnterState(ArmoredGuardState.Idle);
            return;
        }

        // Move horizontally
        desiredVelocityX = facingDirection * patrolSpeed;
    }

    private void UpdateChase()
    {
        if (targetPlayer == null)
        {
            desiredVelocityX = 0f;
            EnterState(ArmoredGuardState.Patrol);
            return;
        }

        float distToPlayer = Vector2.Distance(transform.position, targetPlayer.position);

        // Disengage check
        if (distToPlayer > disengageRadius)
        {
            desiredVelocityX = 0f;
            EnterState(ArmoredGuardState.Idle);
            return;
        }

        // Direction to player with deadzone hysteresis to avoid rapid flipping
        float diffX = targetPlayer.position.x - transform.position.x;
        if (Mathf.Abs(diffX) > 0.35f)
        {
            UpdateFacing(diffX > 0 ? 1 : -1);
        }

        // Attack range check
        if (distToPlayer <= attackRange)
        {
            desiredVelocityX = 0f;
            if (Time.time >= nextAttackTime)
            {
                EnterState(ArmoredGuardState.AttackWindup);
            }
            return;
        }

        // Stop chasing off ledges or into walls
        if (IsLedgeAhead() || IsWallAhead())
        {
            desiredVelocityX = 0f;
            return;
        }

        // Chase movement
        desiredVelocityX = facingDirection * chaseSpeed;
    }

    private void UpdateAttackWindup()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ExecuteAttackChoice();
        }
    }

    private void ExecuteAttackChoice()
    {
        EnterState(ArmoredGuardState.Attacking);
        nextAttackTime = Time.time + attackCooldown;

        attackCount++;
        bool isHeavy = (attackCount % 3 == 0); // Every 3rd attack is a heavy slam
        int dmg = isHeavy ? heavyAttackDamage : lightAttackDamage;
        string trigger = isHeavy ? heavySlamTrigger : (attackCount % 2 == 1 ? lightAttack1Trigger : lightAttack2Trigger);

        if (animator != null && !string.IsNullOrEmpty(trigger))
        {
            animator.SetTrigger(trigger);
        }

        AudioManager.Play(SoundID.GauntletStrike, 0.9f);

        float totalDuration = isHeavy ? 0.96f : 0.60f;
        float fallbackOpen = isHeavy ? 0.48f : 0.20f;
        float fallbackClose = isHeavy ? 0.75f : 0.40f;

        if (activeCombatRoutine != null)
            StopCoroutine(activeCombatRoutine);

        activeCombatRoutine = StartCoroutine(AttackTimeoutRoutine(totalDuration, fallbackOpen, fallbackClose, dmg));
    }

    private IEnumerator AttackTimeoutRoutine(float duration, float openTime, float closeTime, int dmg)
    {
        // Fallback open if animation event didn't fire
        yield return new WaitForSeconds(openTime);
        if (!attackHitboxOpen)
        {
            OpenAttackHitboxInternal(dmg);
        }

        // Fallback close
        yield return new WaitForSeconds(Mathf.Max(0.05f, closeTime - openTime));
        CloseAttackHitboxInternal();

        // Recovery remaining
        float remaining = Mathf.Max(0.05f, duration - closeTime);
        yield return new WaitForSeconds(remaining);

        activeCombatRoutine = null;
        EnterState(ArmoredGuardState.Chase);
    }

    // --- ANIMATION EVENT HOOKS ---

    public void OpenAttackHitbox()
    {
        if (currentState == ArmoredGuardState.Attacking)
        {
            int dmg = (attackCount % 3 == 0) ? heavyAttackDamage : lightAttackDamage;
            OpenAttackHitboxInternal(dmg);
        }
    }

    public void CloseAttackHitbox()
    {
        CloseAttackHitboxInternal();
    }

    private void OpenAttackHitboxInternal(int dmg)
    {
        attackHitboxOpen = true;
        if (attackHitbox != null)
        {
            attackHitbox.SetFacing(facingDirection);
            attackHitbox.EnableHitbox(dmg);
        }
    }

    private void CloseAttackHitboxInternal()
    {
        attackHitboxOpen = false;
        if (attackHitbox != null)
        {
            attackHitbox.DisableHitbox();
        }
    }

    // --- DAMAGE & DEATH HANDLERS ---

    private void HandleDamaged(int damageTaken)
    {
        if (currentState == ArmoredGuardState.Dead)
            return;

        AudioManager.Play(SoundID.ArmorClang, 1.0f);

        if (activeCombatRoutine != null)
        {
            StopCoroutine(activeCombatRoutine);
            activeCombatRoutine = null;
        }

        CloseAttackHitboxInternal();
        EnterState(ArmoredGuardState.Hit);
    }

    private void HandleDeath()
    {
        if (currentState == ArmoredGuardState.Dead)
            return;

        if (activeCombatRoutine != null)
        {
            StopCoroutine(activeCombatRoutine);
            activeCombatRoutine = null;
        }

        CloseAttackHitboxInternal();
        EnterState(ArmoredGuardState.Dead);
    }

    // --- NAVIGATION & SENSORS ---

    public void UpdateFacing(int direction, bool force = false)
    {
        EnsureComponents();

        if (!force && (currentState == ArmoredGuardState.Attacking || currentState == ArmoredGuardState.Hit || currentState == ArmoredGuardState.Dead))
            return;

        int newFacing = direction >= 0 ? 1 : -1;
        if (!force && newFacing == facingDirection)
            return;

        facingDirection = newFacing;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (facingDirection == -1);
        }

        if (attackHitbox != null)
        {
            attackHitbox.SetFacing(facingDirection);
        }
    }

    private bool IsPlayerInDetectionRange()
    {
        if (targetPlayer == null)
            return false;

        float dist = Vector2.Distance(transform.position, targetPlayer.position);
        return dist <= detectionRadius;
    }

    private bool IsLedgeAhead()
    {
        Vector2 checkOrigin = (Vector2)transform.position + new Vector2(facingDirection * groundCheckOffsetForward, 0.2f);
        RaycastHit2D hit = Physics2D.Raycast(checkOrigin, Vector2.down, ledgeCheckDistance, groundLayer);
        return hit.collider == null;
    }

    private bool IsWallAhead()
    {
        Vector2 checkOrigin = (Vector2)transform.position + new Vector2(0f, 0.8f);
        RaycastHit2D hit = Physics2D.Raycast(checkOrigin, Vector2.right * facingDirection, wallCheckDistance, groundLayer);
        return hit.collider != null;
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat(speedParam, horizontalSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Ledge check
        Gizmos.color = Color.cyan;
        Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(facingDirection * groundCheckOffsetForward, 0.2f);
        Gizmos.DrawLine(ledgeOrigin, ledgeOrigin + Vector2.down * ledgeCheckDistance);

        // Wall check
        Gizmos.color = Color.magenta;
        Vector2 wallOrigin = (Vector2)transform.position + new Vector2(0f, 0.8f);
        Gizmos.DrawLine(wallOrigin, wallOrigin + Vector2.right * facingDirection * wallCheckDistance);
    }
}
