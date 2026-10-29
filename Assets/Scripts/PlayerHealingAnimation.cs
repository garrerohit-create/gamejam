using System.Collections;
using UnityEngine;

public class PlayerHealingAnimation : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite flaskSprite;
    [SerializeField] private Sprite healRingSprite;
    [SerializeField] private Sprite sparkleSprite;

    [Header("Animation Settings")]
    [SerializeField] private float healDuration = 0.75f;
    [SerializeField] private Color healGlowColor = new Color(0.35f, 1.0f, 0.55f, 1f);

    private KnightMovement knightMovement;
    private PlayerHealth playerHealth;
    private SpriteRenderer knightRenderer;
    private Color originalKnightColor = Color.white;

    private void Awake()
    {
        knightMovement = GetComponent<KnightMovement>();
        playerHealth = GetComponent<PlayerHealth>();
        knightRenderer = GetComponent<SpriteRenderer>();
        if (knightRenderer != null) originalKnightColor = knightRenderer.color;

        LoadSprites();
    }

    private void LoadSprites()
    {
#if UNITY_EDITOR
        if (flaskSprite == null)
            flaskSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/Flask_Icon.png");
        if (healRingSprite == null)
            healRingSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/HealRing_VFX.png");
        if (sparkleSprite == null)
            sparkleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/HealSparkle.png");
#endif
    }

    public void PlayHealAnimation()
    {
        LoadSprites();

        if (knightMovement != null)
        {
            knightMovement.TriggerHealAnimation(healDuration);
        }

        StartCoroutine(HealVisualRoutine());
    }

    private IEnumerator HealVisualRoutine()
    {
        int facing = 1;
        if (knightRenderer != null && knightRenderer.flipX) facing = -1;

        // 1. Animated Ground Vitality Circle
        var ringObj = new GameObject("Heal_GroundRing");
        ringObj.transform.position = transform.position + new Vector3(0f, -0.2f, 0f);
        var ringSr = ringObj.AddComponent<SpriteRenderer>();
        if (healRingSprite != null) ringSr.sprite = healRingSprite;
        ringSr.color = new Color(0.3f, 1.0f, 0.6f, 0f);
        ringSr.sortingLayerName = "Default";
        ringSr.sortingOrder = 3;

        // 2. Animated Flask in Knight's Hands
        var flaskObj = new GameObject("Heal_Flask_Prop");
        flaskObj.transform.position = transform.position + new Vector3(facing * 0.45f, 1.35f, 0f);
        var flaskSr = flaskObj.AddComponent<SpriteRenderer>();
        if (flaskSprite != null) flaskSr.sprite = flaskSprite;
        flaskSr.sortingLayerName = "Default";
        flaskSr.sortingOrder = 16;
        flaskObj.transform.localScale = Vector3.one * 0.45f;

        // 3. Spiral Vitality Particle System
        var particleObj = new GameObject("Heal_SpiralParticles");
        particleObj.transform.position = transform.position + new Vector3(0f, 0.5f, 0f);
        var ps = particleObj.AddComponent<ParticleSystem>();
        var psr = particleObj.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Sprites/Default");
        if (shader != null) psr.material = new Material(shader);
        if (sparkleSprite != null && psr.material != null) psr.material.mainTexture = sparkleSprite.texture;
        psr.sortingLayerName = "Default";
        psr.sortingOrder = 18;

        var main = ps.main;
        main.duration = healDuration;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.gravityModifier = -1.2f; // Rises dynamically
        main.startColor = new Color(0.3f, 1.0f, 0.6f, 1f);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 35;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.55f;

        ps.Play();

        // Animate Timeline
        float elapsed = 0f;
        while (elapsed < healDuration)
        {
            float t = elapsed / healDuration;

            // Animate Flask tilt (rises and tilts back to helmet to drink)
            float tiltAngle = Mathf.Lerp(0f, facing * -45f, Mathf.Sin(t * Mathf.PI));
            flaskObj.transform.rotation = Quaternion.Euler(0f, 0f, tiltAngle);
            flaskObj.transform.position = transform.position + new Vector3(facing * 0.42f, 1.35f + Mathf.Sin(t * Mathf.PI) * 0.25f, 0f);

            // Animate Ground Ring (rotates and expands)
            ringObj.transform.rotation = Quaternion.Euler(0f, 0f, elapsed * 120f);
            float ringScale = Mathf.Lerp(0.6f, 1.35f, Mathf.Sin(t * Mathf.PI));
            ringObj.transform.localScale = Vector3.one * ringScale;
            float ringAlpha = Mathf.Sin(t * Mathf.PI) * 0.85f;
            ringSr.color = new Color(0.3f, 1.0f, 0.6f, ringAlpha);

            // Knight Body Vitality Glow at drinking peak
            if (knightRenderer != null)
            {
                float glow = Mathf.Sin(t * Mathf.PI);
                knightRenderer.color = Color.Lerp(originalKnightColor, healGlowColor, glow * 0.75f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Clean up
        if (knightRenderer != null) knightRenderer.color = originalKnightColor;

        if (Application.isPlaying)
        {
            Destroy(ringObj);
            Destroy(flaskObj);
            Destroy(particleObj, 0.5f);
        }
        else
        {
            DestroyImmediate(ringObj);
            DestroyImmediate(flaskObj);
            DestroyImmediate(particleObj);
        }
    }
}

