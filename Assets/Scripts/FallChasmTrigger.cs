using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FallChasmTrigger : MonoBehaviour
{
    [SerializeField] private int fallDamage = 1;
    [SerializeField] private Vector3 defaultRespawnPoint = new Vector3(0f, 0f, 0f);

    private static Vector3 currentCheckpoint;
    private static bool hasCheckpoint;

    public static void SetCheckpoint(Vector3 checkpointPos)
    {
        currentCheckpoint = checkpointPos;
        hasCheckpoint = true;
    }

    public static Vector3 GetCurrentCheckpoint(Vector3 fallback)
    {
        return hasCheckpoint ? currentCheckpoint : fallback;
    }

    private void Awake()
    {
        if (!hasCheckpoint)
        {
            currentCheckpoint = defaultRespawnPoint;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var km = collision.GetComponent<KnightMovement>();
        if (km == null) km = collision.GetComponentInParent<KnightMovement>();

        if (km != null)
        {
            km.TakeDamage(fallDamage, gameObject);
            CameraShake.Shake(0.35f, 0.4f);

            var rb = km.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            km.transform.position = hasCheckpoint ? currentCheckpoint : defaultRespawnPoint;
        }
    }
}
