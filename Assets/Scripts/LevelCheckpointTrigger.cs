using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelCheckpointTrigger : MonoBehaviour
{
    [SerializeField] private Vector3 respawnOffset = new Vector3(0f, 1f, 0f);
    private bool activated;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (activated) return;

        var km = collision.GetComponent<KnightMovement>();
        if (km == null) km = collision.GetComponentInParent<KnightMovement>();

        if (km != null)
        {
            activated = true;
            FallChasmTrigger.SetCheckpoint(transform.position + respawnOffset);
        }
    }
}
