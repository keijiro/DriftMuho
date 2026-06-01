using UnityEngine;

#pragma warning disable 0649
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip enemyShot;
    [SerializeField] private AudioClip gunHit;
    [SerializeField] private AudioClip gunShot;
    [SerializeField] private AudioClip enemyDestroyed;
    [SerializeField] private AudioClip playerDamage;
    [SerializeField] private AudioClip playerDeath;
    [SerializeField] private AudioClip lockOnBeep;

    [Header("Blend Settings")]
    [Range(0f, 1f)] [SerializeField] private float enemyShotSpatialBlend = 0.5f; // 0 = 2D (no fade), 1 = 3D (directional & distance fade)
    [Range(0f, 1f)] [SerializeField] private float gunHitSpatialBlend = 0.6f;    // Blend ratio for bullet impact sound
    [Range(0f, 1f)] [SerializeField] private float enemyDestroyedSpatialBlend = 0.5f; // Blend ratio for enemy destruction sound
    [SerializeField] private float enemyDestroyedPitch = 0.85f;                  // Pitch for enemy destruction sound (slightly lower for extra weight)

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

    public static void PlayEnemyShot(Vector3 position)
    {
        if (Instance != null) 
        {
            PlayCustom3DClip(Instance.enemyShot, position, 1.0f, Instance.enemyShotSpatialBlend);
        }
    }

    public static void PlayGunHit(Vector3 position)
    {
        if (Instance != null) 
        {
            PlayCustom3DClip(Instance.gunHit, position, 1.0f, Instance.gunHitSpatialBlend);
        }
    }

    public static void PlayGunShot(Vector3 position)
    {
        if (Instance != null) 
        {
            PlayDefaultClip(Instance.gunShot, position, 0.9f);
        }
    }

    public static void PlayPlayerDamage(Vector3 position)
    {
        if (Instance != null) 
        {
            PlayDefaultClip(Instance.playerDamage, position, 0.9f);
        }
    }

    public static void PlayPlayerDeath(Vector3 position)
    {
        if (Instance != null)
        {
            PlayCustom3DClip(Instance.playerDeath, position, 1.0f, 0.0f, 0.8f); // 2D at 100% volume, with 0.8 pitch
        }
    }

    public static void PlayEnemyDestroyed(Vector3 position)
    {
        if (Instance != null) 
        {
            PlayCustom3DClip(Instance.enemyDestroyed, position, 1.0f, Instance.enemyDestroyedSpatialBlend, Instance.enemyDestroyedPitch);
        }
    }

    public static void PlayLockOnBeep()
    {
        if (Instance != null)
        {
            PlayCustom3DClip(Instance.lockOnBeep, Vector3.zero, 0.8f, 0.0f); // 2D at 80% volume
        }
    }

    private static void PlayDefaultClip(AudioClip clip, Vector3 position, float volume)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }

    private static void PlayCustom3DClip(AudioClip clip, Vector3 position, float volume, float spatialBlend, float pitch = 1.0f)
    {
        if (clip == null) return;

        GameObject go = new GameObject("OneShotAudio_" + clip.name);
        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = spatialBlend; // Blend: 0.5 means 50% 2D and 50% 3D
        source.pitch = pitch;
        
        source.Play();

        Destroy(go, clip.length / Mathf.Max(0.01f, pitch));
    }
}
#pragma warning restore 0649
