using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SwordHitbox : MonoBehaviour
{
    [Header("Hitbox")]
    [SerializeField] private Collider2D hitboxCollider;
    [SerializeField] private int damage = 1;
    [SerializeField] private float forwardOffset = 1.45f;
    [SerializeField] private float verticalOffset = 0.3f;
    [SerializeField] private Vector2 hitboxSize = new Vector2(2.4f, 3.4f);

    private readonly HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(16);
    private ContactFilter2D contactFilter;

    private Transform knightTransform;
    private SpriteRenderer knightSprite;
    private int currentFacing = 1; // 1 = right, -1 = left
    private bool isActive;
    private int currentDamage;

    public bool IsActive => isActive;
    public int Damage => currentDamage > 0 ? currentDamage : damage;
    public float ForwardOffset => forwardOffset;
    public float VerticalOffset => verticalOffset;
    public Vector2 HitboxSize => hitboxSize;

    public event System.Action<EnemyHealth, int> OnHitEnemy;

    private void Awake()
    {
        currentDamage = damage;
        EnsureComponents();
        ConfigureCollider();
        UpdatePosition();

        // Start inactive outside attack window
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

        if (knightTransform == null)
            knightTransform = transform.parent;

        if (knightTransform != null && knightSprite == null)
            knightSprite = knightTransform.GetComponent<SpriteRenderer>();

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

    public void SetDamage(int newDamage)
    {
        currentDamage = newDamage;
    }

    public void ResetDamage()
    {
        currentDamage = damage;
    }

    public void UpdatePosition()
    {
        EnsureComponents();

        if (knightSprite != null && currentFacing == 0)
        {
            currentFacing = knightSprite.flipX ? -1 : 1;
        }

        transform.localPosition = new Vector3(
            Mathf.Abs(forwardOffset) * currentFacing,
            verticalOffset,
            0f
        );
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    public void EnableHitbox(int attackDamage)
    {
        SetDamage(attackDamage);
        EnableHitbox();
    }

    public void EnableHitbox()
    {
        EnsureComponents();
        hitEnemies.Clear();
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
            TryDamageEnemy(overlapResults[i]);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive)
            return;

        TryDamageEnemy(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive)
            return;

        TryDamageEnemy(other);
    }

    private void TryDamageEnemy(Collider2D other)
    {
        if (other == null)
            return;

        // Ignore Knight's own colliders
        if (knightTransform != null && (other.transform == knightTransform || other.transform.IsChildOf(knightTransform)))
            return;

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null || !hitEnemies.Add(enemy))
            return;

        enemy.TakeDamage(Damage);
        OnHitEnemy?.Invoke(enemy, Damage);
    }

    private void OnDrawGizmos()
    {
        Collider2D col = hitboxCollider != null ? hitboxCollider : GetComponent<Collider2D>();
        if (col == null)
            return;

        Bounds bounds = col.bounds;
        if (isActive)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.45f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        else
        {
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.2f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
