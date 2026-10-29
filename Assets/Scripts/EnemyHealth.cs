using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth = 3;
    [SerializeField] private bool autoDestroy = true;
    [SerializeField] private float destroyDelay = 2.0f;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    public event Action<int> OnDamaged;
    public event Action OnDeath;
    public delegate bool DamageFilterDelegate(int damage);
    public event DamageFilterDelegate OnDamageFilter;

    private bool isDying;

    private void Awake()
    {
        currentHealth = maxHealth;
        isDying = false;
    }

    private void OnEnable()
    {
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
            isDying = false;
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDying = false;
    }

    public void SetMaxHealth(int newMax)
    {
        maxHealth = newMax;
        currentHealth = newMax;
    }

    public void SetAutoDestroy(bool auto, float delay = 2.0f)
    {
        autoDestroy = auto;
        destroyDelay = delay;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDying)
            return;

        // Check if enemy has a damage filter (e.g. Boss defensive guard / parry stance)
        if (OnDamageFilter != null && !OnDamageFilter(damage))
        {
            // Damage blocked / deflected by defensive stance
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // Spawn visceral blood splatter
        Vector2 bloodDir = Vector2.up * 0.4f + (UnityEngine.Random.value > 0.5f ? Vector2.right : Vector2.left) * 0.6f;
        BloodSplatterSystem.SpawnBlood(transform.position + new Vector3(0f, 1.3f, 0f), bloodDir, damage >= 2);

        // Trigger rich bleeding animation (arterial slash arc, trauma flash, and ground-dripping blood)
        var bleed = GetComponent<BleedWoundEffect>();
        if (bleed == null) bleed = gameObject.AddComponent<BleedWoundEffect>();
        bleed.TriggerBleed(transform.position + new Vector3(0f, 1.3f, 0f), bloodDir, damage >= 2);

        Debug.Log(
            gameObject.name + " took " + damage +
            " damage! Health: " + currentHealth
        );

        OnDamaged?.Invoke(damage);

        if (currentHealth <= 0 && !isDying)
        {
            isDying = true;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " was defeated!");
        OnDeath?.Invoke();

        if (autoDestroy)
        {
            Destroy(gameObject, destroyDelay > 0f ? destroyDelay : 0f);
        }
    }
}
