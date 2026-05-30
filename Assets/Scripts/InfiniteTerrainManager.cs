using UnityEngine;
using System.Collections.Generic;

public class InfiniteTerrainManager : MonoBehaviour
{
    #pragma warning disable 0649
        [Header("Player Tracking")]
        [SerializeField] private Transform playerTransform;

        [Header("Terrain Parameters")]
        [SerializeField] private float chunkSize = 30f;      // Width & length of each chunk in meters
        [SerializeField] private int resolution = 15;        // Number of subdivisions per chunk
        [SerializeField] private int viewRadius = 4;         // Number of chunks to keep loaded around player
        [SerializeField] private Material chunkMaterial;

        [Header("Dynamic Targets Setup")]
        [Tooltip("Number of target dummies to spawn per chunk")]
        [SerializeField] private int targetsPerChunk = 5;

        [Tooltip("The red target material applied to dummy bases")]
        [SerializeField] private Material targetMaterial;

        [Tooltip("The metal material applied to dummy top spheres")]
        [SerializeField] private Material metalMaterial;

        [Header("Terrain Roughness / Noise Adjustments")]
        [Tooltip("Maximum height of the hills")]
        [Range(0f, 40f)] [SerializeField] private float terrainHeightMultiplier = 7.0f;
        
        [Tooltip("The horizontal scale of the terrain hills (smaller = larger hills, larger = micro-bumps)")]
        [Range(0.001f, 0.05f)] [SerializeField] private float terrainNoiseScale = 0.008f;

        [Tooltip("Complexity of the terrain detail (number of stacked octave layers)")]
        [Range(1, 6)] [SerializeField] private int noiseOctaves = 3;

        [Tooltip("Roughness factor of detail layers (higher = more jagged micro-details)")]
        [Range(0.1f, 0.8f)] [SerializeField] private float terrainPersistence = 0.45f;

        [Tooltip("The flatness threshold. Higher values flatten more of the lower valleys into smooth plains (-1 is fully bumpy, 1 is fully flat)")]
        [Range(-1.0f, 1.0f)] [SerializeField] private float terrainFloorThreshold = -0.3f;
        #pragma warning restore 0649

    private Dictionary<Vector2Int, TerrainChunk> activeChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private Vector2Int lastPlayerChunkCoords;
    private bool isInitialized = false;

