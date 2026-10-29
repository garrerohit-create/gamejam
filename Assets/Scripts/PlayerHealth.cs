using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(KnightMovement))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth = 10;

    [Header("Healing Flasks")]
    [SerializeField] private int maxHeals = 4;
    [SerializeField] private int currentHeals = 4;
    [SerializeField] private int healAmount = 3;
    [SerializeField] private KeyCode healKey = KeyCode.Q;
    [SerializeField] private float healCooldown = 0.8f;

    [Header("Events")]
    public UnityEvent<int> onHealthChanged;
    public UnityEvent onPlayerDied;
    public UnityEvent<int, int> onHealsChanged;

    public event Action<int, int> OnHealthChanged; // (current, max)
    public event Action<int, int> OnHealsChanged;  // (current, max)
    public event Action OnPlayerDied;

    private KnightMovement knightMovement;
    private bool isDead;
    private float nextHealTime;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentHeals => currentHeals;
    public int MaxHeals => maxHeals;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentHeals = maxHeals;
        isDead = false;
        if (knightMovement == null)
            knightMovement = GetComponent<KnightMovement>();
        Subscribe();
    }

    private void Start()
    {
        NotifyHealthChanged();
        OnHealsChanged?.Invoke(currentHeals, maxHeals);
        onHealsChanged?.Invoke(currentHeals, maxHeals);
    }

    private void Update()
    {
        if (Input.GetKeyDown(healKey))
        {
            TryUseHeal();
        }
    }

    public bool TryUseHeal()
    {
        if (isDead)
            return false;

        if (Time.time < nextHealTime)
            return false;

        if (currentHeals <= 0)
        {
            Debug.Log("No heals remaining!");
            return false;
        }

        if (currentHealth >= maxHealth)
        {
            Debug.Log("Health is already full!");
            return false;
        }

        nextHealTime = Time.time + healCooldown;
        currentHeals--;

        var healAnim = GetComponent<PlayerHealingAnimation>();
        if (healAnim != null)
        {
            healAnim.PlayHealAnimation();
        }
        else
        {
            SpawnHealVFX();
        }

        Heal(healAmount);

        Debug.Log(string.Format("Used heal! Restored {0} HP. Remaining heals: {1}/{2}", healAmount, currentHeals, maxHeals));
        onHealsChanged?.Invoke(currentHeals, maxHeals);
        OnHealsChanged?.Invoke(currentHeals, maxHeals);
        return true;
    }

    public void ResetHeals()
    {
        currentHeals = maxHeals;
        onHealsChanged?.Invoke(currentHeals, maxHeals);
        OnHealsChanged?.Invoke(currentHeals, maxHeals);
    }

    private void SpawnHealVFX()
    {
        var healObj = new GameObject("Heal_FX");
        healObj.transform.position = transform.position + new Vector3(0f, 1.2f, 0f);

        var ps = healObj.AddComponent<ParticleSystem>();
        var psr = healObj.GetComponent<ParticleSystemRenderer>();

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            var mat = new Material(shader);
            if (Application.isPlaying) psr.material = mat;
            else psr.sharedMaterial = mat;
        }

#if UNITY_EDITOR
        var sparkle = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/HealSparkle.png");
        if (sparkle != null && psr.sharedMaterial != null) psr.sharedMaterial.mainTexture = sparkle.texture;
#endif

        psr.sortingLayerName = "Default";
        psr.sortingOrder = 15;

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.gravityModifier = -0.6f; // Rises upwards
        main.startColor = new Color(0.25f, 0.95f, 0.45f, 1f); // Vibrant emerald glint

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.6f;

        ps.Play();

        if (Application.isPlaying)
        {
            Destroy(healObj, 1.2f);
        }
        else
        {
            DestroyImmediate(healObj);
        }
    }

    private void OnEnable()
    {
        if (knightMovement == null)
            knightMovement = GetComponent<KnightMovement>();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private bool isSubscribed;

    public void Subscribe()
    {
        if (knightMovement == null)
            knightMovement = GetComponent<KnightMovement>();

        if (!isSubscribed && knightMovement != null)
        {
            knightMovement.OnDamageTaken += HandleDamageTaken;
            isSubscribed = true;
        }
    }

    public void Unsubscribe()
    {
        if (isSubscribed && knightMovement != null)
        {
            knightMovement.OnDamageTaken -= HandleDamageTaken;
            isSubscribed = false;
        }
    }

    public void SetMaxHealth(int max)
    {
        maxHealth = Mathf.Max(1, max);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        NotifyHealthChanged();
    }

    public void RespawnPlayer()
    {
        isDead = false;
        currentHealth = maxHealth;
        currentHeals = maxHeals;
        NotifyHealthChanged();
        OnHealsChanged?.Invoke(currentHeals, maxHeals);
        onHealsChanged?.Invoke(currentHeals, maxHeals);
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        NotifyHealthChanged();
    }

    private void HandleDamageTaken(int damage)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log(string.Format("Player took {0} damage! Remaining Health: {1}/{2}", damage, currentHealth, maxHealth));

        NotifyHealthChanged();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void NotifyHealthChanged()
    {
        onHealthChanged?.Invoke(currentHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Debug.Log("PLAYER DIED!");
        onPlayerDied?.Invoke();
        OnPlayerDied?.Invoke();
    }
}
