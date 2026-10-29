using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpikeHazard : MonoBehaviour
{
    [Header("Hazard Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 1.0f;
    [SerializeField] private float upwardKnockback = 10f;
    [SerializeField] private float horizontalKnockback = 4f;

    private float nextDamageTime;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void TryDamagePlayer(GameObject target)
    {
        if (Time.time < nextDamageTime)
            return;

        var km = target.GetComponent<KnightMovement>();
        if (km == null)
            km = target.GetComponentInParent<KnightMovement>();

        if (km != null)
        {
            nextDamageTime = Time.time + damageCooldown;
            km.TakeDamage(damage, gameObject);
            CameraShake.Shake(0.28f, 0.35f);

            var rb = km.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Knock player away from spike center and up
                float dirX = target.transform.position.x >= transform.position.x ? 1f : -1f;
                rb.linearVelocity = new Vector2(dirX * horizontalKnockback, upwardKnockback);
            }
        }
    }
}
