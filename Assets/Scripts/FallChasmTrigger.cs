using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FallChasmTrigger : MonoBehaviour
{
    [SerializeField] private int fallDamage = 1;
    [SerializeField] private Vector3 defaultRespawnPoint = new Vector3(1.61f, 2.50f, 0f);

    private static Vector3 currentCheckpoint;
    private static bool hasCheckpoint = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStaticData()
    {
        hasCheckpoint = false;
        currentCheckpoint = Vector3.zero;
    }

    public static void ResetCheckpoint()
    {
        hasCheckpoint = false;
        currentCheckpoint = Vector3.zero;
    }

    public static void SetCheckpoint(Vector3 checkpointPos)
    {
        currentCheckpoint = checkpointPos;
        hasCheckpoint = true;
        Debug.Log($"[Checkpoint] New checkpoint registered at: {checkpointPos}");
    }

    public static Vector3 GetCurrentCheckpoint(Vector3 fallback)
    {
        return hasCheckpoint ? currentCheckpoint : fallback;
    }

    public static bool HasCheckpoint => hasCheckpoint;

    private void Awake()
    {
        var knight = FindAnyObjectByType<KnightMovement>();
        if (knight != null)
        {
            defaultRespawnPoint = knight.transform.position;
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

            Vector3 targetPos = hasCheckpoint ? currentCheckpoint : defaultRespawnPoint;
            km.transform.position = targetPos;
        }
    }
}
