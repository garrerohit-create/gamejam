using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class HeavyAttackEffects : MonoBehaviour
{
    [Header("Prefabs & References")]
    [SerializeField] private GameObject heavyImpactPrefab;
    [SerializeField] private ParticleSystem chargeParticles;
    [SerializeField] private Light2D chargeLight;

    [Header("Charge Flare")]
    [SerializeField] private ParticleSystem maxChargeFlare;

    [Header("Camera Shake Settings")]
    [SerializeField] private float baseShakeDuration = 0.22f;
    [SerializeField] private float baseShakeMagnitude = 0.25f;
    [SerializeField] private float maxShakeMagnitude = 0.50f;

    [Header("Hit Stop")]
    [SerializeField] private float hitStopDuration = 0.06f;

    private Coroutine hitStopRoutine;
    private bool wasFullyCharged;

    private void Awake()
    {
        if (chargeParticles != null)
            chargeParticles.Stop();

        if (chargeLight != null)
            chargeLight.enabled = false;
    }

    public void SetCharging(bool isCharging, float chargeRatio)
    {
        if (chargeParticles != null)
        {
            if (isCharging && !chargeParticles.isPlaying)
                chargeParticles.Play();
            else if (!isCharging && chargeParticles.isPlaying)
                chargeParticles.Stop();
        }

        if (chargeLight != null)
        {
            chargeLight.enabled = isCharging;
            if (isCharging)
            {
                chargeLight.intensity = Mathf.Lerp(0.5f, 3.0f, chargeRatio);
                chargeLight.color = Color.Lerp(new Color(1f, 0.6f, 0.2f), new Color(1f, 0.2f, 0.05f), chargeRatio);
            }
        }

        if (isCharging && chargeRatio >= 1.0f && !wasFullyCharged)
        {
            wasFullyCharged = true;
            if (maxChargeFlare != null)
                maxChargeFlare.Play();
            CameraShake.Shake(0.08f, 0.08f);
        }
        else if (!isCharging)
        {
            wasFullyCharged = false;
        }
    }

    public void SpawnHeavyImpact(Vector3 impactPoint, int facingDirection, float chargeRatio)
    {
        // 1. Spawn Impact VFX Prefab
        if (heavyImpactPrefab != null)
        {
            GameObject vfx = Instantiate(heavyImpactPrefab, impactPoint, Quaternion.identity);
            
            // Flip if facing left
            if (facingDirection < 0)
            {
                vfx.transform.localScale = new Vector3(-1f, 1f, 1f);
            }

            // Scale light & particles with charge
            var light2D = vfx.GetComponentInChildren<Light2D>();
            if (light2D != null)
            {
                light2D.intensity = Mathf.Lerp(2.5f, 5.0f, chargeRatio);
                light2D.pointLightOuterRadius = Mathf.Lerp(3.5f, 6.0f, chargeRatio);
            }

            var pss = vfx.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in pss)
            {
                var main = ps.main;
                main.startSizeMultiplier *= Mathf.Lerp(1.0f, 1.4f, chargeRatio);
            }
        }

        // 2. Camera Shake scaled by charge
        float shakeMag = Mathf.Lerp(baseShakeMagnitude, maxShakeMagnitude, chargeRatio);
        CameraShake.Shake(baseShakeDuration, shakeMag);
    }

    public void TriggerHitStop()
    {
        if (hitStopRoutine != null)
            StopCoroutine(hitStopRoutine);

        hitStopRoutine = StartCoroutine(HitStopRoutine(hitStopDuration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
        hitStopRoutine = null;
    }

    private void OnDisable()
    {
        Time.timeScale = 1.0f;
    }
}

