using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 3.0f;
    [SerializeField] private float damage = 1f;

    private void Start()
    {
        // Automatically destroy after lifetime expires
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore trigger collisions with the player car or other bullets
        if (other.CompareTag("Player") || other.GetComponent<Bullet>() != null)
        {
            return;
        }

        // Check if we hit a target dummy
        TargetDummy target = other.GetComponentInParent<TargetDummy>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }

        // Spawn a simple impact effect programmatically
        CreateImpactVisual(transform.position);

        // Destroy the bullet
        Destroy(gameObject);
    }

    private void CreateImpactVisual(Vector3 position)
    {
        // Programmatic simple particle explosion effect
        GameObject expRoot = new GameObject("ImpactParticles");
        expRoot.transform.position = position;

        // Spawn a few small cubes that fly outwards
        int particleCount = 6;
        for (int i = 0; i < particleCount; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.transform.position = position;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.15f);

            var renderer = p.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                renderer.material.color = new Color(1f, 0.8f, 0.2f); // Golden yellow
            }

            var col = p.GetComponent<Collider>();
            if (col != null) Destroy(col); // No collision calculation for particles

            var rb = p.AddComponent<Rigidbody>();
            rb.useGravity = true;
            // Throw them outwards
            Vector3 forceDir = Random.onUnitSphere;
            if (forceDir.y < 0) forceDir.y = -forceDir.y; // force upwards
            rb.AddForce(forceDir * Random.Range(3f, 7f), ForceMode.Impulse);

            Destroy(p, 0.5f);
        }

        Destroy(expRoot, 0.6f);
    }
}
