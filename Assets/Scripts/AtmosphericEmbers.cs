using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AtmosphericEmbers : MonoBehaviour
{
    [Header("Embers Settings")]
    [SerializeField] private int maxEmbers = 45;
    [SerializeField] private Vector2 windVelocity = new Vector2(-1.2f, 0.45f);
    [SerializeField] private Color emberColorA = new Color(1.0f, 0.45f, 0.12f, 0.85f); // Fiery orange
    [SerializeField] private Color emberColorB = new Color(0.95f, 0.15f, 0.15f, 0.75f); // Crimson red

    private ParticleSystem ps;
    private Transform camTransform;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        SetupParticleSystem();
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        // Follow camera view smoothly so embers are always visible around the player
        if (camTransform != null)
        {
            transform.position = new Vector3(camTransform.position.x + 2.0f, camTransform.position.y, 0f);
        }
    }

    private void SetupParticleSystem()
    {
        if (ps == null) ps = GetComponent<ParticleSystem>();
        if (ps == null) return;

        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.maxParticles = maxEmbers;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.0f, 5.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);

        // Gradient between fiery orange and crimson ember
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(emberColorA, 0.0f), new GradientColorKey(emberColorB, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.2f), new GradientAlphaKey(0.85f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Emission rate
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 8f;

        // Box shape spawning in front of camera
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(26f, 15f, 1f);

        // Wind / Force over lifetime
        var force = ps.forceOverLifetime;
        force.enabled = true;
        force.space = ParticleSystemSimulationSpace.World;
        force.x = new ParticleSystem.MinMaxCurve(windVelocity.x - 0.4f, windVelocity.x + 0.4f);
        force.y = new ParticleSystem.MinMaxCurve(windVelocity.y - 0.2f, windVelocity.y + 0.3f);

        // Size over lifetime (fade shrink)
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        var curve = new AnimationCurve();
        curve.AddKey(0f, 0.3f);
        curve.AddKey(0.3f, 1.0f);
        curve.AddKey(1f, 0.1f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // Renderer
        var rend = GetComponent<ParticleSystemRenderer>();
        if (rend != null)
        {
            rend.sortingLayerName = "Default";
            rend.sortingOrder = 8; // In front of platforms/characters, behind HUD
#if UNITY_EDITOR
            var emberMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Effects/Mat_BloodSlash.mat");
            if (emberMat != null) rend.sharedMaterial = emberMat;
#endif
        }
    }
}

