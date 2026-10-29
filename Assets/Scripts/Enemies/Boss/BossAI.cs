using System;
using System.Collections;
using UnityEngine;

public enum BossState
{
    Dormant,
    RoarIntro,
    Idle,
    Chase,
    AttackSlash,
    AttackSlam,
    AttackShockwave,
    Hurt,
    Dead
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(EnemyHealth))]
public class BossAI : MonoBehaviour
{
    [Header("Boss Identity")]
    [SerializeField] private string bossName = "MALAKOR, THE ABYSSAL WARLORD";
    [SerializeField] private int bossMaxHealth = 45;

    [Header("Locomotion")]
    [SerializeField] private float chaseSpeed = 3.8f;
    [SerializeField] private float chaseSpeedPhase2 = 5.5f;
    [SerializeField] private float attackRangeMelee = 2.8f;
    [SerializeField] private float attackRangeSlam = 4.8f;

    [Header("Combat Stats")]
    [SerializeField] private int slashDamage = 2;
    [SerializeField] private int slamDamage = 3;
    [SerializeField] private int shockwaveDamage = 2;
    [SerializeField] private float attackCooldown = 1.3f;
    [SerializeField] private float attackCooldownPhase2 = 0.65f;

    [Header("Poise & Hyper-Armor")]
    [SerializeField] private int maxPoise = 10;
    private int currentPoise;
    private float poiseRegenTimer = 0f;
    private bool isHyperArmored = false;

    [Header("Phase State")]
    private bool isPhase2 = false;
    private bool isPhaseTransitioning = false;

    [Header("Skull Perch Cinematic")]
    [SerializeField] private Sprite sittingSprite;
    [SerializeField] private Sprite riseSprite;
    [SerializeField] private Sprite leapApexSprite;
    [SerializeField] private Vector3 skullPerchPosition = new Vector3(74.5f, 5.8f, 0f);
    [SerializeField] private Vector3 arenaLandingPosition = new Vector3(73.5f, -0.08f, 0f);

    [Header("References")]
    [SerializeField] private BossAttackHitbox meleeHitbox;
    [SerializeField] private GameObject shockwaveProjectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;

    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;
    private EnemyHealth health;
    private Transform player;

    // State Tracking
    private BossState currentState = BossState.Dormant;
    private int facingDirection = -1; // -1 = left
    private float nextActionTime;
    private bool hasTriggeredIntro = false;
    private int attackCycle = 0;
    private Coroutine activeAttackRoutine = null;

    // Static Events
    public static event Action OnBossDefeated;
    public static event Action<BossAI> OnBossAwakened;
    public static event Action<BossAI, int> OnBossPhaseChanged; // (boss, phase)

    public string BossName => bossName;
    public EnemyHealth Health
    {
        get
        {
            if (health == null) health = GetComponent<EnemyHealth>();
            return health;
        }
    }
    public BossState CurrentState => currentState;
    public bool IsDead => currentState == BossState.Dead;
    public bool IsPhase2 => isPhase2;

