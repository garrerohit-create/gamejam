using System;
using System.Collections;
using UnityEngine;

public enum BossState
{
    Dormant,
    RoarIntro,
    Idle,
    CombatStalk,
    Chase,
    AttackCleave,
    AttackSlam,
    AttackShockwave,
    AttackCombo,
    AttackThrust,
    AttackWhirlwind,
    AttackFissure,
    DefensiveGuard,
    Hurt,
    Dead
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(EnemyHealth))]
public class BossAI : MonoBehaviour
{
    [Header("Boss Identity")]
    [SerializeField] private string bossName = "THE INFERNAL EXECUTIONER — WARDEN OF THE DAMNED";
    [SerializeField] private int bossMaxHealth = 60;

    [Header("Phase Thresholds (HP)")]
    [SerializeField] private int phase2Threshold = 39; // 65% of 60 HP
    [SerializeField] private int phase3Threshold = 18; // 30% of 60 HP

    [Header("Locomotion Speeds")]
    [SerializeField] private float chaseSpeedPhase1 = 3.6f;
    [SerializeField] private float chaseSpeedPhase2 = 4.8f;
    [SerializeField] private float chaseSpeedPhase3 = 5.8f;
    [SerializeField] private float stalkSpeedPhase1 = 2.0f;
    [SerializeField] private float stalkSpeedPhase2 = 2.8f;
    [SerializeField] private float stalkSpeedPhase3 = 3.6f;
    [SerializeField] private float attackRangeMelee = 3.8f;
    [SerializeField] private float attackRangeSlam = 5.8f;

    [Header("Combat Stats")]
    [SerializeField] private int cleaveDamage = 3;
    [SerializeField] private int slamDamage = 4;
    [SerializeField] private int cataclysmDamage = 6;
    [SerializeField] private int shockwaveDamage = 3;
    [SerializeField] private int thrustDamage = 4;
    [SerializeField] private int whirlwindDamage = 4;
    [SerializeField] private int fissureDamage = 3;
    [SerializeField] private int counterDamage = 3;

    [Header("Attack Cooldowns")]
    [SerializeField] private float attackCooldownPhase1 = 1.25f;
    [SerializeField] private float attackCooldownPhase2 = 0.70f;
    [SerializeField] private float attackCooldownPhase3 = 0.42f;

    [Header("Poise & Hyper-Armor")]
    [SerializeField] private int maxPoise = 10;
    private int currentPoise;
    private float poiseRegenTimer = 0f;
    private bool isHyperArmored = false;

    [Header("Phase State")]
    private int currentPhase = 1;
    private bool isPhaseTransitioning = false;
    private float stalkTimer = 0f;

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
    private int facingDirection = -1; // -1 = left, 1 = right
    private float nextActionTime;
    private bool hasTriggeredIntro = false;
    private int attackCycle = 0;
    private Coroutine activeAttackRoutine = null;

    // Static Events
    public static event Action OnBossDefeated;
    public static event Action<BossAI> OnBossAwakened;
    public static event Action<BossAI, int> OnBossPhaseChanged; // (boss, phase)

