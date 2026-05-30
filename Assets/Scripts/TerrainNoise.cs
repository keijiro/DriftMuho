using UnityEngine;

public static class TerrainNoise
{
    // Configure the scale and feel of the low-poly terrain
    public static float baseScale = 0.008f;        // Frequency of the base hills
    public static float heightMultiplier = 7.0f;    // Maximum height amplitude
    public static int octaves = 3;                  // Number of noise layers (FBM)
    public static float lacunarity = 2.2f;          // Frequency multiplier per octave
    public static float persistence = 0.45f;        // Amplitude multiplier per octave
    public static float floorThreshold = -0.3f;     // Normalized threshold below which terrain is flattened into plains

    /// <summary>
    /// Computes a continuous, seam-free height for any point in world space.
    /// Since it relies strictly on world-space X and Z coordinates, adjacent boundaries will always match.
    /// </summary>
    public static float GetHeight(float x, float z)
    {
        float height = 0f;
        float amplitude = 1f;
        float frequency = baseScale;
        float totalAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            // Apply coordinates offset to avoid Perlin symmetry and artifacts near origin (0,0)
            float sampleX = (x + 25000.123f) * frequency;
            float sampleZ = (z + 25000.123f) * frequency;

            // Mathf.PerlinNoise returns [0..1]
            float noiseValue = Mathf.PerlinNoise(sampleX, sampleZ);

            // Recenter to [-1..1] to allow valleys and peaks around a baseline of 0
            float value = (noiseValue - 0.5f) * 2f;

            height += value * amplitude;
            totalAmplitude += amplitude;

            frequency *= lacunarity;
            amplitude *= persistence;
        }

        // Normalize height so it stays within [-1..1] range before applying multiplier
        if (totalAmplitude > 0f)
        {
            height /= totalAmplitude;
        }

        // Apply flatness threshold (clamp values below threshold to create flat plains/valleys)
        if (height < floorThreshold)
        {
            height = floorThreshold;
        }

        return height * heightMultiplier;
    }
}
