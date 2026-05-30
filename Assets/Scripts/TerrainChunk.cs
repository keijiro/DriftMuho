using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    private Vector2Int coordinates;
    private float chunkSize;
    private int resolution;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh generatedMesh;

    private int targetCount;
    private Material targetMat;
    private Material metalMat;
    private System.Collections.Generic.List<GameObject> spawnedTargets = new System.Collections.Generic.List<GameObject>();

    public Vector2Int Coordinates => coordinates;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
    }

    /// <summary>
    /// Initializes the chunk with its coordinates and parameters, and triggers the procedural mesh generation.
    /// </summary>
    public void Initialize(Vector2Int coords, float size, int res, Material material, int targetCount, Material targetMat, Material metalMat)
    {
        this.coordinates = coords;
        this.chunkSize = size;
        this.resolution = res;
        this.targetCount = targetCount;
        this.targetMat = targetMat;
        this.metalMat = metalMat;

        // Position this GameObject in world space based on chunk coordinates
        transform.position = new Vector3(coords.x * size, 0f, coords.y * size);

        meshRenderer.sharedMaterial = material;

        GenerateLowPolyMesh();
    }

    /// <summary>
    /// Re-evaluates and reconstructs the chunk's mesh (useful for runtime noise adjustment).
    /// </summary>
    public void Regenerate()
    {
        GenerateLowPolyMesh();
    }

    /// <summary>
    /// Generates a grid of vertices, samples height from global noise, and constructs
    /// a flat-shaded (low-poly) mesh by duplicating vertices for every triangle.
    /// </summary>
    private void GenerateLowPolyMesh()
    {
        if (generatedMesh != null)
        {
            DestroyImmediate(generatedMesh);
        }

        generatedMesh = new Mesh();
        generatedMesh.name = $"TerrainChunk_{coordinates.x}_{coordinates.y}";

        float cellSize = chunkSize / resolution;
        float worldOffsetX = coordinates.x * chunkSize;
        float worldOffsetZ = coordinates.y * chunkSize;

        // Total quads per chunk = resolution * resolution
        // Triangles = quads * 2
        // Vertices = triangles * 3 = resolution * resolution * 6 (flat-shaded, no shared vertices)
        int numQuads = resolution * resolution;
        int numVertices = numQuads * 6;

        Vector3[] vertices = new Vector3[numVertices];
        int[] triangles = new int[numVertices];
        Vector2[] uvs = new Vector2[numVertices];

        int vIndex = 0;

        for (int x = 0; x < resolution; x++)
        {
            for (int z = 0; z < resolution; z++)
            {
                // Local corners in chunk-local space
                float lx0 = x * cellSize;
                float lx1 = (x + 1) * cellSize;
                float lz0 = z * cellSize;
                float lz1 = (z + 1) * cellSize;

                // World corners in world space
                float wx0 = worldOffsetX + lx0;
                float wx1 = worldOffsetX + lx1;
                float wz0 = worldOffsetZ + lz0;
                float wz1 = worldOffsetZ + lz1;

                // Sample continuous height from the global noise function
                float yA = TerrainNoise.GetHeight(wx0, wz0); // Bottom-Left (A)
                float yB = TerrainNoise.GetHeight(wx1, wz0); // Bottom-Right (B)
                float yC = TerrainNoise.GetHeight(wx0, wz1); // Top-Left (C)
                float yD = TerrainNoise.GetHeight(wx1, wz1); // Top-Right (D)

                // Define 3D positions
                Vector3 pA = new Vector3(lx0, yA, lz0);
                Vector3 pB = new Vector3(lx1, yB, lz0);
                Vector3 pC = new Vector3(lx0, yC, lz1);
                Vector3 pD = new Vector3(lx1, yD, lz1);

                // Normal UV coordinates [0..1] across this chunk
                float u0 = (float)x / resolution;
                float u1 = (float)(x + 1) / resolution;
                float v0 = (float)z / resolution;
                float v1 = (float)(z + 1) / resolution;

                // --- Triangle 1: A -> C -> B (Clockwise) ---
                vertices[vIndex] = pA;
                uvs[vIndex] = new Vector2(u0, v0);
                triangles[vIndex] = vIndex;
                vIndex++;

                vertices[vIndex] = pC;
                uvs[vIndex] = new Vector2(u0, v1);
                triangles[vIndex] = vIndex;
                vIndex++;

                vertices[vIndex] = pB;
                uvs[vIndex] = new Vector2(u1, v0);
                triangles[vIndex] = vIndex;
                vIndex++;

                // --- Triangle 2: B -> C -> D (Clockwise) ---
                vertices[vIndex] = pB;
                uvs[vIndex] = new Vector2(u1, v0);
                triangles[vIndex] = vIndex;
                vIndex++;

                vertices[vIndex] = pC;
                uvs[vIndex] = new Vector2(u0, v1);
                triangles[vIndex] = vIndex;
                vIndex++;

                vertices[vIndex] = pD;
                uvs[vIndex] = new Vector2(u1, v1);
                triangles[vIndex] = vIndex;
                vIndex++;
            }
        }

        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles;
        generatedMesh.uv = uvs;

        // Recalculating normals with unshared vertices yields perfectly sharp low-poly flat shading!
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        meshFilter.sharedMesh = generatedMesh;
        meshCollider.sharedMesh = generatedMesh;

        // Spawn dynamic targets inside this chunk
        ClearSpawnedTargets();
        if (targetCount > 0)
        {
            SpawnTargets(targetCount, targetMat, metalMat);
        }
        }

        /// <summary>
        /// Spawns dynamic target dummies at consistent pseudo-random positions inside this chunk.
        /// Uses seed coordinates to keep positions identical when reloading the chunk.
        /// </summary>
        private void SpawnTargets(int count, Material matTarget, Material matMetal)
        {
        float worldOffsetX = coordinates.x * chunkSize;
        float worldOffsetZ = coordinates.y * chunkSize;

        // Save and initialize stable seed based on chunk coordinate hashing
        Random.State oldState = Random.state;
        Random.InitState(coordinates.x * 73856093 ^ coordinates.y * 19349663);

        for (int i = 0; i < count; i++)
        {
            // Pick a random location inside the chunk, keeping away from chunk edges
            float localX = Random.Range(2f, chunkSize - 2f);
            float localZ = Random.Range(2f, chunkSize - 2f);

            float wx = worldOffsetX + localX;
            float wz = worldOffsetZ + localZ;

            // Get terrain height
            float wy = TerrainNoise.GetHeight(wx, wz);

            GameObject tRoot = new GameObject($"DynamicTarget_{coordinates.x}_{coordinates.y}_{i}");
            tRoot.transform.position = new Vector3(wx, wy, wz);
            tRoot.transform.SetParent(transform);

            // Base cylinder
            GameObject baseCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseCyl.name = "BaseCylinder";
            baseCyl.transform.SetParent(tRoot.transform, false);
            baseCyl.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            baseCyl.transform.localScale = new Vector3(1.2f, 0.6f, 1.2f);
            if (matTarget != null) baseCyl.GetComponent<Renderer>().sharedMaterial = matTarget;
            DestroyImmediate(baseCyl.GetComponent<Collider>());

            // Top sphere
            GameObject topSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            topSphere.name = "TopSphere";
            topSphere.transform.SetParent(tRoot.transform, false);
            topSphere.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            topSphere.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            if (matMetal != null) topSphere.GetComponent<Renderer>().sharedMaterial = matMetal;
            DestroyImmediate(topSphere.GetComponent<Collider>());

            // Physics Rigidbody
            Rigidbody tRb = tRoot.AddComponent<Rigidbody>();
            tRb.mass = 500f;
            tRb.linearDamping = 0.5f;

            // Single Box Collider enclosing the whole target
            BoxCollider tCol = tRoot.AddComponent<BoxCollider>();
            tCol.center = new Vector3(0f, 1.1f, 0f);
            tCol.size = new Vector3(1.3f, 2.2f, 1.3f);

            // Target Behavior
            tRoot.AddComponent<TargetDummy>();

            spawnedTargets.Add(tRoot);
        }

        Random.state = oldState;
        }

        /// <summary>
        /// Cleans up all spawned targets in memory cleanly.
        /// </summary>
        private void ClearSpawnedTargets()
        {
        foreach (var t in spawnedTargets)
        {
            if (t != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(t);
                }
                else
                {
                    DestroyImmediate(t);
                }
            }
        }
        spawnedTargets.Clear();
        }

        private void OnDestroy()
        {
        ClearSpawnedTargets();

        if (generatedMesh != null)
        {
            Destroy(generatedMesh);
        }
        }
        }
