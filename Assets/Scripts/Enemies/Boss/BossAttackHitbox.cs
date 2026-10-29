using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 2;
    private Collider2D hitboxCollider;
    private readonly HashSet<Collider2D> alreadyDamaged = new HashSet<Collider2D>();
    private int facingDirection = -1;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;
    }

    public void SetFacing(int dir)
    {
        facingDirection = dir >= 0 ? 1 : -1;
        transform.localPosition = new Vector3(Mathf.Abs(transform.localPosition.x) * facingDirection, transform.localPosition.y, 0f);
    }

    public void EnableHitbox(int dmg)
    {
        damage = dmg;
        alreadyDamaged.Clear();
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }
    }

    public void DisableHitbox()
    {
        alreadyDamaged.Clear();
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
            km.TakeDamage(damage, transform.root.gameObject);
        }
    }
}

