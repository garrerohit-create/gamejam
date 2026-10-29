using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Rates")]
    [Tooltip("0 = fixed to camera (moves 100% with camera), 1 = fixed in world space (doesn't follow camera)")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxEffectX = 0.85f;
    [Range(0f, 1f)]
    [SerializeField] private float parallaxEffectY = 0.85f;

    private Transform camTransform;
    private Vector3 lastCamPos;

    private void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
            lastCamPos = camTransform.position;
        }
    }

    private void LateUpdate()
    {
        if (camTransform == null)
        {
            if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
                lastCamPos = camTransform.position;
            }
            return;
        }

        Vector3 delta = camTransform.position - lastCamPos;
        // Move with camera proportional to parallax effect
        transform.position += new Vector3(delta.x * parallaxEffectX, delta.y * parallaxEffectY, 0f);
        lastCamPos = camTransform.position;
    }
}
