using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 2;
    [SerializeField] private bool isParryable = true;
    private Collider2D hitboxCollider;
    private BoxCollider2D boxCol;
    private Vector2 defaultSize = new Vector2(2.4f, 2.8f);
    private Vector2 defaultOffset = Vector2.zero;
    private readonly HashSet<Collider2D> alreadyDamaged = new HashSet<Collider2D>();
    private int facingDirection = -1;

    public bool IsParryable => isParryable;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
        boxCol = hitboxCollider as BoxCollider2D;
        if (boxCol != null)
        {
            defaultSize = boxCol.size;
            defaultOffset = boxCol.offset;
        }
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false;
        }
    }

    public void SetFacing(int dir)
    {
        facingDirection = dir >= 0 ? 1 : -1;
        transform.localPosition = new Vector3(Mathf.Abs(transform.localPosition.x) * facingDirection, transform.localPosition.y, 0f);
    }

    public void EnableHitbox(int dmg, bool parryable = true, Vector2? customSize = null, Vector2? customOffset = null)
    {
        damage = dmg;
        isParryable = parryable;
        alreadyDamaged.Clear();

        if (boxCol != null)
        {
            boxCol.size = customSize ?? defaultSize;
            boxCol.offset = customOffset ?? defaultOffset;
        }

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }
    }

    public void DisableHitbox()
    {
        alreadyDamaged.Clear();
        if (boxCol != null)
        {
            boxCol.size = defaultSize;
            boxCol.offset = defaultOffset;
        }
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hitboxCollider.enabled) return;
        if (alreadyDamaged.Contains(collision)) return;

        var km = collision.GetComponent<KnightMovement>();
        if (km == null) km = collision.GetComponentInParent<KnightMovement>();

        if (km != null)
        {
            alreadyDamaged.Add(collision);

            // Counterplay check: Can this attack be parried?
            if (isParryable && km.IsParrying)
            {
                bool parried = km.TryParry(damage, transform.root.gameObject);
                if (parried)
                {
                    DisableHitbox();
                    var boss = GetComponentInParent<BossAI>();
                    if (boss != null)
                    {
                        boss.OnAttackParried();
                    }
                    return;
                }
            }

            km.TakeDamage(damage, transform.root.gameObject, isParryable);
        }
    }
}
