using UnityEngine;
using UnityEngine.InputSystem;

#pragma warning disable 0649
public class CarWeapon : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private float fireRate = 0.15f; // Time between shots in seconds
    [SerializeField] private float bulletSpeed = 40f;

    [Header("Turret Rotation (Optional)")]
    [SerializeField] private Transform turretPivot;
    [SerializeField] private float turretRotateSpeed = 5f;
#pragma warning restore 0649

    private OffroadCarController carController;
    private float nextFireTime;

    private void Start()
    {
        carController = GetComponent<OffroadCarController>();

        // Auto-assign muzzle and turret if not set
        if (turretPivot == null) turretPivot = transform.Find("Visuals/TurretPivot");
        if (muzzlePoint == null)
        {
            muzzlePoint = transform.Find("Visuals/TurretPivot/Muzzle");
            if (muzzlePoint == null) muzzlePoint = transform.Find("Visuals/TurretPivot/Turret/Muzzle");
        }
    }

    private void Update()
    {
        HandleTurretAiming();
        HandleShooting();
    }

    private void HandleTurretAiming()
    {
        if (turretPivot == null) return;

        if (carController != null && carController.lockedTarget != null)
        {
            // Aim smoothly at the locked-on target's visual center
            Vector3 targetPoint = carController.lockedTarget.transform.position;
            targetPoint.y += 0.8f; // Aim towards the upper center of the target dummy

            Vector3 lookDir = targetPoint - turretPivot.position;
            lookDir.y = 0f; // Keep the turret level in horizontal plane

            if (lookDir.sqrMagnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                // Faster turret alignment when locked on
                turretPivot.rotation = Quaternion.Slerp(turretPivot.rotation, targetRotation, turretRotateSpeed * 2.5f * Time.deltaTime);
            }
        }
        else
        {
            // Smoothly return turret to face forward when not locked on
            Quaternion targetRotation = transform.rotation;
            turretPivot.rotation = Quaternion.Slerp(turretPivot.rotation, targetRotation, turretRotateSpeed * Time.deltaTime);
        }
    }

    private void HandleShooting()
    {
        // Handled completely automatically during lock-on. Manual shooting is deleted as requested.
        bool isLockedOn = carController != null && carController.lockedTarget != null;

        if (isLockedOn && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            FireBullet();
        }
    }

    private void FireBullet()
    {
        if (muzzlePoint == null) return;

        // Play gunshot SFE
        SoundManager.PlayGunShot(muzzlePoint.position);

        // Create bullet
        GameObject bulletObj;
        if (bulletPrefab != null)
        {
            bulletObj = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        }
        else
        {
            // Fallback: programmatic bullet if prefab is not set
            bulletObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletObj.transform.position = muzzlePoint.position;
            bulletObj.transform.rotation = muzzlePoint.rotation;
            bulletObj.transform.localScale = Vector3.one * 0.25f;

            // Make it look yellow/orange
            var renderer = bulletObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                renderer.material.color = new Color(1.0f, 0.6f, 0.0f); // Bright orange
            }

            // Ensure collider is a trigger
            var col = bulletObj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Add Bullet component
            bulletObj.AddComponent<Bullet>();
        }

        // Set bullet speed/direction via Rigidbody
        Rigidbody bulletRb = bulletObj.GetComponent<Rigidbody>();
        if (bulletRb == null)
        {
            bulletRb = bulletObj.AddComponent<Rigidbody>();
            bulletRb.useGravity = false;
        }
        
        bulletRb.linearVelocity = muzzlePoint.forward * bulletSpeed;

        // Small visual kickback on the muzzle
        StartCoroutine(KickbackVisual());
    }

    private System.Collections.IEnumerator KickbackVisual()
    {
        if (turretPivot != null)
        {
            Transform turretBody = turretPivot.Find("Model_Turret");
            if (turretBody == null) turretBody = turretPivot.Find("Turret");

            if (turretBody != null)
            {
                Vector3 originalPos = turretBody.localPosition;
                Vector3 kickbackPos = originalPos - Vector3.forward * 0.15f;

                // Move back
                turretBody.localPosition = kickbackPos;
                yield return new WaitForSeconds(0.05f);
                // Return to original
                turretBody.localPosition = originalPos;
            }
        }
    }
}
