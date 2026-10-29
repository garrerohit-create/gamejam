using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target Tracking")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(2f, 0.4f, -10f);

    [Header("Smoothing")]
    [SerializeField] private float smoothTimeX = 0.22f;
    [SerializeField] private float smoothTimeY = 0.35f;
    [SerializeField] private float lookAheadDistance = 1.8f;

    [Header("World Limits")]
    [SerializeField] private bool useLimits = true;
    [SerializeField] private float minX = -1f;
    [SerializeField] private float maxX = 85f;
    [SerializeField] private float minY = -2f;
    [SerializeField] private float maxY = 12f;

    private float currentVelocityX;
    private float currentVelocityY;
    private float currentLookAheadX;

    public Transform Target => target;

    private void Start()
    {
        if (target == null)
        {
            var knight = FindAnyObjectByType<KnightMovement>();
            if (knight != null) target = knight.transform;
        }

        if (target != null)
        {
            float startX = target.position.x + offset.x;
            float startY = target.position.y + offset.y;
            if (useLimits)
            {
                startX = Mathf.Clamp(startX, minX, maxX);
                startY = Mathf.Clamp(startY, minY, maxY);
            }
            transform.position = new Vector3(startX, startY, offset.z);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Facing direction detection for subtle dynamic lookahead
        var sr = target.GetComponent<SpriteRenderer>();
        float facing = (sr != null && sr.flipX) ? -1f : 1f;
        float desiredLookAhead = facing * lookAheadDistance;
        currentLookAheadX = Mathf.Lerp(currentLookAheadX, desiredLookAhead, Time.deltaTime * 3.5f);

        float desiredX = target.position.x + offset.x + currentLookAheadX;
        float desiredY = target.position.y + offset.y;

        if (useLimits)
        {
            desiredX = Mathf.Clamp(desiredX, minX, maxX);
            desiredY = Mathf.Clamp(desiredY, minY, maxY);
        }

        float newX = Mathf.SmoothDamp(transform.position.x, desiredX, ref currentVelocityX, smoothTimeX);
        float newY = Mathf.SmoothDamp(transform.position.y, desiredY, ref currentVelocityY, smoothTimeY);

        // Add shake offset if active
        Vector3 shake = CameraShake.CurrentOffset;
        transform.position = new Vector3(newX + shake.x, newY + shake.y, offset.z);
    }
}
