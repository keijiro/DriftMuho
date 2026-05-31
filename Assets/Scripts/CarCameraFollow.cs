using UnityEngine;

public class CarCameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] public Transform target;
    [SerializeField] private float distance = 9.0f;       // Back distance from target
    [SerializeField] private float height = 2.4f;         // Height above target
    [SerializeField] private float positionSmoothTime = 0.05f; // Almost instant position follow (smaller = tighter)
    [SerializeField] private float rotationSmoothTime = 0.15f; // Sleek, slightly lagged rotation follow
    [SerializeField] private float lookAtOffset = 1.0f;     // Target height offset to look at

    [Header("Lock-On Settings")]
    [SerializeField] private float lockOnDistanceMultiplier = 1.15f; // Distance multiplier when locked on
    [SerializeField] private float lockOnHeightMultiplier = 1.3f;     // Height multiplier when locked on
    [SerializeField] private float maxBankAngle = 8.0f;               // Maximum camera bank (roll) in degrees
    [SerializeField] private float bankSensitivity = 15.0f;           // Speed (m/s) at which maximum bank is reached
    [SerializeField] private float bankSmoothTime = 0.15f;            // Smooth damp time for bank transition

    private OffroadCarController carController;
    private Vector3 positionVelocity;
    private float currentRotationAngle;
    private float rotationVelocity;
    private Vector3 lastPlayerPosition;

    // Blending state for Lock-On Cinematic Camera
    private float lockOnBlend = 0f;
    private float currentBankAngle = 0f;
    private float bankVelocity;

    private void Start()
    {
        if (target != null)
        {
            carController = target.GetComponent<OffroadCarController>();
            lastPlayerPosition = target.position;

            // Immediate rigid snap to correct target state on start
            currentRotationAngle = target.eulerAngles.y;
            Vector3 targetPos = CalculateTargetPosition(currentRotationAngle);
            transform.position = targetPos;
            
            Vector3 lookAtTarget = target.position + Vector3.up * lookAtOffset;
            transform.LookAt(lookAtTarget);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Ensure we have the controller cached
        if (carController == null)
        {
            carController = target.GetComponent<OffroadCarController>();
        }

        // Calculate player velocity based on frame-to-frame delta
        Vector3 playerVelocity = Vector3.zero;
        if (Time.deltaTime > 0f)
        {
            playerVelocity = (target.position - lastPlayerPosition) / Time.deltaTime;
        }
        lastPlayerPosition = target.position;

        // 1. Calculate standard chase camera values
        float targetRotationAngle = target.eulerAngles.y;
        currentRotationAngle = Mathf.SmoothDampAngle(currentRotationAngle, targetRotationAngle, ref rotationVelocity, rotationSmoothTime);
        Vector3 standardTargetPosition = CalculateTargetPosition(currentRotationAngle);
        Vector3 standardLookAt = target.position + Vector3.up * lookAtOffset;

        // 2. Smoothly update Lock-On blend state
        bool hasLockOn = carController != null && carController.lockedTarget != null;
        lockOnBlend = Mathf.MoveTowards(lockOnBlend, hasLockOn ? 1f : 0f, 2.5f * Time.deltaTime); // Transition over ~0.4s

        Vector3 lockOnTargetPosition = standardTargetPosition;
        Vector3 lockOnLookAt = standardLookAt;
        float targetBank = 0f;

        // 3. Compute Lock-On Cinematic Camera values if active/blending
        if (carController != null && carController.lockedTarget != null)
        {
            Vector3 enemyPos = carController.lockedTarget.transform.position;
            
            // Vector pointing outward from target to player
            Vector3 radialDirection = (target.position - enemyPos).normalized;
            radialDirection.y = 0f; // horizontal only
            radialDirection.Normalize();

            // Pull slightly higher and further back for cinematic lock-on framing
            lockOnTargetPosition = target.position + radialDirection * (distance * lockOnDistanceMultiplier) + Vector3.up * (height * lockOnHeightMultiplier);

            // Focus on the midpoint between the player car and the enemy to frame both perfectly
            Vector3 midpoint = (target.position + enemyPos) * 0.5f;
            lockOnLookAt = midpoint + Vector3.up * lookAtOffset;

            // Calculate centrifugal-like bank angle based on orbit direction and speed
            Vector3 horizontalToPlayer = target.position - enemyPos;
            horizontalToPlayer.y = 0f;
            if (horizontalToPlayer.sqrMagnitude > 0.001f)
            {
                Vector3 toPlayerNorm = horizontalToPlayer.normalized;
                Vector3 cross = Vector3.Cross(toPlayerNorm, playerVelocity);
                // cross.y > 0 is counter-clockwise (left orbit turn), < 0 is clockwise (right orbit turn)
                float orbitSpeed = cross.y;
                float speedRatio = Mathf.Clamp(orbitSpeed / bankSensitivity, -1f, 1f);
                targetBank = speedRatio * maxBankAngle;
            }
        }

        // Apply lock-on blend to the bank target angle
        targetBank *= lockOnBlend;

        // Smoothly interpolate the bank angle
        currentBankAngle = Mathf.SmoothDamp(currentBankAngle, targetBank, ref bankVelocity, bankSmoothTime);

        // 4. Smoothly blend standard chase and lock-on cameras
        Vector3 blendedTargetPosition = Vector3.Lerp(standardTargetPosition, lockOnTargetPosition, lockOnBlend);
        Vector3 blendedLookAt = Vector3.Lerp(standardLookAt, lockOnLookAt, lockOnBlend);

        // 5. Apply smooth damping and look at target
        transform.position = Vector3.SmoothDamp(transform.position, blendedTargetPosition, ref positionVelocity, positionSmoothTime);
        transform.LookAt(blendedLookAt);

        // Apply bank angle (Z roll) relative to look direction
        if (Mathf.Abs(currentBankAngle) > 0.01f)
        {
            transform.localRotation = transform.localRotation * Quaternion.Euler(0f, 0f, currentBankAngle);
        }
    }

    /// <summary>
    /// Computes the camera's position based on a specific Y-axis rotation angle,
    /// keeping the calculation flat on the horizontal XZ plane relative to the target's center.
    /// </summary>
    private Vector3 CalculateTargetPosition(float yawAngle)
    {
        // Convert Y angle to a flat rotation representation
        Quaternion rotation = Quaternion.Euler(0f, yawAngle, 0f);
        
        // Calculate backward offset vector in flat horizontal space
        Vector3 offset = rotation * Vector3.back * distance;
        
        // Add flat offset and absolute height offset to target position
        return target.position + offset + Vector3.up * height;
    }
}
