using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 7.0f;
    [SerializeField] private int damage = 2;
    [SerializeField] private float lifetime = 3.5f;

    private int direction = -1;
    private Rigidbody2D rb;
    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public void Launch(int dir, int dmg = 2)
    {
        direction = dir >= 0 ? 1 : -1;
        damage = dmg;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = (direction == -1);
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * speed, 0f);
        }

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        var km = collision.GetComponent<KnightMovement>();
        if (km == null) km = collision.GetComponentInParent<KnightMovement>();

        if (km != null)
        {
            hasHit = true;
            km.TakeDamage(damage, gameObject);
            Destroy(gameObject, 0.05f);
        }
    }
}

