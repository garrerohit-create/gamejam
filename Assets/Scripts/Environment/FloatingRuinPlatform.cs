using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FloatingRuinPlatform : MonoBehaviour
{
    [Header("Hover Physics")]
    [SerializeField] private float bobAmplitude = 0.07f;
    [SerializeField] private float bobFrequency = 1.15f;
    [SerializeField] private float landingDip = 0.06f;

    private Vector3 initialWorldPos;
    private float timeOffset;
    private float currentDip = 0f;
    private Rigidbody2D rb;

    private void Awake()
    {
        initialWorldPos = transform.position;
        timeOffset = Random.Range(0f, 6.28f);

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void FixedUpdate()
    {
        if (currentDip > 0.001f)
        {
            currentDip = Mathf.MoveTowards(currentDip, 0f, Time.fixedDeltaTime * 0.35f);
        }

        float hoverY = Mathf.Sin((Time.time + timeOffset) * bobFrequency) * bobAmplitude - currentDip;
        Vector3 targetPos = initialWorldPos + new Vector3(0f, hoverY, 0f);

        if (rb != null)
        {
            rb.MovePosition(targetPos);
        }
        else
        {
            transform.position = targetPos;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<KnightMovement>() != null)
        {
            currentDip = landingDip;
        }
    }
}
