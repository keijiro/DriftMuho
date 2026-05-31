using UnityEngine;

public class TireSmokeController : MonoBehaviour
{
    [Header("Smoke Settings")]
    [SerializeField] private Texture2D smokeTexture;
    [SerializeField] private float slipThreshold = 1.8f;     // Side velocity (m/s) before smoke begins
    [SerializeField] private float maxSlipSpeed = 10.0f;     // Side velocity (m/s) for maximum smoke density
    [SerializeField] private float maxEmissionRate = 75f;    // Max particles per second

    private OffroadCarController carController;
    private Rigidbody carRigidbody;

    private ParticleSystem leftSmokePS;
    private ParticleSystem rightSmokePS;
    private Material smokeMaterial;

    private void Reset()
    {
        #if UNITY_EDITOR
        smokeTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Smoke.png");
        #endif
    }

    private void Start()
    {
        carController = GetComponent<OffroadCarController>();
        carRigidbody = GetComponent<Rigidbody>();

        if (carController == null || carRigidbody == null)
        {
            Debug.LogError("TireSmokeController: Missing OffroadCarController or Rigidbody on " + gameObject.name);
            enabled = false;
            return;
        }

        // Auto-load texture if missing
        if (smokeTexture == null)
        {
            #if UNITY_EDITOR
            smokeTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Smoke.png");
            #endif
        }

        // Create URP Particles Unlit Material
        smokeMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        
        // Configure material for alpha-blended transparency in URP
        smokeMaterial.SetFloat("_Surface", 1.0f); // 1.0f = Transparent surface
        smokeMaterial.SetFloat("_Blend", 0.0f);   // 0.0f = Alpha blend
        smokeMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        smokeMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        smokeMaterial.SetFloat("_ZWrite", 0.0f);
        smokeMaterial.DisableKeyword("_ALPHATEST_ON");
        smokeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        smokeMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        if (smokeTexture != null)
        {
            smokeMaterial.SetTexture("_BaseMap", smokeTexture);
        }

        // Create smoke emitters as children of each rear wheel collider's pivot
        leftSmokePS = CreateSmokeEmitter(carController.RearLeft.collider, "LeftTireSmokeTrail");
        rightSmokePS = CreateSmokeEmitter(carController.RearRight.collider, "RightTireSmokeTrail");
    }

    private ParticleSystem CreateSmokeEmitter(WheelCollider wc, string emitterName)
    {
        if (wc == null) return null;

        GameObject emitterGO = new GameObject(emitterName);
        emitterGO.transform.SetParent(wc.transform, false);
        emitterGO.transform.localPosition = Vector3.zero;

        ParticleSystem ps = emitterGO.AddComponent<ParticleSystem>();
        // Stop playing before modifying properties to avoid "Setting the duration while system is still playing is not supported" warning/error.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystemRenderer psRenderer = emitterGO.GetComponent<ParticleSystemRenderer>();

        // Apply custom URP unlit material
        psRenderer.material = smokeMaterial;

        // Configure Particle System Main Module
        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.gravityModifier = -0.06f; // Make smoke drift upwards gently
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Critical: smoke stays in world space!
        main.maxParticles = 600;

        // Configure Particle System Emission Module
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        // Configure Particle System Shape Module (Point / Small Sphere)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        // Configure Color Over Lifetime (Smooth Fade-Out)
        var colorModule = ps.colorOverLifetime;
        colorModule.enabled = true;
        Gradient fadeGradient = new Gradient();
        fadeGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.9f, 0.9f, 0.9f), 0f), new GradientColorKey(new Color(0.85f, 0.85f, 0.85f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0.0f, 1f) }
        );
        colorModule.color = new ParticleSystem.MinMaxGradient(fadeGradient);

        // Configure Size Over Lifetime (Smoke dissipates and grows larger)
        var sizeModule = ps.sizeOverLifetime;
        sizeModule.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.4f);
        sizeCurve.AddKey(1f, 2.2f);
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Re-play the stopped particle system so that it's in the playing state and ready to emit particles when rateOverTime is updated.
        ps.Play();

        return ps;
    }

    private void Update()
    {
        UpdateWheelSmoke(carController.RearLeft.collider, leftSmokePS);
        UpdateWheelSmoke(carController.RearRight.collider, rightSmokePS);
    }

    private void UpdateWheelSmoke(WheelCollider wc, ParticleSystem ps)
    {
        if (wc == null || ps == null) return;

        var emission = ps.emission;

        if (wc.isGrounded && wc.GetGroundHit(out WheelHit hit))
        {
            // Position the emitter exactly at the tire's ground contact point (raised slightly to avoid floor clipping)
            ps.transform.position = hit.point + Vector3.up * 0.1f;

            // Get exact velocity of the wheel's physical point on the car
            Vector3 wheelVelocity = carRigidbody.GetPointVelocity(wc.transform.position);

            // Calculate lateral (sideways) movement velocity relative to the tire's current direction
            // If the car drifts, it slides sideways, which results in a high lateral speed
            float lateralSlipSpeed = Mathf.Abs(Vector3.Dot(wheelVelocity, wc.transform.right));

            if (lateralSlipSpeed > slipThreshold)
            {
                // Linearly interpolate slip intensity to scale emission rate
                float intensityFactor = Mathf.InverseLerp(slipThreshold, maxSlipSpeed, lateralSlipSpeed);
                emission.rateOverTime = intensityFactor * maxEmissionRate;
            }
            else
            {
                emission.rateOverTime = 0f;
            }
        }
        else
        {
            // Tire is in mid-air: stop all smoke emission instantly
            emission.rateOverTime = 0f;
        }
    }

    private void OnDestroy()
    {
        if (smokeMaterial != null)
        {
            Destroy(smokeMaterial);
        }
    }
}
// Force compile under newly resolved ParticleSystemModule context
