using UnityEngine;

#pragma warning disable 0649
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(OffroadCarController))]
[RequireComponent(typeof(Rigidbody))]
public class CarEngineSound : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip engineLoopClip;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 2.0f;
    [SerializeField] private float minVolume = 0.2f;
    [SerializeField] private float maxVolume = 0.75f;

    [Header("Engine Load Settings")]
    [SerializeField] private float throttlePitchBoost = 0.3f;
    [SerializeField] private float throttleVolumeBoost = 0.1f;
    [SerializeField] private float driftPitchBoost = 0.3f;           // Extra pitch swell to simulate high-RPM wheelspin during lock-on drift
    [SerializeField] private float smoothSpeed = 6f;

    private AudioSource audioSource;
    private OffroadCarController carController;
    private Rigidbody rb;

    private float currentPitch;
    private float currentVolume;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        carController = GetComponent<OffroadCarController>();
        rb = GetComponent<Rigidbody>();

        // Configure looping audio source
        audioSource.clip = engineLoopClip;
        audioSource.loop = true;
        audioSource.spatialBlend = 0.0f; // Play as 2D for the player's own cockpit experience
        audioSource.playOnAwake = true;

        currentPitch = minPitch;
        currentVolume = minVolume;
        audioSource.pitch = currentPitch;
        audioSource.volume = currentVolume;

        if (engineLoopClip != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("[CarEngineSound] EngineLoop clip is not assigned.");
        }
    }

    private void Update()
    {
        if (rb == null || carController == null || audioSource == null) return;

        // 1. Calculate speed ratio
        float currentSpeed = rb.linearVelocity.magnitude;
        float maxSpeed = carController.TargetCruisingSpeed;
        if (maxSpeed < 1f) maxSpeed = 18f; // fallback

        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);

        // 2. Get throttle input load factor
        float throttleInput = Mathf.Clamp01(Mathf.Abs(carController.MotorInput));

        // 3. Compute target pitch and volume
        bool isDrifting = carController.lockedTarget != null;
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, speedRatio) + (throttleInput * throttlePitchBoost);
        
        if (isDrifting)
        {
            targetPitch += driftPitchBoost;
        }

        float targetVolume = Mathf.Lerp(minVolume, maxVolume, speedRatio) + (throttleInput * throttleVolumeBoost);

        // Keep volume within sensible boundaries
        targetVolume = Mathf.Clamp(targetVolume, minVolume, maxVolume + throttleVolumeBoost);

        // 4. Smoothly interpolate to avoid harsh jumps
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, smoothSpeed * Time.deltaTime);
        currentVolume = Mathf.Lerp(currentVolume, targetVolume, smoothSpeed * Time.deltaTime);

        // 5. Apply to AudioSource
        audioSource.pitch = currentPitch;
        audioSource.volume = currentVolume;
    }
}
#pragma warning restore 0649