    private void EnsureComponents()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (health == null) health = GetComponent<EnemyHealth>();
        if (meleeHitbox == null) meleeHitbox = GetComponentInChildren<BossAttackHitbox>();
    }

    private void LoadDefaultSittingSprites()
    {
#if UNITY_EDITOR
        if (sittingSprite == null)
            sittingSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Enemies/Boss/Sprites/Boss_Sitting_0.png");
        if (riseSprite == null)
            riseSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Enemies/Boss/Sprites/Boss_Sitting_Rise.png");
        if (leapApexSprite == null)
            leapApexSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Enemies/Boss/Sprites/Boss_Slam_1.png");
#endif
    }

    private void OnValidate()
    {
        EnsureComponents();
        LoadDefaultSittingSprites();
        if (currentState == BossState.Dormant && !Application.isPlaying)
        {
            SetupPerchedState();
        }
    }

    private void Awake()
    {
        EnsureComponents();
        LoadDefaultSittingSprites();

        if (health != null)
        {
            health.SetMaxHealth(bossMaxHealth);
            health.SetAutoDestroy(false);
            health.OnDamaged += HandleDamaged;
            health.OnDeath += HandleDeath;
        }

        currentPoise = maxPoise;
        UpdateFacing(-1);

        // Initial state: perched high upon the Giant Skull in the background
        if (currentState == BossState.Dormant)
        {
            SetupPerchedState();
        }
    }

    private void SetupPerchedState()
    {
        transform.position = skullPerchPosition;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        if (animator != null)
        {
            animator.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = -4; // Rendered in background layer atop skull
            if (sittingSprite != null)
            {
                spriteRenderer.sprite = sittingSprite;
            }
            spriteRenderer.color = new Color(0.70f, 0.50f, 0.50f, 1f);
        }
    }

    private void Start()
    {
        var knight = FindAnyObjectByType<KnightMovement>();
        if (knight != null)
            player = knight.transform;
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDeath -= HandleDeath;
        }
    }

    private void Update()
    {
        if (currentState == BossState.Dead) return;

        if (player == null)
        {
            var knight = FindAnyObjectByType<KnightMovement>();
            if (knight != null) player = knight.transform;
            return;
        }

        // Poise regeneration over time if not taking damage
        if (poiseRegenTimer > 0f)
        {
            poiseRegenTimer -= Time.deltaTime;
            if (poiseRegenTimer <= 0f)
            {
                currentPoise = maxPoise;
            }
        }

        float deltaX = player.position.x - transform.position.x;
        float distToPlayer = Mathf.Abs(deltaX);

        // 1. Perched on Skull in the Background until player reaches arena (X >= 59.0)
        if (currentState == BossState.Dormant)
        {
            // Subtle brooding crimson silhouette pulse on the skull
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(
                    new Color(0.65f, 0.45f, 0.45f, 1f),
                    new Color(0.85f, 0.55f, 0.55f, 1f),
                    Mathf.PingPong(Time.time * 1.5f, 1f)
                );
            }

            if (player.position.x >= 59.0f)
            {
                TriggerIntro();
            }
            return;
        }

        // Face player during chase / idle
        if (currentState == BossState.Idle || currentState == BossState.Chase)
        {
            int targetFacing = deltaX >= 0 ? 1 : -1;
            if (targetFacing != facingDirection)
            {
                UpdateFacing(targetFacing);
            }
        }

        // State Machine
        switch (currentState)
        {
            case BossState.Idle:
                if (animator != null) animator.SetFloat("Speed", 0f);
                if (Time.time >= nextActionTime)
                {
                    currentState = BossState.Chase;
                }
                break;

            case BossState.Chase:
                float currentSpeed = isPhase2 ? chaseSpeedPhase2 : chaseSpeed;
                if (animator != null) animator.SetFloat("Speed", currentSpeed);
                rb.linearVelocity = new Vector2(facingDirection * currentSpeed, rb.linearVelocity.y);

                if (Time.time >= nextActionTime)
                {
                    ChooseNextAttack(distToPlayer);
                }
                break;
        }
    }

    private void TriggerIntro()
    {
        if (hasTriggeredIntro) return;
        hasTriggeredIntro = true;
        currentState = BossState.RoarIntro;
        rb.linearVelocity = Vector2.zero;

        StartCoroutine(LeapFromSkullIntroRoutine());
    }

    private IEnumerator LeapFromSkullIntroRoutine()
    {
        // Step 1: Malakor awakens on the Skull Throne
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            if (riseSprite != null)
            {
                spriteRenderer.sprite = riseSprite;
            }
        }

        CameraShake.Shake(0.25f, 0.30f);
        AudioManager.Play(SoundID.SwordCharge, 1.0f);

        // Pause as he rises from the throne holding his giant axe
        yield return new WaitForSeconds(0.60f);

        // Step 2: Leap from Skull down into Arena Floor
        Vector3 startPos = transform.position;
        Vector3 endPos = arenaLandingPosition;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 2; // Jump into the foreground
            if (leapApexSprite != null)
            {
                spriteRenderer.sprite = leapApexSprite;
            }
        }

        AudioManager.Play(SoundID.BossAxeSlash, 1.15f);

        float leapDuration = 0.72f;
        float elapsed = 0f;

        while (elapsed < leapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / leapDuration);

            // Parabolic leap trajectory
            float curX = Mathf.Lerp(startPos.x, endPos.x, t);
            float curY = Mathf.Lerp(startPos.y, endPos.y, t) + Mathf.Sin(t * Mathf.PI) * 2.2f;

            transform.position = new Vector3(curX, curY, 0f);
            yield return null;
        }

        transform.position = endPos;

        // Step 3: Ground Impact on Arena Floor
        if (rb != null)
        {
            rb.gravityScale = 3f;
            rb.linearVelocity = Vector2.zero;
        }

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
        }

        CameraShake.Shake(0.70f, 0.50f);
        AudioManager.Play(SoundID.BossLightningSlam, 1.30f);

        // Release twin shockwaves across the arena floor
        SpawnShockwaveProjectileDir(1);
        SpawnShockwaveProjectileDir(-1);

        yield return new WaitForSeconds(0.25f);

        // Step 4: Awakening Roar
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Roar");
        }

        AudioManager.Play(SoundID.BossRoar, 1.25f);
        OnBossAwakened?.Invoke(this);

        yield return new WaitForSeconds(1.4f);

        // Step 5: Engage the player!
        currentState = BossState.Chase;
        nextActionTime = Time.time + 0.3f;
    }

    private void ChooseNextAttack(float dist)
    {
        rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetFloat("Speed", 0f);

        attackCycle++;

        // Melee Range (dist <= 2.8)
        if (dist <= attackRangeMelee)
        {
            if (isPhase2)
            {
                float roll = UnityEngine.Random.value;
                if (roll < 0.50f)
                    StartAttack(DoubleSlashRoutine());
                else if (roll < 0.80f)
                    StartAttack(SlamAttackRoutine());
                else
                    StartAttack(BackstepRoutine());
            }
            else
            {
                if (attackCycle % 3 == 0)
                    StartAttack(SlamAttackRoutine());
                else
                    StartAttack(SlashAttackRoutine());
            }
        }
        // Mid Range (2.8 < dist <= 6.5)
        else if (dist <= attackRangeSlam)
        {
            if (isPhase2)
            {
                float roll = UnityEngine.Random.value;
                if (roll < 0.45f)
                    StartAttack(LeapSlamRoutine());
                else if (roll < 0.80f)
                    StartAttack(ShockwaveAttackRoutine());
                else
                    StartAttack(DoubleSlashRoutine());
            }
            else
            {
                float roll = UnityEngine.Random.value;
                if (roll < 0.40f)
                    StartAttack(LeapSlamRoutine());
                else if (roll < 0.70f)
                    StartAttack(SlamAttackRoutine());
                else
                    StartAttack(ShockwaveAttackRoutine());
            }
        }
        // Long Range (dist > 6.5) - Close the gap aggressively!
        else
        {
            if (UnityEngine.Random.value < 0.65f)
                StartAttack(LeapSlamRoutine());
            else
                StartAttack(ShockwaveAttackRoutine());
        }
    }

    private void StartAttack(IEnumerator routine)
    {
        StopAttackCoroutines();
        activeAttackRoutine = StartCoroutine(routine);
    }

    private void StopAttackCoroutines()
    {
        if (activeAttackRoutine != null)
        {
            StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = null;
        }

        if (meleeHitbox != null) meleeHitbox.DisableHitbox();
        isHyperArmored = false;
    }

    // --- ATTACK ROUTINES ---

    // 1. Standard Heavy Slash (Phase 1 Primary)
    private IEnumerator SlashAttackRoutine()
    {
        currentState = BossState.AttackSlash;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.BossAxeSlash, 1.0f);

        // Windup
        yield return new WaitForSeconds(0.32f);

        // Active slash window
        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(slashDamage);
        }

        yield return new WaitForSeconds(0.28f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        // Recovery
        yield return new WaitForSeconds(0.30f);
        currentState = BossState.Idle;
        nextActionTime = Time.time + (isPhase2 ? attackCooldownPhase2 : attackCooldown);
    }

    // 2. Relentless 2-Hit Slash Combo (Phase 2 Devastating Cleave)
    private IEnumerator DoubleSlashRoutine()
    {
        currentState = BossState.AttackSlash;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        // --- STRIKE 1: Downward Cleave ---
        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.BossAxeSlash, 1.0f);

        yield return new WaitForSeconds(0.26f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(slashDamage);
        }

        yield return new WaitForSeconds(0.20f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        // Forward dash step
        rb.linearVelocity = new Vector2(facingDirection * (isPhase2 ? 7.0f : 4.5f), rb.linearVelocity.y);
        yield return new WaitForSeconds(0.14f);
        rb.linearVelocity = Vector2.zero;

        // Re-align facing toward player
        if (player != null)
        {
            int targetFacing = (player.position.x - transform.position.x) >= 0 ? 1 : -1;
            UpdateFacing(targetFacing);
        }

        // --- STRIKE 2: Reverse Uppercut Cleave ---
        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.BossAxeSlash, 1.15f);

        yield return new WaitForSeconds(0.24f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(slashDamage + 1); // Extra damage on second strike
        }

        yield return new WaitForSeconds(0.22f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        // Short recovery
        yield return new WaitForSeconds(isPhase2 ? 0.20f : 0.35f);
        currentState = BossState.Idle;
        nextActionTime = Time.time + (isPhase2 ? attackCooldownPhase2 : attackCooldown);
    }

    // 3. Stationary Thunder Slam
    private IEnumerator SlamAttackRoutine()
    {
        currentState = BossState.AttackSlam;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetTrigger("Slam");

        // Windup & leap
        yield return new WaitForSeconds(0.38f);

        // Impact ground
        AudioManager.Play(SoundID.BossLightningSlam, 1.1f);
        CameraShake.Shake(0.40f, 0.40f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(isPhase2 ? slamDamage + 1 : slamDamage);
        }

        // In Phase 2: Dual ground ripples
        if (isPhase2)
        {
            SpawnShockwaveProjectileDir(1);
            SpawnShockwaveProjectileDir(-1);
        }

        yield return new WaitForSeconds(0.25f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        // Stagger recovery
        yield return new WaitForSeconds(isPhase2 ? 0.25f : 0.40f);
        currentState = BossState.Idle;
        nextActionTime = Time.time + (isPhase2 ? attackCooldownPhase2 : attackCooldown);
    }

    // 4. Abyssal Leap Slam (Dynamic Gap-Closer)
    private IEnumerator LeapSlamRoutine()
    {
        currentState = BossState.AttackSlam;
        isHyperArmored = true;

        if (animator != null) animator.SetTrigger("Slam");

        // Crouch preparation
        yield return new WaitForSeconds(0.22f);

        // Calculate launch trajectory toward player
        float targetX = player != null ? player.position.x : transform.position.x + facingDirection * 5f;
        targetX = Mathf.Clamp(targetX, 60.5f, 83.5f); // Stay within arena boundaries
        float diffX = targetX - transform.position.x;
        int targetFacing = diffX >= 0 ? 1 : -1;
        UpdateFacing(targetFacing);

        // Launch into air
        float jumpVx = Mathf.Clamp(diffX * 1.6f, -11f, 11f);
        rb.linearVelocity = new Vector2(jumpVx, 10.5f);

        // Mid-air ascent
        yield return new WaitForSeconds(0.40f);

        // Force ground smash descent
        rb.linearVelocity = new Vector2(jumpVx * 0.4f, -14f);

        // Wait until boss reaches ground
        float timeout = 1.0f;
        while (timeout > 0f && rb.linearVelocity.y < -0.1f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        // Ground Impact Cataclysm
        AudioManager.Play(SoundID.BossLightningSlam, 1.25f);
        CameraShake.Shake(0.55f, 0.45f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(isPhase2 ? slamDamage + 1 : slamDamage);
        }

        // Ground shockwave release
        if (isPhase2)
        {
            SpawnShockwaveProjectileDir(1);
            SpawnShockwaveProjectileDir(-1);
        }
        else
        {
            SpawnShockwaveProjectileDir(facingDirection);
        }

        yield return new WaitForSeconds(0.26f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        yield return new WaitForSeconds(isPhase2 ? 0.25f : 0.45f);
        currentState = BossState.Idle;
        nextActionTime = Time.time + (isPhase2 ? attackCooldownPhase2 : attackCooldown);
    }

    // 5. Crescent Shockwave (Single in Phase 1, Rapid Dual Barrage in Phase 2)
    private IEnumerator ShockwaveAttackRoutine()
    {
        currentState = BossState.AttackShockwave;
        rb.linearVelocity = Vector2.zero;

        int waves = isPhase2 ? 2 : 1;
        for (int w = 0; w < waves; w++)
        {
            if (animator != null) animator.SetTrigger("Shockwave");

            yield return new WaitForSeconds(isPhase2 ? 0.28f : 0.40f);

            AudioManager.Play(SoundID.BossShockwaveLaunch, 1.0f);
            SpawnShockwaveProjectile();

            yield return new WaitForSeconds(isPhase2 ? 0.24f : 0.35f);
        }

        currentState = BossState.Idle;
        nextActionTime = Time.time + (isPhase2 ? attackCooldownPhase2 : attackCooldown);
    }

    // 6. Tactical Shadow Backstep Counter
    private IEnumerator BackstepRoutine()
    {
        rb.linearVelocity = Vector2.zero;

        // Shadow dash backwards
        rb.linearVelocity = new Vector2(-facingDirection * 9f, 2.5f);
        CameraShake.Shake(0.18f, 0.20f);

        yield return new WaitForSeconds(0.24f);
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(0.10f);

        // Immediate retaliatory strike!
        StartAttack(SlashAttackRoutine());
    }

    private void SpawnShockwaveProjectile()
    {
        SpawnShockwaveProjectileDir(facingDirection);
    }

    private void SpawnShockwaveProjectileDir(int dir)
    {
        if (shockwaveProjectilePrefab == null) return;

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position + new Vector3(dir * 1.5f, 0.8f, 0f);
        var proj = Instantiate(shockwaveProjectilePrefab, spawnPos, Quaternion.identity);
        var pComp = proj.GetComponent<BossProjectile>();
        if (pComp != null)
        {
            pComp.Launch(dir, shockwaveDamage);
        }
    }

    // --- PHASE TRANSITION & POISE HANDLING ---

    private void HandleDamaged(int dmg)
    {
        if (currentState == BossState.Dead || isPhaseTransitioning) return;

        // Visceral blood splatter on boss
        var bleed = GetComponent<BleedWoundEffect>();
        if (bleed == null) bleed = gameObject.AddComponent<BleedWoundEffect>();
        Vector2 bloodDir = player != null ? ((Vector2)(transform.position - player.position)).normalized + Vector2.up * 0.4f : Vector2.up;
        bleed.TriggerBleed(transform.position + new Vector3(0f, 2.0f, 0f), bloodDir, dmg >= 2);

        AudioManager.Play(SoundID.BossHurt, 0.95f);

        // Poise damage calculation
        currentPoise -= dmg;
        poiseRegenTimer = 3.5f;

        // Check Phase 2 trigger (at <= 50% HP)
        if (!isPhase2 && health != null && health.CurrentHealth <= (health.MaxHealth / 2))
        {
            StartCoroutine(TriggerPhase2Transition());
            return;
        }

        // Only stagger if poise is broken AND boss is not in hyper-armor
        if (currentPoise <= 0 && !isHyperArmored)
        {
            currentPoise = maxPoise;
            StartCoroutine(StaggerRoutine());
        }
    }

    private IEnumerator TriggerPhase2Transition()
    {
        isPhase2 = true;
        isPhaseTransitioning = true;
        StopAttackCoroutines();

        currentState = BossState.RoarIntro;
        rb.linearVelocity = Vector2.zero;

        // Enraged visual tint & aura
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1f, 0.45f, 0.45f, 1f);

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Roar");
        }

        AudioManager.Play(SoundID.BossRoar, 1.25f);
        CameraShake.Shake(0.65f, 0.55f);

        OnBossPhaseChanged?.Invoke(this, 2);
        Debug.Log("<color=#FF2222>PHASE 2 UNLEASHED: MALAKOR ENTERS BLOODRAGE!</color>");

        yield return new WaitForSeconds(1.8f);

        isPhaseTransitioning = false;
        currentState = BossState.Chase;
        nextActionTime = Time.time + 0.2f;
    }

    private IEnumerator StaggerRoutine()
    {
        StopAttackCoroutines();
        currentState = BossState.Hurt;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Hit");
        }

        CameraShake.Shake(0.35f, 0.30f);
        Debug.Log("BOSS POISE BROKEN! Malakor is staggered!");

        // Stagger opening window
        yield return new WaitForSeconds(1.1f);

        currentState = BossState.Chase;
        nextActionTime = Time.time + 0.2f;
    }

    private void HandleDeath()
    {
        if (currentState == BossState.Dead) return;
        currentState = BossState.Dead;
        StopAttackCoroutines();
        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        if (bodyCollider != null) bodyCollider.enabled = false;
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        if (animator != null)
        {
            animator.SetBool("IsDead", true);
            animator.SetTrigger("Hit");
        }

        AudioManager.Play(SoundID.BossDeath, 1.0f);
        CameraShake.Shake(0.6f, 0.5f);
        Debug.Log("BOSS DEFEATED! The Dreadlord has fallen!");

        OnBossDefeated?.Invoke();
    }

    public void UpdateFacing(int dir)
    {
        facingDirection = dir >= 0 ? 1 : -1;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (facingDirection == -1);
        }
        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
        }
    }
}
