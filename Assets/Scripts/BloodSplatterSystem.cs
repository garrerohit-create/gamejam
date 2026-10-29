using UnityEngine;

public class BloodSplatterSystem : MonoBehaviour
{
    private static BloodSplatterSystem instance;
    public static BloodSplatterSystem Instance => instance;

    [Header("Blood Visuals")]
    [SerializeField] private Sprite bloodDropletSprite;
    [SerializeField] private Material particleMaterial;
    [SerializeField] private Color deepBloodColor = new Color(0.55f, 0.02f, 0.02f, 1f);
    [SerializeField] private Color brightBloodColor = new Color(0.85f, 0.08f, 0.08f, 1f);

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (bloodDropletSprite == null)
        {
            bloodDropletSprite = Resources.Load<Sprite>("BloodDroplet");
#if UNITY_EDITOR
            if (bloodDropletSprite == null)
            {
                bloodDropletSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/BloodDroplet.png");
            }
#endif
        }

        if (particleMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                particleMaterial = new Material(shader);
            }
        }
    }

    /// <summary>
    /// Spawns a visceral blood burst at the target location, spraying in the hit direction.
    /// </summary>
    public static void SpawnBlood(Vector3 worldPos, Vector2 hitDirection, bool isHeavy = false)
    {
        if (instance == null)
        {
            var go = new GameObject("[BloodSplatterSystem]");
            instance = go.AddComponent<BloodSplatterSystem>();
        }

        instance.CreateBloodBurst(worldPos, hitDirection, isHeavy);
    }

    private void CreateBloodBurst(Vector3 worldPos, Vector2 hitDirection, bool isHeavy)
    {
        if (hitDirection.sqrMagnitude < 0.01f)
        {
            hitDirection = Vector2.up + (Random.value > 0.5f ? Vector2.right : Vector2.left);
        }
        hitDirection.Normalize();

        var bloodObj = new GameObject("BloodSplatter_FX");
        bloodObj.transform.position = worldPos;

        var ps = bloodObj.AddComponent<ParticleSystem>();
        var psr = bloodObj.GetComponent<ParticleSystemRenderer>();

        // Material & Sprite
        if (particleMaterial != null)
        {
            if (Application.isPlaying)
                psr.material = particleMaterial;
            else
                psr.sharedMaterial = particleMaterial;
        }
        if (bloodDropletSprite != null && psr.sharedMaterial != null)
        {
            psr.sharedMaterial.mainTexture = bloodDropletSprite.texture;
        }
        psr.sortingLayerName = "Default";
        psr.sortingOrder = 10; // In front of characters and ground

        // Particle System Main Module
        var main = ps.main;
        main.duration = 0.8f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(isHeavy ? 6.5f : 4.5f, isHeavy ? 11.0f : 8.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, isHeavy ? 0.42f : 0.28f);
        main.gravityModifier = 3.2f; // Fast downward arc
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Color gradient: starts bright arterial red, turns into deep drying crimson, fades out
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(brightBloodColor, 0.0f),
                new GradientColorKey(deepBloodColor, 0.4f),
                new GradientColorKey(new Color(0.25f, 0.01f, 0.01f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.9f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        // Emission Module: instantaneous burst
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        int particleCount = isHeavy ? 55 : 30;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)particleCount) });

        // Shape Module: directional cone facing hitDirection
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = isHeavy ? 45f : 30f;
        shape.radius = 0.15f;

        // Calculate rotation pointing towards hitDirection
        float angleZ = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        // Cone points along Z by default, align with 2D plane
        bloodObj.transform.rotation = Quaternion.Euler(0f, 0f, angleZ);

        // Size over lifetime: shrink before vanishing
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        var curve = new AnimationCurve();
        curve.AddKey(0.0f, 1.0f);
        curve.AddKey(0.5f, 0.85f);
        curve.AddKey(1.0f, 0.15f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        ps.Play();

        // Auto destroy after particles settle
        if (Application.isPlaying)
        {
            Destroy(bloodObj, 1.6f);
        }
        else
        {
            DestroyImmediate(bloodObj);
        }
    }
}
