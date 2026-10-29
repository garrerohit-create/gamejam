using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;
    public static CameraShake Instance => instance;

    [SerializeField] private float defaultZ = -10f;
    private Vector3 baseLocalPos;
    private Vector3 currentOffset = Vector3.zero;
    private Coroutine currentShakeRoutine;
    private bool isShaking;

    public static Vector3 CurrentOffset => instance != null ? instance.currentOffset : Vector3.zero;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(this);
            return;
        }

        CaptureBasePosition();
    }

    private void OnEnable()
    {
        CaptureBasePosition();
    }

    private void CaptureBasePosition()
    {
        baseLocalPos = transform.localPosition;
        if (Mathf.Abs(baseLocalPos.z) < 0.1f)
        {
            baseLocalPos.z = defaultZ;
            transform.localPosition = baseLocalPos;
        }
    }

    public static void Shake(float duration, float magnitude)
    {
        if (instance == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                instance = mainCam.GetComponent<CameraShake>();
                if (instance == null)
                    instance = mainCam.gameObject.AddComponent<CameraShake>();
            }
        }

        if (instance != null)
        {
            instance.StartShake(duration, magnitude);
        }
    }

    public void StartShake(float duration, float magnitude)
    {
        if (!isShaking)
        {
            CaptureBasePosition();
        }

        if (currentShakeRoutine != null)
            StopCoroutine(currentShakeRoutine);

        currentShakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        isShaking = true;
        float elapsed = 0f;
        bool hasFollow = GetComponent<CameraFollow2D>() != null;

        while (elapsed < duration)
        {
            float percent = 1f - (elapsed / duration);
            float currentMag = magnitude * percent;

            float offsetX = Random.Range(-1f, 1f) * currentMag;
            float offsetY = Random.Range(-1f, 1f) * currentMag;

            currentOffset = new Vector3(offsetX, offsetY, 0f);

            if (!hasFollow)
            {
                transform.localPosition = new Vector3(
                    baseLocalPos.x + offsetX,
                    baseLocalPos.y + offsetY,
                    baseLocalPos.z
                );
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        currentOffset = Vector3.zero;
        if (!hasFollow) transform.localPosition = baseLocalPos;
        currentShakeRoutine = null;
        isShaking = false;
    }

    private void OnDisable()
    {
        currentOffset = Vector3.zero;
        if (isShaking && GetComponent<CameraFollow2D>() == null)
        {
            transform.localPosition = baseLocalPos;
        }
        isShaking = false;
        currentShakeRoutine = null;
    }
}
