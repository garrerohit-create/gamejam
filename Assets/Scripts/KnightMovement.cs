using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum KnightActionState
{
    Locomotion,
    LightAttack1,
    LightAttack2,
    HeavyAttack,
    Dodge,
    Roll,
    Parry,
    Heal
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class KnightMovement : MonoBehaviour
{
    [Header("Locomotion")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.15f;

    [Header("Collider Alignment")]
    [SerializeField] private Vector2 baseColliderOffset = new Vector2(-0.48f, 0.0f);
    [SerializeField] private Vector2 standingColliderSize = new Vector2(1.4f, 4.54f);
    [SerializeField] private Vector2 rollColliderSize = new Vector2(1.4f, 2.50f);
    [SerializeField] private float rollColliderOffsetY = -1.02f;

    [Header("Combat References")]
    [SerializeField] private SwordHitbox swordHitbox;
    [SerializeField] private HeavyAttackEffects heavyAttackEffects;

    [Header("Input Bindings")]
    [SerializeField] private KeyCode lightAttackKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode heavyAttackKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode dodgeKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode rollKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode parryKey = KeyCode.E;

    [Header("Light Attacks")]
    [SerializeField] private string lightAttack1Trigger = "LightAttack1";
    [SerializeField] private string lightAttack2Trigger = "LightAttack2";
    [SerializeField] private int lightAttack1Damage = 1;
    [SerializeField] private int lightAttack2Damage = 1;
    [SerializeField] private float lightAttack1Duration = 0.75f;
    [SerializeField] private float lightAttack2Duration = 0.60f;
    [SerializeField] private float comboWindowStart = 0.35f;
    [SerializeField] private float comboWindowEnd = 0.75f;
    [SerializeField] private float lightAttack1HitboxDelay = 0.35f;
    [SerializeField] private float lightAttack1HitboxActive = 0.25f;
    [SerializeField] private float lightAttack2HitboxDelay = 0.20f;
    [SerializeField] private float lightAttack2HitboxActive = 0.24f;

    [Header("Heavy Attack")]
    [SerializeField] private string heavyAttackTrigger = "HeavyAttack";
    [SerializeField] private string heavyChargeBool = "IsChargingHeavy";
    [SerializeField] private int heavyAttackBaseDamage = 2;
    [SerializeField] private float heavyAttackMaxChargeTime = 1.0f;
    [SerializeField] private float heavyAttackMaxMultiplier = 2.0f;
    [SerializeField] private float heavyAttackCooldown = 0.9f;
    [SerializeField] private float heavyAttackHitboxDelay = 0.44f;
    [SerializeField] private float heavyAttackHitboxActive = 0.20f;
    [SerializeField] private float heavyAttackDuration = 0.85f;
    [SerializeField] private float heavyReleaseHitboxDelay = 0.06f;
    [SerializeField] private float heavyReleaseDuration = 0.45f;

    [Header("Dodge")]
    [SerializeField] private string dodgeTrigger = "Dodge";
    [SerializeField] private float dodgeSpeed = 10f;
    [SerializeField] private float dodgeDuration = 0.38f;
    [SerializeField] private float dodgeCooldown = 0.8f;
    [SerializeField] private float dodgeInvulnerableDuration = 0.30f;

    [Header("Roll")]
    [SerializeField] private string rollTrigger = "Roll";
    [SerializeField] private float rollSpeed = 8f;
    [SerializeField] private float rollDuration = 0.55f;
    [SerializeField] private float rollCooldown = 0.9f;
    [SerializeField] private float rollInvulnerableDuration = 0.40f;

    [Header("Parry")]
    [SerializeField] private string parryTrigger = "Parry";
    [SerializeField] private float parryStartup = 0.10f;
    [SerializeField] private float parryActiveDuration = 0.25f;
    [SerializeField] private float parryRecovery = 0.30f;
    [SerializeField] private float parryCooldown = 0.9f;

    [Header("Events")]
    public UnityEvent onParrySuccess;
    public UnityEvent<int> onDamageTaken;

    public event Action OnParrySuccess;
    public event Action<int> OnDamageTaken;

    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CapsuleCollider2D knightCollider;

    // State machine
    private KnightActionState currentState = KnightActionState.Locomotion;
    private int facingDirection = 1; // 1 = right, -1 = left
    private float moveInput;
    private bool isGrounded;
    private Vector2 groundNormal = Vector2.up;
    private bool isOnSlope;
    private float baseGravityScale = 3f;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    // Timers & tracking
    private float attackStateStartTime;
    private bool comboQueued;
    private bool isChargingHeavy;
    private float heavyChargeStartTime;
    private float currentHeavyChargeRatio;
    private bool heavyImpactSpawned;
    private float nextHeavyAttackTime;
    private float nextDodgeTime;
    private float nextRollTime;
    private float nextParryTime;
    private bool isInvulnerable;
    private bool isParryWindowActive;

    private Coroutine activeActionRoutine;
    private Coroutine hitboxRoutine;

    // Public getters
    public KnightActionState CurrentState => currentState;
    public int FacingDirection => facingDirection;
    public bool IsGrounded => isGrounded;
    public bool IsInvulnerable => isInvulnerable;
    public bool IsParrying => currentState == KnightActionState.Parry && isParryWindowActive;
    public SwordHitbox SwordHitboxComponent => swordHitbox;
    public Vector2 BaseColliderOffset => baseColliderOffset;

    private void Awake()
    {
        EnsureComponents();

        if (rb != null)
            baseGravityScale = rb.gravityScale;

        if (swordHitbox != null)
        {
            swordHitbox.DisableHitbox();
            swordHitbox.OnHitEnemy += HandleHitEnemy;
        }

        UpdateFacing(1, force: true);
    }

    private void OnDestroy()
    {
        if (swordHitbox != null)
        {
            swordHitbox.OnHitEnemy -= HandleHitEnemy;
        }
    }

    private void HandleHitEnemy(EnemyHealth enemy, int damageDealt)
    {
        if (damageDealt >= 2 || currentState == KnightActionState.HeavyAttack)
        {
            AudioManager.Play(SoundID.FleshHitHeavy, 1.0f);
        }
        else
        {
            AudioManager.Play(SoundID.FleshHit, 0.85f);
        }

        if (currentState == KnightActionState.HeavyAttack && heavyAttackEffects != null)
        {
            heavyAttackEffects.TriggerHitStop();
        }
    }

    private void OnValidate()
    {
        EnsureComponents();
        if (knightCollider != null)
        {
            knightCollider.size = standingColliderSize;
            knightCollider.offset = new Vector2(
                Mathf.Abs(baseColliderOffset.x) * (baseColliderOffset.x < 0 ? -facingDirection : facingDirection),
                baseColliderOffset.y
            );
        }
    }

    private void EnsureComponents()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (knightCollider == null)
            knightCollider = GetComponent<CapsuleCollider2D>();

        if (swordHitbox == null)
            swordHitbox = GetComponentInChildren<SwordHitbox>();

        if (heavyAttackEffects == null)
            heavyAttackEffects = GetComponent<HeavyAttackEffects>();
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (animator != null && currentState == KnightActionState.Locomotion)
        {
            animator.SetBool("IsRunning", Mathf.Abs(moveInput) > 0.01f);
        }

        // Facing direction only changes during locomotion
        if (currentState == KnightActionState.Locomotion)
        {
            if (moveInput > 0.01f)
                UpdateFacing(1);
            else if (moveInput < -0.01f)
                UpdateFacing(-1);
        }

        CheckGrounded();

        // Platforming Timers (Coyote Time & Jump Buffer)
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        HandleInput();
    }

    private void FixedUpdate()
    {
        // Normal horizontal movement only runs during locomotion
        if (currentState == KnightActionState.Locomotion)
        {
            if (isGrounded && isOnSlope)
            {
                if (Mathf.Abs(moveInput) > 0.01f)
                {
                    rb.gravityScale = 0f;
                    Vector2 slopeTangent = new Vector2(groundNormal.y, -groundNormal.x).normalized;
                    rb.linearVelocity = slopeTangent * (moveInput * moveSpeed);
                }
                else
                {
                    // Prevent sliding down frictionless slope when stationary
                    rb.gravityScale = 0f;
                    rb.linearVelocity = Vector2.zero;
                }
            }
            else
            {
                rb.gravityScale = baseGravityScale;
                rb.linearVelocity = new Vector2(
                    moveInput * moveSpeed,
                    rb.linearVelocity.y
                );
            }
        }
        else
        {
            rb.gravityScale = baseGravityScale;
        }
    }

    private void HandleInput()
    {
        // 1. Jump (handles coyote time, jump buffer, and grounded/locomotion state)
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && currentState == KnightActionState.Locomotion)
        {
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            rb.gravityScale = baseGravityScale;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
            isOnSlope = false;
            AudioManager.Play(SoundID.Jump, 0.85f);
            if (animator != null)
                animator.SetTrigger("Jump");
        }

        // Variable jump height cut when releasing Space mid-ascent
        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 2.0f && currentState == KnightActionState.Locomotion)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        // 2. Light Attack / Combo Input
        if (Input.GetKeyDown(lightAttackKey))
        {
            if (currentState == KnightActionState.Locomotion)
            {
                PerformLightAttack1();
            }
            else if (currentState == KnightActionState.LightAttack1)
            {
                float elapsed = Time.time - attackStateStartTime;
                if (elapsed >= comboWindowStart && elapsed <= comboWindowEnd)
                {
                    PerformLightAttack2();
                }
                else if (elapsed < comboWindowStart)
                {
                    comboQueued = true;
                }
            }
        }

        // Handle queued combo transition
        if (currentState == KnightActionState.LightAttack1 && comboQueued)
        {
            float elapsed = Time.time - attackStateStartTime;
            if (elapsed >= comboWindowStart)
            {
                comboQueued = false;
                PerformLightAttack2();
            }
        }

        // 3. Heavy Attack (hold to charge, release or max time to strike)
        if (Input.GetKeyDown(heavyAttackKey) && currentState == KnightActionState.Locomotion && Time.time >= nextHeavyAttackTime)
        {
            StartHeavyCharge();
        }

        if (isChargingHeavy)
        {
            float chargeDuration = Time.time - heavyChargeStartTime;
            float chargeRatio = Mathf.Clamp01(chargeDuration / heavyAttackMaxChargeTime);
            if (heavyAttackEffects != null)
            {
                heavyAttackEffects.SetCharging(true, chargeRatio);
            }

            if (Input.GetKeyUp(heavyAttackKey) || chargeDuration >= heavyAttackMaxChargeTime)
            {
                ReleaseHeavyAttack(chargeDuration);
            }
        }

        // 4. Dodge
        if (Input.GetKeyDown(dodgeKey) && currentState == KnightActionState.Locomotion && Time.time >= nextDodgeTime)
        {
            PerformDodge();
        }

        // 5. Roll (grounded only)
        if (Input.GetKeyDown(rollKey) && currentState == KnightActionState.Locomotion && isGrounded && Time.time >= nextRollTime)
        {
            PerformRoll();
        }

        // 6. Parry (grounded only)
        if (Input.GetKeyDown(parryKey) && currentState == KnightActionState.Locomotion && isGrounded && Time.time >= nextParryTime)
        {
            PerformParry();
        }
    }

    // --- ABILITY IMPLEMENTATIONS ---

    public void PerformLightAttack1()
    {
        if (currentState != KnightActionState.Locomotion && currentState != KnightActionState.LightAttack1)
            return;

        StopCurrentActions();
        currentState = KnightActionState.LightAttack1;
        attackStateStartTime = Time.time;
        comboQueued = false;

        if (animator != null)
            animator.SetTrigger(lightAttack1Trigger);

        AudioManager.Play(SoundID.SwordSwingLight1, 0.9f);

        if (swordHitbox != null)
        {
            swordHitbox.SetFacing(facingDirection);
            swordHitbox.SetDamage(lightAttack1Damage);
        }

        activeActionRoutine = StartCoroutine(ActionTimeoutRoutine(lightAttack1Duration));
        hitboxRoutine = StartCoroutine(HitboxWindowRoutine(lightAttack1HitboxDelay, lightAttack1HitboxActive, lightAttack1Damage));
    }

    public void PerformLightAttack2()
    {
        StopCurrentActions();
        currentState = KnightActionState.LightAttack2;
        attackStateStartTime = Time.time;
        comboQueued = false;

        if (animator != null)
            animator.SetTrigger(lightAttack2Trigger);

        AudioManager.Play(SoundID.SwordSwingLight2, 0.9f);

        if (swordHitbox != null)
        {
            swordHitbox.SetFacing(facingDirection);
            swordHitbox.SetDamage(lightAttack2Damage);
        }

        activeActionRoutine = StartCoroutine(ActionTimeoutRoutine(lightAttack2Duration));
        hitboxRoutine = StartCoroutine(HitboxWindowRoutine(lightAttack2HitboxDelay, lightAttack2HitboxActive, lightAttack2Damage));
    }

    private void StartHeavyCharge()
    {
        StopCurrentActions();
        currentState = KnightActionState.HeavyAttack;
        isChargingHeavy = true;
        heavyChargeStartTime = Time.time;
        currentHeavyChargeRatio = 0f;
        heavyImpactSpawned = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null && !string.IsNullOrEmpty(heavyChargeBool))
        {
            animator.SetBool(heavyChargeBool, true);
        }

        if (heavyAttackEffects != null)
        {
            heavyAttackEffects.SetCharging(true, 0f);
        }

        AudioManager.Play(SoundID.SwordCharge, 0.85f);
    }

    public void ReleaseHeavyAttack(float chargeDuration)
    {
        isChargingHeavy = false;
        nextHeavyAttackTime = Time.time + heavyAttackCooldown;

        currentHeavyChargeRatio = Mathf.Clamp01(chargeDuration / heavyAttackMaxChargeTime);
        float multiplier = Mathf.Lerp(1f, heavyAttackMaxMultiplier, currentHeavyChargeRatio);
        int finalDamage = Mathf.Max(heavyAttackBaseDamage, Mathf.RoundToInt(heavyAttackBaseDamage * multiplier));

        bool wasCharging = chargeDuration >= 0.15f;

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(heavyChargeBool))
                animator.SetBool(heavyChargeBool, false);

            animator.SetTrigger(heavyAttackTrigger);
        }

        AudioManager.Play(SoundID.SwordSwingHeavy, 1.0f);

        if (heavyAttackEffects != null)
        {
            heavyAttackEffects.SetCharging(false, 0f);
        }

        if (swordHitbox != null)
        {
            swordHitbox.SetFacing(facingDirection);
            swordHitbox.SetDamage(finalDamage);
        }

        float hitboxDelay = wasCharging ? heavyReleaseHitboxDelay : heavyAttackHitboxDelay;
        float actionDuration = wasCharging ? heavyReleaseDuration : heavyAttackDuration;

        activeActionRoutine = StartCoroutine(ActionTimeoutRoutine(actionDuration));
        hitboxRoutine = StartCoroutine(HitboxWindowRoutine(hitboxDelay, heavyAttackHitboxActive, finalDamage));
    }

    public void PerformHeavyAttack(float chargeDuration = 0f)
    {
        if (currentState != KnightActionState.Locomotion && currentState != KnightActionState.HeavyAttack)
            return;

        StartHeavyCharge();
        ReleaseHeavyAttack(chargeDuration);
    }

    public void PerformDodge()
    {
        if (currentState != KnightActionState.Locomotion)
            return;

        StopCurrentActions();
        currentState = KnightActionState.Dodge;
        nextDodgeTime = Time.time + dodgeCooldown;

        if (animator != null)
            animator.SetTrigger(dodgeTrigger);

        AudioManager.Play(SoundID.DodgeWhoosh, 0.85f);

        activeActionRoutine = StartCoroutine(DodgeRoutine());
    }

    private IEnumerator DodgeRoutine()
    {
        isInvulnerable = true;
        rb.linearVelocity = new Vector2(facingDirection * dodgeSpeed, rb.linearVelocity.y);

        float iFrameTime = Mathf.Min(dodgeInvulnerableDuration, dodgeDuration);
        yield return new WaitForSeconds(iFrameTime);
        isInvulnerable = false;

        float remaining = dodgeDuration - iFrameTime;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        ReturnToLocomotion();
    }

    public void PerformRoll()
    {
        if (currentState != KnightActionState.Locomotion || !isGrounded)
            return;

        StopCurrentActions();
        currentState = KnightActionState.Roll;
        nextRollTime = Time.time + rollCooldown;

        if (animator != null)
            animator.SetTrigger(rollTrigger);

        AudioManager.Play(SoundID.RollSlide, 0.85f);

        // Adjust collider for rolling
        if (knightCollider != null)
        {
            knightCollider.size = rollColliderSize;
            knightCollider.offset = new Vector2(baseColliderOffset.x * facingDirection, rollColliderOffsetY);
            Physics2D.SyncTransforms();
        }

        activeActionRoutine = StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        isInvulnerable = true;
        rb.linearVelocity = new Vector2(facingDirection * rollSpeed, rb.linearVelocity.y);

        float iFrameTime = Mathf.Min(rollInvulnerableDuration, rollDuration);
        yield return new WaitForSeconds(iFrameTime);
        isInvulnerable = false;

        float remaining = rollDuration - iFrameTime;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        // Restore collider
        RestoreCollider();
        ReturnToLocomotion();
    }

    public void PerformParry()
    {
        if (currentState != KnightActionState.Locomotion || !isGrounded)
            return;

        StopCurrentActions();
        currentState = KnightActionState.Parry;
        nextParryTime = Time.time + parryCooldown;
        isParryWindowActive = false;

        // Halt horizontal movement during parry
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
            animator.SetTrigger(parryTrigger);

        activeActionRoutine = StartCoroutine(ParryRoutine());
    }

    private IEnumerator ParryRoutine()
    {
        // Startup phase
        if (parryStartup > 0f)
            yield return new WaitForSeconds(parryStartup);

        // Active parry deflection window
        isParryWindowActive = true;
        if (parryActiveDuration > 0f)
            yield return new WaitForSeconds(parryActiveDuration);

        // Recovery phase
        isParryWindowActive = false;
        if (parryRecovery > 0f)
            yield return new WaitForSeconds(parryRecovery);

        ReturnToLocomotion();
    }

    // --- PARRY & DAMAGE RECEPTION INTERFACE ---

    public bool TryParry(int incomingDamage, GameObject attacker = null)
    {
        if (IsParrying)
        {
            Debug.Log(string.Format("Knight PARRIED {0} damage from {1}!", incomingDamage, attacker != null ? attacker.name : "attack"));
            AudioManager.Play(SoundID.ParryClash, 1.0f);
            onParrySuccess?.Invoke();
            OnParrySuccess?.Invoke();
            return true;
        }

        return false;
    }

    public bool TakeDamage(int damage, GameObject attacker = null, bool canBeParried = true)
    {
        // 1. Check parry (only if attack is parryable)
        if (canBeParried && TryParry(damage, attacker))
            return false;

        // 2. Check i-frames from Dodge or Roll
        if (isInvulnerable)
        {
            Debug.Log("Knight avoided damage via invulnerability frames!");
            return false;
        }

        // 3. Take damage
        Debug.Log(string.Format("Knight took {0} damage from {1}!", damage, attacker != null ? attacker.name : "attack"));
        AudioManager.Play(SoundID.KnightHurt, 0.95f);

        // Spawn visceral blood splatter on player
        Vector2 bloodDir = attacker != null ? ((Vector2)(transform.position - attacker.transform.position)).normalized + Vector2.up * 0.4f : Vector2.up + Vector2.right * (spriteRenderer != null && spriteRenderer.flipX ? 1f : -1f);
        BloodSplatterSystem.SpawnBlood(transform.position + new Vector3(0f, 1.4f, 0f), bloodDir, damage >= 2);

        // Trigger rich bleeding animation on player (arterial slash arc, trauma flash, and ground-dripping blood)
        var bleed = GetComponent<BleedWoundEffect>();
        if (bleed == null) bleed = gameObject.AddComponent<BleedWoundEffect>();
        bleed.TriggerBleed(transform.position + new Vector3(0f, 1.4f, 0f), bloodDir, damage >= 2);

        onDamageTaken?.Invoke(damage);
        OnDamageTaken?.Invoke(damage);
        return true;
    }

    // --- HITBOX TIMING & ANIMATION EVENTS ---

    private IEnumerator HitboxWindowRoutine(float delay, float activeTime, int dmg)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        OpenSwordHitboxInternal(dmg);

        if (activeTime > 0f)
            yield return new WaitForSeconds(activeTime);

        CloseSwordHitboxInternal();
        hitboxRoutine = null;
    }

    public void OpenSwordHitbox()
    {
        if (hitboxRoutine != null)
        {
            StopCoroutine(hitboxRoutine);
            hitboxRoutine = null;
        }

        int dmg = lightAttack1Damage;
        if (currentState == KnightActionState.LightAttack2)
            dmg = lightAttack2Damage;
        else if (currentState == KnightActionState.HeavyAttack)
            dmg = swordHitbox != null ? swordHitbox.Damage : heavyAttackBaseDamage;

        OpenSwordHitboxInternal(dmg);
    }

    public void CloseSwordHitbox()
    {
        if (hitboxRoutine != null)
        {
            StopCoroutine(hitboxRoutine);
            hitboxRoutine = null;
        }

        CloseSwordHitboxInternal();
    }

    private void OpenSwordHitboxInternal(int dmg)
    {
        if (swordHitbox != null)
        {
            swordHitbox.SetFacing(facingDirection);
            swordHitbox.EnableHitbox(dmg);
        }

        if (currentState == KnightActionState.HeavyAttack && !heavyImpactSpawned)
        {
            heavyImpactSpawned = true;
            TriggerHeavyImpactVFX();
        }
    }

    private void TriggerHeavyImpactVFX()
    {
        AudioManager.Play(SoundID.HeavyImpact, 1.0f);
        if (heavyAttackEffects != null)
        {
            Vector3 impactPos = transform.position + new Vector3(facingDirection * 1.5f, -2.0f, 0f);
            heavyAttackEffects.SpawnHeavyImpact(impactPos, facingDirection, currentHeavyChargeRatio);
        }
    }

    private void CloseSwordHitboxInternal()
    {
        if (swordHitbox != null)
        {
            swordHitbox.DisableHitbox();
        }
    }

    // Animation Event hooks for Parry
    public void StartParryWindow()
    {
        if (currentState == KnightActionState.Parry)
            isParryWindowActive = true;
    }

    public void EndParryWindow()
    {
        if (currentState == KnightActionState.Parry)
            isParryWindowActive = false;
    }

    // --- STATE CLEANUP & UTILITIES ---

    private IEnumerator ActionTimeoutRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        ReturnToLocomotion();
    }

    public void ReturnToLocomotion()
    {
        StopCurrentActions();
        RestoreCollider();
        isInvulnerable = false;
        isParryWindowActive = false;
        isChargingHeavy = false;
        heavyImpactSpawned = false;
        comboQueued = false;
        currentState = KnightActionState.Locomotion;

        if (rb != null)
        {
            rb.gravityScale = baseGravityScale;
        }

        if (animator != null && !string.IsNullOrEmpty(heavyChargeBool))
        {
            animator.SetBool(heavyChargeBool, false);
        }

        if (heavyAttackEffects != null)
        {
            heavyAttackEffects.SetCharging(false, 0f);
        }

        if (swordHitbox != null)
        {
            swordHitbox.DisableHitbox();
            swordHitbox.ResetDamage();
        }
    }

    private void StopCurrentActions()
    {
        if (activeActionRoutine != null)
        {
            StopCoroutine(activeActionRoutine);
            activeActionRoutine = null;
        }

        if (hitboxRoutine != null)
        {
            StopCoroutine(hitboxRoutine);
            hitboxRoutine = null;
        }

        if (swordHitbox != null)
        {
            swordHitbox.DisableHitbox();
        }
    }

    public void TriggerHealAnimation(float duration = 0.75f)
    {
        if (currentState != KnightActionState.Locomotion)
            return;

        StopCurrentActions();
        currentState = KnightActionState.Heal;

        if (animator != null)
            animator.SetTrigger("Heal");

        AudioManager.Play(SoundID.HealCast, 1.0f);

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        StartCoroutine(HealLockRoutine(duration));
    }

    private IEnumerator HealLockRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (currentState == KnightActionState.Heal)
        {
            currentState = KnightActionState.Locomotion;

        if (rb != null)
        {
            rb.gravityScale = baseGravityScale;
        }
        }
    }

    private void RestoreCollider()
    {
        if (knightCollider != null)
        {
            knightCollider.size = standingColliderSize;
            knightCollider.offset = new Vector2(
                Mathf.Abs(baseColliderOffset.x) * (baseColliderOffset.x < 0 ? -facingDirection : facingDirection),
                baseColliderOffset.y
            );
            Physics2D.SyncTransforms();
        }
    }

    public void UpdateFacing(int direction, bool force = false)
    {
        EnsureComponents();

        int newFacing = direction >= 0 ? 1 : -1;
        if (!force && newFacing == facingDirection)
            return;

        facingDirection = newFacing;

        // Visual flip
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (facingDirection == -1);
        }

        // Mirror physics collider offset
        if (knightCollider != null)
        {
            float currentY = currentState == KnightActionState.Roll ? rollColliderOffsetY : baseColliderOffset.y;
            knightCollider.offset = new Vector2(
                Mathf.Abs(baseColliderOffset.x) * (baseColliderOffset.x < 0 ? -facingDirection : facingDirection),
                currentY
            );
        }

        // Reposition sword hitbox child
        if (swordHitbox != null)
        {
            swordHitbox.SetFacing(facingDirection);
        }

        Physics2D.SyncTransforms();
    }

    private void CheckGrounded()
    {
        if (knightCollider == null)
            knightCollider = GetComponent<CapsuleCollider2D>();

        if (knightCollider == null)
            return;

        Bounds bounds = knightCollider.bounds;
        float footInset = bounds.extents.x * 0.65f; // Check left, center, right feet
        float originY = bounds.min.y + 0.10f;
        float rayLength = Mathf.Max(0.32f, groundCheckDistance + 0.18f);

        Vector2 centerFoot = new Vector2(bounds.center.x, originY);
        Vector2 leftFoot = new Vector2(bounds.center.x - footInset, originY);
        Vector2 rightFoot = new Vector2(bounds.center.x + footInset, originY);

        RaycastHit2D hitCenter = Physics2D.Raycast(centerFoot, Vector2.down, rayLength, groundLayer);
        RaycastHit2D hitLeft = Physics2D.Raycast(leftFoot, Vector2.down, rayLength, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(rightFoot, Vector2.down, rayLength, groundLayer);

        bool wasGrounded = isGrounded;
        isGrounded = hitCenter.collider != null || hitLeft.collider != null || hitRight.collider != null;

        RaycastHit2D bestHit = hitCenter.collider != null ? hitCenter : (hitRight.collider != null ? hitRight : hitLeft);
        if (bestHit.collider != null)
        {
            groundNormal = bestHit.normal;
            isOnSlope = isGrounded && groundNormal.y < 0.99f && groundNormal.y > 0.25f;
        }
        else
        {
            groundNormal = Vector2.up;
            isOnSlope = false;
        }

        // If newly landed, ensure animator clears jump trigger cleanly
        if (!wasGrounded && isGrounded)
        {
            AudioManager.Play(SoundID.LandThud, 0.70f);
            if (animator != null)
            {
                animator.ResetTrigger("Jump");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col == null)
            return;

        Bounds bounds = col.bounds;
        float footInset = bounds.extents.x * 0.72f;
        float originY = bounds.min.y + 0.06f;
        float rayLength = Mathf.Max(0.24f, groundCheckDistance + 0.08f);

        Gizmos.color = isGrounded ? Color.green : Color.yellow;
        Gizmos.DrawLine(new Vector2(bounds.center.x, originY), new Vector2(bounds.center.x, originY - rayLength));
        Gizmos.DrawLine(new Vector2(bounds.center.x - footInset, originY), new Vector2(bounds.center.x - footInset, originY - rayLength));
        Gizmos.DrawLine(new Vector2(bounds.center.x + footInset, originY), new Vector2(bounds.center.x + footInset, originY - rayLength));
    }
}