    public string BossName => bossName;
    public int CurrentPhase => currentPhase;
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
            health.OnDamageFilter += HandleDamageFilter;
        }

        currentPoise = maxPoise;
        currentPhase = 1;
        UpdateFacing(-1);

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
            spriteRenderer.sortingOrder = -4; // Render in background layer atop skull
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
            health.OnDamageFilter -= HandleDamageFilter;
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

        // Poise regeneration
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

        // 1. Perched on Skull until player reaches arena (X >= 59.0)
        if (currentState == BossState.Dormant)
        {
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

        // Face player during Stalk / Chase / Idle
        if (currentState == BossState.Idle || currentState == BossState.Chase || currentState == BossState.CombatStalk)
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
                    EnterCombatStalk();
                }
                break;

            case BossState.CombatStalk:
                UpdateCombatStalk(distToPlayer, deltaX);
                break;

            case BossState.Chase:
                float currentSpeed = currentPhase == 3 ? chaseSpeedPhase3 : (currentPhase == 2 ? chaseSpeedPhase2 : chaseSpeedPhase1);
                if (animator != null) animator.SetFloat("Speed", currentSpeed);
                rb.linearVelocity = new Vector2(facingDirection * currentSpeed, rb.linearVelocity.y);

                if (distToPlayer <= attackRangeMelee || Time.time >= nextActionTime)
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
        // 1. Malakor awakens atop the Skull Throne
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

        yield return new WaitForSeconds(0.60f);

        // 2. Leap from Skull down into Arena Floor
        Vector3 startPos = transform.position;
        Vector3 endPos = arenaLandingPosition;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 2; // Move to foreground layer
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

            float curX = Mathf.Lerp(startPos.x, endPos.x, t);
            float curY = Mathf.Lerp(startPos.y, endPos.y, t) + Mathf.Sin(t * Mathf.PI) * 2.2f;

            transform.position = new Vector3(curX, curY, 0f);
            yield return null;
        }

        transform.position = endPos;

        // 3. Ground Impact on Arena Floor
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

        SpawnShockwaveProjectileDir(1);
        SpawnShockwaveProjectileDir(-1);

        yield return new WaitForSeconds(0.25f);

        // 4. Awakening Roar
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Roar");
        }

        AudioManager.Play(SoundID.BossRoar, 1.25f);
        OnBossAwakened?.Invoke(this);

        yield return new WaitForSeconds(1.4f);

        // 5. Engage the player with an imposing Combat Stalk
        EnterCombatStalk();
    }

    private void EnterCombatStalk()
    {
        currentState = BossState.CombatStalk;
        float baseCooldown = currentPhase == 3 ? attackCooldownPhase3 : (currentPhase == 2 ? attackCooldownPhase2 : attackCooldownPhase1);
        stalkTimer = UnityEngine.Random.Range(baseCooldown * 0.95f, baseCooldown * 1.55f);
    }

    private void UpdateCombatStalk(float dist, float deltaX)
    {
        stalkTimer -= Time.deltaTime;

        float stalkSpeed = currentPhase == 3 ? stalkSpeedPhase3 : (currentPhase == 2 ? stalkSpeedPhase2 : stalkSpeedPhase1);

        // Check if player jumped or rolled behind the boss
        bool playerBehind = (deltaX > 0 && facingDirection == -1) || (deltaX < 0 && facingDirection == 1);
        if (playerBehind && dist < 3.5f)
        {
            // Player slipped behind boss! 65% chance to punish with Whirlwind or Backstep!
            if (UnityEngine.Random.value < 0.65f)
            {
                if (currentPhase >= 2 && UnityEngine.Random.value < 0.60f)
                    StartAttack(WhirlwindExecutionRoutine());
                else
                    StartAttack(ShadowBackstepRoutine());
                return;
            }
            else
            {
                UpdateFacing(deltaX >= 0 ? 1 : -1);
            }
        }

        // Tactical spacing logic
        if (dist > 6.0f)
        {
            // Player is distant: stride deliberately towards player
            rb.linearVelocity = new Vector2(facingDirection * stalkSpeed, rb.linearVelocity.y);
            if (animator != null) animator.SetFloat("Speed", stalkSpeed);
        }
        else if (dist > 3.2f)
        {
            // Optimal duel range: stalk forward slowly with high tension
            rb.linearVelocity = new Vector2(facingDirection * (stalkSpeed * 0.70f), rb.linearVelocity.y);
            if (animator != null) animator.SetFloat("Speed", stalkSpeed * 0.70f);
        }
        else
        {
            // Close quarters: slow menacing pacing
            rb.linearVelocity = new Vector2(facingDirection * (stalkSpeed * 0.40f), rb.linearVelocity.y);
            if (animator != null) animator.SetFloat("Speed", stalkSpeed * 0.40f);

            // Reactive defense: In Phase 1 or 2, chance to test player with Defensive Guard
            if (stalkTimer > 0.4f && UnityEngine.Random.value < (currentPhase == 1 ? 0.05f : 0.08f))
            {
                StartAttack(DefensiveGuardRoutine());
                return;
            }
        }

        // When stalk timer concludes, choose next calibrated attack
        if (stalkTimer <= 0f)
        {
            ChooseNextAttack(dist);
        }
    }

    private bool HandleDamageFilter(int dmg)
    {
        if (currentState == BossState.DefensiveGuard)
        {
            // Player struck the boss during his defensive guard!
            TriggerGuardCounter();
            return false; // Absorb / Negate damage!
        }
        return true; // Take damage normally
    }

    private void TriggerGuardCounter()
    {
        StopAttackCoroutines();
        StartAttack(GuardCounterRiposteRoutine());
    }

    private void ChooseNextAttack(float dist)
    {
        rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetFloat("Speed", 0f);

        attackCycle++;

        if (currentPhase == 1)
        {
            ChoosePhase1Attack(dist);
        }
        else if (currentPhase == 2)
        {
            ChoosePhase2Attack(dist);
        }
        else
        {
            ChoosePhase3Attack(dist);
        }
    }

    private void ChoosePhase1Attack(float dist)
    {
        // Phase 1 — The Executioner (Controlled, Telegraphed, Readable)
        if (dist <= attackRangeMelee)
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.38f)
                StartAttack(ExecutionerCleaveRoutine());
            else if (roll < 0.68f)
                StartAttack(RuinousOverheadRoutine());
            else if (roll < 0.85f)
                StartAttack(DefensiveGuardRoutine());
            else
                StartAttack(ShadowBackstepRoutine());
        }
        else if (dist <= attackRangeSlam)
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.40f)
                StartAttack(AbyssalLungeThrustRoutine());
            else if (roll < 0.70f)
                StartAttack(CrimsonAdvanceRoutine());
            else
                StartAttack(HellfireShockwaveRoutine(1));
        }
        else
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.50f)
                StartAttack(AbyssalLungeThrustRoutine());
            else if (roll < 0.80f)
                StartAttack(CrimsonAdvanceRoutine());
            else
                StartAttack(HellfireShockwaveRoutine(1));
        }
    }

    private void ChoosePhase2Attack(float dist)
    {
        // Phase 2 — Blood Awakening (Faster combos, delayed bait slams, leaping gap closers, whirlwind)
        if (dist <= attackRangeMelee)
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.30f)
                StartAttack(BloodCleaveComboRoutine());
            else if (roll < 0.55f)
                StartAttack(WhirlwindExecutionRoutine());
            else if (roll < 0.75f)
                StartAttack(DelayedOverheadSlamRoutine());
            else if (roll < 0.88f)
                StartAttack(DefensiveGuardRoutine());
            else
                StartAttack(ShadowBackstepRoutine());
        }
        else if (dist <= attackRangeSlam)
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.35f)
                StartAttack(AbyssalLungeThrustRoutine());
            else if (roll < 0.65f)
                StartAttack(AbyssalLeapSlamRoutine());
            else if (roll < 0.82f)
                StartAttack(HellfireShockwaveRoutine(2));
            else
                StartAttack(DelayedOverheadSlamRoutine());
        }
        else
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.40f)
                StartAttack(AbyssalLeapSlamRoutine());
            else if (roll < 0.70f)
                StartAttack(AbyssalLungeThrustRoutine());
            else
                StartAttack(EarthFissureRoutine());
        }
    }

    private void ChoosePhase3Attack(float dist)
    {
        // Phase 3 — Final Judgment (Cataclysmic leaps, 3-hit fury, rapid shockwave barrage, fissures)
        if (dist <= attackRangeMelee)
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.32f)
                StartAttack(ChainedFuryRoutine());
            else if (roll < 0.58f)
                StartAttack(WhirlwindExecutionRoutine());
            else if (roll < 0.78f)
                StartAttack(CataclysmicExecutionRoutine());
            else if (roll < 0.90f)
                StartAttack(DefensiveGuardRoutine());
            else
                StartAttack(ShadowBackstepRoutine());
        }
        else if (dist <= attackRangeSlam)
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.35f)
                StartAttack(CataclysmicExecutionRoutine());
            else if (roll < 0.65f)
                StartAttack(AbyssalLungeThrustRoutine());
            else if (roll < 0.82f)
                StartAttack(JudgmentShockwaveBarrageRoutine());
            else
                StartAttack(ChainedFuryRoutine());
        }
        else
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.40f)
                StartAttack(CataclysmicExecutionRoutine());
            else if (roll < 0.70f)
                StartAttack(EarthFissureRoutine());
            else
                StartAttack(AbyssalLungeThrustRoutine());
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

    // --- PHASE 1 MOVES ---

    // 1. Executioner's Cleave: Heavy horizontal strike, readable telegraph, blockable and parryable
    private IEnumerator ExecutionerCleaveRoutine()
    {
        currentState = BossState.AttackCleave;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.BossAxeSlash, 1.0f);

        // Windup window with clear visual telegraph
        yield return new WaitForSeconds(0.36f);

        // Active strike window (PARRYABLE)
        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(cleaveDamage, parryable: true);
        }

        yield return new WaitForSeconds(0.25f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        // Punishable recovery window
        yield return new WaitForSeconds(0.35f);
        EnterCombatStalk();
    }

    // 2. Ruinous Overhead: Slow vertical slam punishing close players, unparryable
    private IEnumerator RuinousOverheadRoutine()
    {
        currentState = BossState.AttackSlam;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetTrigger("Slam");

        // Slow, heavy windup
        yield return new WaitForSeconds(0.50f);

        // Impact ground
        AudioManager.Play(SoundID.BossLightningSlam, 1.10f);
        CameraShake.Shake(0.40f, 0.35f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(slamDamage, parryable: false); // Unparryable heavy slam
        }

        yield return new WaitForSeconds(0.25f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        // Recovery opening
        yield return new WaitForSeconds(0.45f);
        EnterCombatStalk();
    }

    // 3. Crimson Advance: Threatening stalking approach into an Executioner Cleave
    private IEnumerator CrimsonAdvanceRoutine()
    {
        currentState = BossState.Chase;
        float stalkDuration = 0.55f;
        float elapsed = 0f;

        while (elapsed < stalkDuration)
        {
            elapsed += Time.deltaTime;
            if (player != null)
            {
                int targetFacing = (player.position.x - transform.position.x) >= 0 ? 1 : -1;
                UpdateFacing(targetFacing);
                if (Mathf.Abs(player.position.x - transform.position.x) <= attackRangeMelee)
                    break;
            }

            if (animator != null) animator.SetFloat("Speed", chaseSpeedPhase1 * 1.2f);
            rb.linearVelocity = new Vector2(facingDirection * chaseSpeedPhase1 * 1.2f, rb.linearVelocity.y);
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        yield return StartCoroutine(ExecutionerCleaveRoutine());
    }

    // 4. Abyssal Lunge Thrust: High-velocity linear pierce with huge recovery opening
    private IEnumerator AbyssalLungeThrustRoutine()
    {
        currentState = BossState.AttackThrust;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.SwordCharge, 1.20f);

        Color origColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1.0f, 0.45f, 0.45f, 1.0f);

        // Clear windup telegraph (0.42s)
        yield return new WaitForSeconds(0.42f);

        if (spriteRenderer != null) spriteRenderer.color = origColor;

        // Rocket Thrust Forward
        float thrustSpeed = 16.5f;
        rb.linearVelocity = new Vector2(facingDirection * thrustSpeed, rb.linearVelocity.y);

        AudioManager.Play(SoundID.BossAxeSlash, 1.25f);
        AudioManager.Play(SoundID.SwordClang, 1.10f);
        CameraShake.Shake(0.38f, 0.25f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(thrustDamage, parryable: true, new Vector2(3.5f, 2.2f), new Vector2(facingDirection * 1.0f, 0.2f));
        }

        yield return new WaitForSeconds(0.18f);

        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        rb.linearVelocity = Vector2.zero;
        AudioManager.Play(SoundID.HeavyImpact, 1.15f);
        CameraShake.Shake(0.25f, 0.20f);

        isHyperArmored = false;

        // Punishable recovery window
        yield return new WaitForSeconds(0.55f);
        EnterCombatStalk();
    }

    // 5. Demonic Defensive Guard: Raises colossal blade to deflect and counterattack
    private IEnumerator DefensiveGuardRoutine()
    {
        currentState = BossState.DefensiveGuard;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetFloat("Speed", 0f);
        AudioManager.Play(SoundID.SwordCharge, 0.85f);

        Color origColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.85f, 0.95f, 1.0f, 1.0f);

        float guardDuration = 0.75f;
        float elapsed = 0f;

        while (elapsed < guardDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (spriteRenderer != null) spriteRenderer.color = origColor;
        isHyperArmored = false;

        yield return new WaitForSeconds(0.35f);
        EnterCombatStalk();
    }

    // 6. Guard Counter Riposte: Deflection spark & brutal gauntlet strike counter
    private IEnumerator GuardCounterRiposteRoutine()
    {
        currentState = BossState.AttackCleave;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        AudioManager.Play(SoundID.ParryClash, 1.35f);
        AudioManager.Play(SoundID.ArmorClang, 1.25f);
        CameraShake.Shake(0.40f, 0.25f);

        Color origColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1.0f, 0.95f, 0.4f, 1.0f);

        yield return new WaitForSeconds(0.12f);

        if (spriteRenderer != null) spriteRenderer.color = origColor;

        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.GauntletStrike, 1.35f);
        AudioManager.Play(SoundID.BossAxeSlash, 1.15f);

        rb.linearVelocity = new Vector2(facingDirection * 8.0f, rb.linearVelocity.y);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(counterDamage, parryable: false);
        }

        yield return new WaitForSeconds(0.20f);

        if (meleeHitbox != null) meleeHitbox.DisableHitbox();
        rb.linearVelocity = Vector2.zero;

        isHyperArmored = false;

        yield return new WaitForSeconds(0.40f);
        EnterCombatStalk();
    }

    // 7. Whirlwind Execution: 360° twin-spin cleave with front and rear shockwaves
    private IEnumerator WhirlwindExecutionRoutine()
    {
        currentState = BossState.AttackWhirlwind;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        AudioManager.Play(SoundID.SwordCharge, 1.15f);
        yield return new WaitForSeconds(0.36f);

        // Spin 1: Forward Slash
        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.BossAxeSlash, 1.25f);
        CameraShake.Shake(0.45f, 0.30f);

        if (meleeHitbox != null)
        {
            meleeHitbox.EnableHitbox(whirlwindDamage, parryable: false, new Vector2(5.5f, 3.2f), Vector2.zero);
        }

        SpawnShockwaveProjectileDir(facingDirection);

        yield return new WaitForSeconds(0.18f);

        // Spin 2: Reverse 180-degree cleave covering the rear!
        UpdateFacing(-facingDirection);
        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.BossAxeSlash, 1.35f);
        CameraShake.Shake(0.45f, 0.30f);

        SpawnShockwaveProjectileDir(facingDirection);

        yield return new WaitForSeconds(0.20f);

        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        yield return new WaitForSeconds(0.38f);
        EnterCombatStalk();
    }

    // 8. Earth Fissure: Channels hellfire through the earth, erupting a flame pillar under the player
    private IEnumerator EarthFissureRoutine()
    {
        currentState = BossState.AttackFissure;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetTrigger("Slam");
        AudioManager.Play(SoundID.SwordCharge, 1.25f);
        CameraShake.Shake(0.30f, 0.25f);

        float fissureX = player != null ? player.position.x : transform.position.x + facingDirection * 5f;
        fissureX = Mathf.Clamp(fissureX, 60.5f, 83.5f);
        Vector3 fissurePos = new Vector3(fissureX, -0.08f, 0f);

        // Create Warning Sigil Sprite
        var sigilObj = new GameObject("HellfireSigil");
        sigilObj.transform.position = fissurePos;
        var sigilSR = sigilObj.AddComponent<SpriteRenderer>();
        var whiteTex = Texture2D.whiteTexture;
        sigilSR.sprite = Sprite.Create(whiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        sigilSR.color = new Color(1.0f, 0.2f, 0.1f, 0.75f);
        sigilSR.sortingOrder = 3;
        sigilObj.transform.localScale = new Vector3(2.5f, 0.4f, 1f);

        // Warning delay (0.55s)
        float elapsed = 0f;
        while (elapsed < 0.55f)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 8f, 1f);
            if (sigilSR != null) sigilSR.color = Color.Lerp(new Color(1f, 0.2f, 0.1f, 0.7f), new Color(1f, 0.8f, 0.3f, 1f), pulse);
            yield return null;
        }

        // ERUPTION!
        AudioManager.Play(SoundID.BossLightningSlam, 1.35f);
        AudioManager.Play(SoundID.HeavyImpact, 1.20f);
        CameraShake.Shake(0.70f, 0.45f);

        if (sigilObj != null) Destroy(sigilObj);

        var eruptionObj = new GameObject("HellfireEruption");
        eruptionObj.transform.position = fissurePos + new Vector3(0f, 1.8f, 0f);
        var erupSR = eruptionObj.AddComponent<SpriteRenderer>();
        erupSR.sprite = Sprite.Create(whiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        erupSR.color = new Color(1.0f, 0.35f, 0.1f, 0.90f);
        erupSR.sortingOrder = 4;
        eruptionObj.transform.localScale = new Vector3(1.8f, 3.8f, 1f);

        // Damage check
        if (player != null)
        {
            var km = player.GetComponent<KnightMovement>();
            if (km != null && Mathf.Abs(player.position.x - fissureX) <= 1.4f && player.position.y <= 3.5f)
            {
                km.TakeDamage(fissureDamage, gameObject, canBeParried: false);
            }
        }

        yield return new WaitForSeconds(0.35f);

        if (eruptionObj != null) Destroy(eruptionObj);

        isHyperArmored = false;

        yield return new WaitForSeconds(0.40f);
        EnterCombatStalk();
    }

    // --- PHASE 2 MOVES ---

    // 9. Blood Cleave Combo: 2-hit rhythm combo (Downward cleave -> dash step -> reverse uppercut)
    private IEnumerator BloodCleaveComboRoutine()
    {
        currentState = BossState.AttackCombo;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        // --- HIT 1: Downward Cleave (Parryable) ---
        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.BossAxeSlash, 1.05f);

        yield return new WaitForSeconds(0.26f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(cleaveDamage, parryable: true);
        }

        yield return new WaitForSeconds(0.20f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        // Forward dash step
        rb.linearVelocity = new Vector2(facingDirection * 7.0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(0.14f);
        rb.linearVelocity = Vector2.zero;

        if (player != null)
        {
            int targetFacing = (player.position.x - transform.position.x) >= 0 ? 1 : -1;
            UpdateFacing(targetFacing);
        }

        // --- HIT 2: Reverse Uppercut (Parryable) ---
        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.BossAxeSlash, 1.15f);

        yield return new WaitForSeconds(0.24f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(cleaveDamage + 1, parryable: true);
        }

        yield return new WaitForSeconds(0.20f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        yield return new WaitForSeconds(0.30f);
        EnterCombatStalk();
    }

    // 10. Delayed Overhead Slam: Designed specifically to catch premature roll/panic dodges
    private IEnumerator DelayedOverheadSlamRoutine()
    {
        currentState = BossState.AttackSlam;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetTrigger("Slam");

        // Extended delayed windup - holds axe at apex
        yield return new WaitForSeconds(0.70f);

        // Violent slam down
        AudioManager.Play(SoundID.BossLightningSlam, 1.20f);
        CameraShake.Shake(0.50f, 0.40f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(slamDamage + 1, parryable: false);
        }

        // Twin shockwaves ripple outwards
        SpawnShockwaveProjectileDir(1);
        SpawnShockwaveProjectileDir(-1);

        yield return new WaitForSeconds(0.25f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        yield return new WaitForSeconds(0.35f);
        EnterCombatStalk();
    }

    // 11. Abyssal Leap Slam: High-velocity gap-closer
    private IEnumerator AbyssalLeapSlamRoutine()
    {
        currentState = BossState.AttackSlam;
        isHyperArmored = true;

        if (animator != null) animator.SetTrigger("Slam");

        yield return new WaitForSeconds(0.20f);

        float targetX = player != null ? player.position.x : transform.position.x + facingDirection * 5f;
        targetX = Mathf.Clamp(targetX, 60.5f, 83.5f);
        float diffX = targetX - transform.position.x;
        int targetFacing = diffX >= 0 ? 1 : -1;
        UpdateFacing(targetFacing);

        float jumpVx = Mathf.Clamp(diffX * 1.7f, -12f, 12f);
        rb.linearVelocity = new Vector2(jumpVx, 11f);

        yield return new WaitForSeconds(0.40f);

        // Force ground slam descent
        rb.linearVelocity = new Vector2(jumpVx * 0.4f, -15f);

        float timeout = 1.0f;
        while (timeout > 0f && rb.linearVelocity.y < -0.1f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        AudioManager.Play(SoundID.BossLightningSlam, 1.25f);
        CameraShake.Shake(0.55f, 0.45f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(slamDamage + 1, parryable: false);
        }

        SpawnShockwaveProjectileDir(1);
        SpawnShockwaveProjectileDir(-1);

        yield return new WaitForSeconds(0.25f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        yield return new WaitForSeconds(0.35f);
        EnterCombatStalk();
    }

    // 12. Hellfire Shockwave: Ground crescent projectile(s)
    private IEnumerator HellfireShockwaveRoutine(int count)
    {
        currentState = BossState.AttackShockwave;
        rb.linearVelocity = Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            if (animator != null) animator.SetTrigger("Shockwave");

            yield return new WaitForSeconds(0.32f);

            AudioManager.Play(SoundID.BossShockwaveLaunch, 1.0f);
            SpawnShockwaveProjectile();

            yield return new WaitForSeconds(0.24f);
        }

        EnterCombatStalk();
    }

    // 13. Tactical Shadow Backstep
    private IEnumerator ShadowBackstepRoutine()
    {
        rb.linearVelocity = Vector2.zero;

        rb.linearVelocity = new Vector2(-facingDirection * 9.5f, 2.5f);
        CameraShake.Shake(0.18f, 0.20f);

        yield return new WaitForSeconds(0.22f);
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(0.10f);

        // Immediate retaliatory strike!
        if (currentPhase == 3)
            StartAttack(BloodCleaveComboRoutine());
        else
            StartAttack(ExecutionerCleaveRoutine());
    }

    // --- PHASE 3 MOVES ---

    // 14. Cataclysmic Execution: Ultimate apex jump with earth fissure & dual shockwaves
    private IEnumerator CataclysmicExecutionRoutine()
    {
        currentState = BossState.AttackSlam;
        isHyperArmored = true;

        if (animator != null) animator.SetTrigger("Roar");
        AudioManager.Play(SoundID.BossRoar, 1.30f);
        CameraShake.Shake(0.40f, 0.35f);

        yield return new WaitForSeconds(0.30f);

        // Apex jump
        float targetX = player != null ? player.position.x : transform.position.x;
        targetX = Mathf.Clamp(targetX, 60.5f, 83.5f);
        float diffX = targetX - transform.position.x;
        int targetFacing = diffX >= 0 ? 1 : -1;
        UpdateFacing(targetFacing);

        rb.linearVelocity = new Vector2(Mathf.Clamp(diffX * 1.8f, -14f, 14f), 13f);

        // Apex pause
        yield return new WaitForSeconds(0.45f);

        // Plunge downwards
        rb.linearVelocity = new Vector2(0f, -18f);

        float timeout = 1.0f;
        while (timeout > 0f && rb.linearVelocity.y < -0.1f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        // Cataclysm impact
        AudioManager.Play(SoundID.BossLightningSlam, 1.35f);
        CameraShake.Shake(0.85f, 0.55f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(cataclysmDamage, parryable: false);
        }

        // Rapid dual ground shockwaves
        SpawnShockwaveProjectileDir(1);
        SpawnShockwaveProjectileDir(-1);

        yield return new WaitForSeconds(0.28f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        yield return new WaitForSeconds(0.35f);
        EnterCombatStalk();
    }

    // 15. Chained Fury: 3-hit rhythm combo (Cleave -> Dash Uppercut -> Overhead Slam)
    private IEnumerator ChainedFuryRoutine()
    {
        currentState = BossState.AttackCombo;
        isHyperArmored = true;
        rb.linearVelocity = Vector2.zero;

        // --- STRIKE 1: Horizontal Cleave (Parryable) ---
        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.BossAxeSlash, 1.10f);

        yield return new WaitForSeconds(0.24f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(cleaveDamage, parryable: true);
        }

        yield return new WaitForSeconds(0.18f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        // Forward Dash Step
        rb.linearVelocity = new Vector2(facingDirection * 8.0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(0.12f);
        rb.linearVelocity = Vector2.zero;

        if (player != null)
        {
            int targetFacing = (player.position.x - transform.position.x) >= 0 ? 1 : -1;
            UpdateFacing(targetFacing);
        }

        // --- STRIKE 2: Reverse Dash Cleave (Parryable) ---
        if (animator != null) animator.SetTrigger("Slash");
        AudioManager.Play(SoundID.BossAxeSlash, 1.20f);

        yield return new WaitForSeconds(0.22f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(cleaveDamage + 1, parryable: true);
        }

        yield return new WaitForSeconds(0.18f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        // --- STRIKE 3: Finishing Overhead Slam (Unparryable) ---
        if (animator != null) animator.SetTrigger("Slam");

        yield return new WaitForSeconds(0.35f);

        AudioManager.Play(SoundID.BossLightningSlam, 1.25f);
        CameraShake.Shake(0.55f, 0.40f);

        if (meleeHitbox != null)
        {
            meleeHitbox.SetFacing(facingDirection);
            meleeHitbox.EnableHitbox(slamDamage + 1, parryable: false);
        }

        SpawnShockwaveProjectileDir(facingDirection);

        yield return new WaitForSeconds(0.24f);
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();

        isHyperArmored = false;

        yield return new WaitForSeconds(0.35f);
        EnterCombatStalk();
    }

    // 16. Judgment Shockwave Barrage: Rapid triple crescent projectile barrage
    private IEnumerator JudgmentShockwaveBarrageRoutine()
    {
        currentState = BossState.AttackShockwave;
        rb.linearVelocity = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            if (animator != null) animator.SetTrigger("Shockwave");

            yield return new WaitForSeconds(0.22f);

            AudioManager.Play(SoundID.BossShockwaveLaunch, 1.10f);
            SpawnShockwaveProjectile();

            yield return new WaitForSeconds(0.18f);
        }

        EnterCombatStalk();
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

    // --- COUNTERPLAY: PARRY INTERFACE ---

    public void OnAttackParried()
    {
        if (currentState == BossState.Dead || isPhaseTransitioning) return;

        Debug.Log("<color=#FFEE55>THE INFERNAL EXECUTIONER ATTACK PARRIED! 1.0s STAGGER OPENING!</color>");
        StopAttackCoroutines();
        StartCoroutine(ParryStaggerRoutine());
    }

    private IEnumerator ParryStaggerRoutine()
    {
        currentState = BossState.Hurt;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Hit");
        }

        AudioManager.Play(SoundID.ParryClash, 1.25f);
        AudioManager.Play(SoundID.SwordClang, 1.05f);
        CameraShake.Shake(0.35f, 0.25f);

        // Flash boss yellow/white to signal deflection & stagger
        if (spriteRenderer != null)
        {
            Color orig = spriteRenderer.color;
            spriteRenderer.color = new Color(1f, 0.95f, 0.4f, 1f);
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = orig;
        }

        // Full 1.0 second vulnerable stagger window
        yield return new WaitForSeconds(0.85f);

        EnterCombatStalk();
    }

    // --- PHASE TRANSITIONS & DAMAGE HANDLING ---

    private void HandleDamaged(int dmg)
    {
        if (currentState == BossState.Dead || isPhaseTransitioning) return;

        var bleed = GetComponent<BleedWoundEffect>();
        if (bleed == null) bleed = gameObject.AddComponent<BleedWoundEffect>();
        Vector2 bloodDir = player != null ? ((Vector2)(transform.position - player.position)).normalized + Vector2.up * 0.4f : Vector2.up;
        bleed.TriggerBleed(transform.position + new Vector3(0f, 2.0f, 0f), bloodDir, dmg >= 2);

        AudioManager.Play(SoundID.BossHurt, 0.95f);
        AudioManager.Play(SoundID.ArmorClang, 1.0f);

        currentPoise -= dmg;
        poiseRegenTimer = 3.5f;

        // Check Phase Transitions
        if (health != null)
        {
            if (currentPhase == 1 && health.CurrentHealth <= phase2Threshold)
            {
                StartCoroutine(TriggerPhase2Transition());
                return;
            }
            else if (currentPhase == 2 && health.CurrentHealth <= phase3Threshold)
            {
                StartCoroutine(TriggerPhase3Transition());
                return;
            }
        }

        // Stagger on poise break if not hyper-armored
        if (currentPoise <= 0 && !isHyperArmored)
        {
            currentPoise = maxPoise;
            StartCoroutine(PoiseStaggerRoutine());
        }
    }

    private IEnumerator TriggerPhase2Transition()
    {
        currentPhase = 2;
        isPhaseTransitioning = true;
        StopAttackCoroutines();

        currentState = BossState.RoarIntro;
        rb.linearVelocity = Vector2.zero;

        // Enraged crimson visual tint
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
        Debug.Log("<color=#FF4444>PHASE 2 UNLEASHED: BLOOD AWAKENING!</color>");

        yield return new WaitForSeconds(1.8f);

        isPhaseTransitioning = false;
        EnterCombatStalk();
    }

    private IEnumerator TriggerPhase3Transition()
    {
        currentPhase = 3;
        isPhaseTransitioning = true;
        StopAttackCoroutines();

        currentState = BossState.RoarIntro;
        rb.linearVelocity = Vector2.zero;

        // Apocalyptic crimson visual tint
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1f, 0.15f, 0.15f, 1f);

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Roar");
        }

        AudioManager.Play(SoundID.BossRoar, 1.40f);
        CameraShake.Shake(0.85f, 0.70f);

        OnBossPhaseChanged?.Invoke(this, 3);
        Debug.Log("<color=#FF1111>PHASE 3 UNLEASHED: FINAL JUDGMENT — WARDEN OF THE DAMNED!</color>");

        yield return new WaitForSeconds(2.0f);

        isPhaseTransitioning = false;
        EnterCombatStalk();
    }

    private IEnumerator PoiseStaggerRoutine()
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
        Debug.Log("BOSS POISE BROKEN! The Executioner staggers!");

        yield return new WaitForSeconds(1.1f);

        EnterCombatStalk();
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
        Debug.Log("THE INFERNAL EXECUTIONER HAS FALLEN!");

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
