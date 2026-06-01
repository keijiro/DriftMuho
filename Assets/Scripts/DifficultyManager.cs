using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    private static DifficultyManager instance;
    public static DifficultyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DifficultyManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DifficultyManager");
                    instance = go.AddComponent<DifficultyManager>();
                }
            }
            return instance;
        }
    }

    [Header("Difficulty Settings")]
    [Tooltip("How long in seconds to reach the maximum value on the curve.")]
    [SerializeField] private float targetDuration = 300f; // 5 minutes (300 seconds)

    [Tooltip("Difficulty multiplier curve over normalized time (0 to 1). Evaluated value scales parameters.")]
    [SerializeField] private AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.8f); // Starts at 1.0x, reaches 1.8x at 5 mins

    private float elapsedTime = 0f;
    private int defeatedEnemiesCount = 0;

    public float ElapsedTime => elapsedTime;
    public float TargetDuration => targetDuration;
    public int DefeatedEnemiesCount => defeatedEnemiesCount;

    public void IncrementDefeatedEnemies()
    {
        defeatedEnemiesCount++;
    }

    /// <summary>
    /// Gets the current difficulty factor based on elapsed time.
    /// Starts at 1.0 and increases up to the max curve value.
    /// </summary>
    public float CurrentDifficultyFactor
    {
        get
        {
            if (targetDuration <= 0f) return 1f;
            float normalizedTime = Mathf.Clamp01(elapsedTime / targetDuration);
            return difficultyCurve.Evaluate(normalizedTime);
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Only increase difficulty when player is active and alive
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("PlayerCar");

        if (player != null)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && !health.IsDead)
            {
                elapsedTime += Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// Explicitly resets the difficulty timer and defeat counter (useful when loading a game or scene).
    /// </summary>
    public void ResetDifficulty()
    {
        elapsedTime = 0f;
        defeatedEnemiesCount = 0;
    }
}
