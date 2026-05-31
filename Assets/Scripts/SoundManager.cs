using UnityEngine;

#pragma warning disable 0649
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioClip enemyShot;
    [SerializeField] private AudioClip gunHit;
    [SerializeField] private AudioClip gunShot;
    [SerializeField] private AudioClip playerDamage;

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
        if (Instance != null) PlayClip(Instance.enemyShot, position, 0.7f);
    }

    public static void PlayGunHit(Vector3 position)
    {
        if (Instance != null) PlayClip(Instance.gunHit, position, 0.8f);
    }

    public static void PlayGunShot(Vector3 position)
    {
        if (Instance != null) PlayClip(Instance.gunShot, position, 0.6f);
    }

    public static void PlayPlayerDamage(Vector3 position)
    {
        if (Instance != null) PlayClip(Instance.playerDamage, position, 1.0f);
    }

    private static void PlayClip(AudioClip clip, Vector3 position, float volume)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }
}
#pragma warning restore 0649
