using UnityEngine;
using System.Collections;

public class FlashEffectManager : MonoBehaviour
{
    public static FlashEffectManager Instance { get; private set; }

    [Header("Flash Light Settings")]
    [SerializeField] private float flashIntensity = 12f;
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private Color flashColor = new Color(1f, 0.95f, 0.85f); // Slightly warm white for impact feel
    [SerializeField] private float defaultFlashPitch = 20f;                  // Fixed X-axis tilt (pitch) in degrees to strike terrain nicely

    [Header("Main Light Reference (Auto-detected if null)")]
    [SerializeField] private Light mainDirectionalLight;

    private int activeFlashCount = 0;

    // Backup properties to restore Main Camera clear flags
    private CameraClearFlags originalClearFlags;
    private Color originalBackgroundColor;
    private bool cameraBackedUp = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (mainDirectionalLight == null)
        {
            FindMainDirectionalLight();
        }
    }

    private void FindMainDirectionalLight()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional && l.gameObject.name == "Directional Light")
            {
                mainDirectionalLight = l;
                break;
            }
        }

        // Fallback: take first active directional light
        if (mainDirectionalLight == null)
        {
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    mainDirectionalLight = l;
                    break;
                }
            }
        }
    }

    public static void TriggerFlash(Vector3 enemyPosition)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.FlashCoroutine(enemyPosition));
        }
    }

    private IEnumerator FlashCoroutine(Vector3 enemyPosition)
    {
        activeFlashCount++;

        // Find player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("PlayerCar");

        // Calculate rotation pointing towards the player on the flat XZ plane (Yaw only)
        Vector3 directionFlat = Vector3.forward;
        if (player != null)
        {
            directionFlat = player.transform.position - enemyPosition;
            directionFlat.y = 0f; // Ignore height difference for horizontal direction
        }

        if (directionFlat.sqrMagnitude < 0.001f)
        {
            directionFlat = Vector3.forward;
        }

        // 1. Point horizontally towards the player (Y-axis Yaw rotation only)
        Quaternion yawRotation = Quaternion.LookRotation(directionFlat.normalized, Vector3.up);

        // 2. Rotate downward on local X-axis by the fixed pitch angle
        Quaternion finalRotation = yawRotation * Quaternion.Euler(defaultFlashPitch, 0f, 0f);

        // If main light reference is lost, try finding it again
        if (mainDirectionalLight == null)
        {
            FindMainDirectionalLight();
        }

        // Find Main Camera to apply Solid Color flash
        Camera mainCam = Camera.main;

        // Disable main scene light to prevent shadow and light source conflict
        if (mainDirectionalLight != null)
        {
            mainDirectionalLight.enabled = false;
        }

        // Apply Solid Color White background to the Camera
        if (mainCam != null)
        {
            if (!cameraBackedUp)
            {
                originalClearFlags = mainCam.clearFlags;
                originalBackgroundColor = mainCam.backgroundColor;
                cameraBackedUp = true;
            }

            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.white;
        }

        // Create temporary directional light
        GameObject flashLightGo = new GameObject("TemporaryFlashLight");
        Light flashLight = flashLightGo.AddComponent<Light>();
        flashLight.type = LightType.Directional;
        flashLight.color = flashColor;
        
        // Point from enemy to player with fixed pitch
        flashLightGo.transform.rotation = finalRotation;
        flashLight.intensity = flashIntensity;
        flashLight.shadows = LightShadows.Soft;

        yield return new WaitForSeconds(flashDuration);

        // Clean up temporary light
        if (flashLightGo != null)
        {
            Destroy(flashLightGo);
        }

        activeFlashCount--;

        // If no other flashes are running, restore the environment
        if (activeFlashCount == 0)
        {
            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.enabled = true;
            }

            if (mainCam != null && cameraBackedUp)
            {
                mainCam.clearFlags = originalClearFlags;
                mainCam.backgroundColor = originalBackgroundColor;
                cameraBackedUp = false; // reset backup state
            }
        }
    }
}
