using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyAttackHitbox : MonoBehaviour
{
    [Header("Hitbox Configuration")]
    [SerializeField] private Collider2D hitboxCollider;
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private float forwardOffset = 1.3f;
    [SerializeField] private float verticalOffset = 1.5f;
    [SerializeField] private Vector2 hitboxSize = new Vector2(2.0f, 2.5f);

    private readonly HashSet<KnightMovement> hitTargets = new HashSet<KnightMovement>();
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(16);
    private ContactFilter2D contactFilter;

    private Transform enemyRoot;
    private int currentFacing = 1; // 1 = right, -1 = left
    private bool isActive;
    private int currentDamage;

    public bool IsActive => isActive;
    public int Damage => currentDamage > 0 ? currentDamage : baseDamage;

    private void Awake()
    {
        currentDamage = baseDamage;
        EnsureComponents();
        ConfigureCollider();
        UpdatePosition();
        DisableHitbox();
    }

    private void OnValidate()
    {
        EnsureComponents();
        ConfigureCollider();
    }

    private void EnsureComponents()
    {
        if (hitboxCollider == null)
            hitboxCollider = GetComponent<Collider2D>();

        if (enemyRoot == null)
            enemyRoot = transform.parent;

        contactFilter = ContactFilter2D.noFilter;
        contactFilter.useTriggers = true;
    }

    private void ConfigureCollider()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            if (hitboxCollider is BoxCollider2D boxCol)
            {
                boxCol.size = hitboxSize;
                boxCol.offset = Vector2.zero;
            }
        }
    }

    public void SetFacing(int direction)
    {
        currentFacing = direction >= 0 ? 1 : -1;
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        transform.localPosition = new Vector3(
            Mathf.Abs(forwardOffset) * currentFacing,
            verticalOffset,
            0f
        );
    }

    public void EnableHitbox(int damageOverride = -1)
    {
        EnsureComponents();
        currentDamage = damageOverride > 0 ? damageOverride : baseDamage;
        hitTargets.Clear();
        isActive = true;

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }

        UpdatePosition();
        Physics2D.SyncTransforms();
        PerformHitCheck();
    }

    public void DisableHitbox()
    {
        isActive = false;
        hitTargets.Clear();

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (!isActive)
            return;

        PerformHitCheck();
    }

    public void PerformHitCheck()
    {
        EnsureComponents();
        if (!isActive || hitboxCollider == null)
            return;

        overlapResults.Clear();
        int count = hitboxCollider.Overlap(contactFilter, overlapResults);

        for (int i = 0; i < count; i++)
        {
            TryDamagePlayer(overlapResults[i]);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive)
            return;

        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive)
            return;

        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (other == null)
            return;

        // Ignore enemy's own root / children
        if (enemyRoot != null && (other.transform == enemyRoot || other.transform.IsChildOf(enemyRoot)))
            return;

        KnightMovement knight = other.GetComponentInParent<KnightMovement>();
        if (knight == null || !hitTargets.Add(knight))
            return;

        GameObject attacker = enemyRoot != null ? enemyRoot.gameObject : gameObject;
        knight.TakeDamage(Damage, attacker);
    }

    private void OnDrawGizmos()
    {
        Collider2D col = hitboxCollider != null ? hitboxCollider : GetComponent<Collider2D>();
        if (col == null)
            return;

        Bounds bounds = col.bounds;
        if (isActive)
        {
            Gizmos.color = new Color(1f, 0.2f, 0f, 0.45f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        else
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}