    private void Start()
    {
        // Copy current inspector settings to the static noise generator
        ApplyNoiseSettingsToNoiseGenerator();

        // Auto-detect player vehicle if not manually linked
        if (playerTransform == null)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                playerTransform = playerGO.transform;
            }
            else
            {
                playerGO = GameObject.Find("PlayerCar");
                if (playerGO != null)
                {
                    playerTransform = playerGO.transform;
                }
            }
        }

        if (playerTransform == null)
        {
            Debug.LogError("InfiniteTerrainManager: Player car target could not be found!");
            return;
        }

        InitializeTerrain();
    }

    private void Update()
    {
        if (!isInitialized || playerTransform == null) return;

        // Convert the player's current continuous position into discrete chunk coordinates
        Vector2Int currentChunkCoords = GetChunkCoordinates(playerTransform.position);

        // Update active chunks only when the player crosses a chunk boundary
        if (currentChunkCoords != lastPlayerChunkCoords)
        {
            UpdateTerrain(currentChunkCoords);
        }
    }

    /// <summary>
    /// Computes the initial set of active chunks around the player.
    /// </summary>
    private void InitializeTerrain()
    {
        Vector2Int currentChunkCoords = GetChunkCoordinates(playerTransform.position);
        UpdateTerrain(currentChunkCoords);
        isInitialized = true;
    }

    /// <summary>
    /// Spawns newly visible chunks within the view radius and destroys distant chunks.
    /// </summary>
    private void UpdateTerrain(Vector2Int currentChunkCoords)
    {
        // 1. Spawning new chunks around the player's current chunk coordinates
        for (int x = -viewRadius; x <= viewRadius; x++)
        {
            for (int z = -viewRadius; z <= viewRadius; z++)
            {
                Vector2Int chunkCoords = new Vector2Int(currentChunkCoords.x + x, currentChunkCoords.y + z);
                if (!activeChunks.ContainsKey(chunkCoords))
                {
                    SpawnChunk(chunkCoords);
                }
            }
        }

        // 2. Finding chunks that are now too far away and queueing them for unloading
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var coords in activeChunks.Keys)
        {
            if (Mathf.Abs(coords.x - currentChunkCoords.x) > viewRadius ||
                Mathf.Abs(coords.y - currentChunkCoords.y) > viewRadius)
            {
                chunksToRemove.Add(coords);
            }
        }

        // 3. Destroying and removing the out-of-range chunks
        foreach (var coords in chunksToRemove)
        {
            if (activeChunks.TryGetValue(coords, out TerrainChunk chunk))
            {
                if (chunk != null && chunk.gameObject != null)
                {
                    Destroy(chunk.gameObject);
                }
                activeChunks.Remove(coords);
            }
        }

        lastPlayerChunkCoords = currentChunkCoords;
    }

    /// <summary>
    /// Spawns a single terrain chunk at the specified chunk grid coordinates.
    /// </summary>
    private void SpawnChunk(Vector2Int coords)
    {
        GameObject chunkGO = new GameObject($"Chunk_{coords.x}_{coords.y}");
        chunkGO.transform.SetParent(transform);

        // Attach required rendering components before initialising the TerrainChunk behavior
        chunkGO.AddComponent<MeshFilter>();
        chunkGO.AddComponent<MeshRenderer>();
        var chunk = chunkGO.AddComponent<TerrainChunk>();
        
        // Auto-assign default materials if they are missing
        if (targetMaterial == null)
        {
            #if UNITY_EDITOR
            targetMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/UI/M_Target.mat");
            #endif
        }
        if (metalMaterial == null)
        {
            #if UNITY_EDITOR
            metalMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/UI/M_Metal.mat");
            #endif
        }

        chunk.Initialize(coords, chunkSize, resolution, chunkMaterial, targetsPerChunk, targetMaterial, metalMaterial);

        activeChunks.Add(coords, chunk);
    }

    /// <summary>
    /// Mathematical floor function to map continuous space to discrete integer grid coordinates.
    /// </summary>
    private Vector2Int GetChunkCoordinates(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / chunkSize),
            Mathf.FloorToInt(position.z / chunkSize)
        );
    }

    /// <summary>
    /// Synchronizes the Inspector noise parameters to the global static TerrainNoise calculation module.
    /// </summary>
    private void ApplyNoiseSettingsToNoiseGenerator()
    {
        TerrainNoise.heightMultiplier = terrainHeightMultiplier;
        TerrainNoise.baseScale = terrainNoiseScale;
        TerrainNoise.octaves = noiseOctaves;
        TerrainNoise.persistence = terrainPersistence;
        TerrainNoise.floorThreshold = terrainFloorThreshold;
    }

    /// <summary>
    /// Triggers dynamically in the Unity Editor when slider properties are adjusted in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        ApplyNoiseSettingsToNoiseGenerator();

        if (Application.isPlaying && isInitialized)
        {
            RegenerateAllChunks();
        }
    }

    /// <summary>
    /// Re-evaluates and recreates procedural meshes for all active chunks currently in memory.
    /// </summary>
    public void RegenerateAllChunks()
    {
        foreach (var chunk in activeChunks.Values)
        {
            if (chunk != null)
            {
                chunk.Regenerate();
            }
        }
    }

    // Public Getters for UI or telemetry tracking
    public int ActiveChunksCount => activeChunks.Count;
    public Vector2Int PlayerChunkCoordinates => lastPlayerChunkCoords;
    public float ChunkSize => chunkSize;
    }
