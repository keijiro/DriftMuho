using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(UIDocument))]
public class VehicleHUD : MonoBehaviour
{
    private Rigidbody carRigidbody;
    private OffroadCarController carController;
    private PlayerHealth playerHealth;
    
    private Label speedValueLabel;
    private RadialGauge energyGauge;
    private Label energyText;
    private VisualElement gameOverScreen;
    private VisualElement lockOnCursor;
    private Label defeatedValueLabel;
    private Label gameOverScoreValueLabel;
    private VisualElement transitionOverlay;
    private VisualElement titleImage;
    private GameObject playerGameObjectCached;

    private void Start()
    {
        // 1. Get the UIDocument and query elements
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            speedValueLabel = uiDocument.rootVisualElement.Q<Label>("speedValue");
            energyGauge = uiDocument.rootVisualElement.Q<RadialGauge>("energyGauge");
            energyText = uiDocument.rootVisualElement.Q<Label>("energyText");
            gameOverScreen = uiDocument.rootVisualElement.Q<VisualElement>("gameOverScreen");
            lockOnCursor = uiDocument.rootVisualElement.Q<VisualElement>("lockOnCursor");
            defeatedValueLabel = uiDocument.rootVisualElement.Q<Label>("defeatedValue");
            gameOverScoreValueLabel = uiDocument.rootVisualElement.Q<Label>("gameOverScoreValue");
            transitionOverlay = uiDocument.rootVisualElement.Q<VisualElement>("transitionOverlay");
            titleImage = uiDocument.rootVisualElement.Q<VisualElement>("titleImage");
        }

        // 2. Find the Player Car and cache components
        FindPlayerComponents();

        // 3. Start the intro transition
        StartCoroutine(PlayIntroTransition());
    }

    private void Update()
    {
        // Try to re-detect player if lost or not found initially
        if (carRigidbody == null)
        {
            FindPlayerComponents();
            if (speedValueLabel != null) speedValueLabel.text = "0";
            if (energyGauge != null) energyGauge.Value = 0f;
            if (energyText != null) energyText.text = "0%";
            if (gameOverScreen != null) gameOverScreen.style.display = DisplayStyle.None;
            if (lockOnCursor != null) lockOnCursor.style.display = DisplayStyle.None;
            return;
        }

        if (carController == null && carRigidbody != null)
        {
            carController = carRigidbody.GetComponent<OffroadCarController>();
        }
        if (playerHealth == null && carRigidbody != null)
        {
            playerHealth = carRigidbody.GetComponent<PlayerHealth>();
        }

        // 3. Update Lock-On Cursor overlay
        if (lockOnCursor != null)
        {
            if (carController != null && carController.lockedTarget != null && !carController.lockedTarget.IsDead)
            {
                // Align target position (slightly above target center)
                Vector3 targetWorldPos = carController.lockedTarget.transform.position;
                
                Vector3 screenPos = Camera.main.WorldToScreenPoint(targetWorldPos);
                if (screenPos.z > 0f)
                {
                    lockOnCursor.style.display = DisplayStyle.Flex;
                    Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(lockOnCursor.panel, targetWorldPos, Camera.main);
                    lockOnCursor.style.left = panelPos.x;
                    lockOnCursor.style.top = panelPos.y;
                }
                else
                {
                    lockOnCursor.style.display = DisplayStyle.None;
                }
            }
            else
            {
                lockOnCursor.style.display = DisplayStyle.None;
            }
        }

        // 4. Update Energy (Health) display
        if (playerHealth != null)
        {
            float hpPct = (playerHealth.CurrentHealth / playerHealth.MaxHealth) * 100f;
            if (energyGauge != null)
            {
                energyGauge.Value = playerHealth.CurrentHealth / playerHealth.MaxHealth;
            }
            if (energyText != null)
            {
                energyText.text = $"{Mathf.RoundToInt(hpPct)}%";
            }

            // Show or hide Game Over overlay
            if (gameOverScreen != null)
            {
                bool isDead = playerHealth.IsDead;
                gameOverScreen.style.display = isDead ? DisplayStyle.Flex : DisplayStyle.None;

                // Sync the high score when the gameover panel becomes active
                if (isDead && gameOverScoreValueLabel != null && DifficultyManager.Instance != null)
                {
                    gameOverScoreValueLabel.text = DifficultyManager.Instance.DefeatedEnemiesCount.ToString();
                }
            }
            }

        // 5. Update Defeated Enemies (Kills) Counter
        if (defeatedValueLabel != null && DifficultyManager.Instance != null)
        {
            defeatedValueLabel.text = DifficultyManager.Instance.DefeatedEnemiesCount.ToString();
        }

        if (speedValueLabel == null) return;

        // 4. Compute current absolute movement velocity in meters per second (includes sideways sliding/drifting)
        float speedMPS = carRigidbody.linearVelocity.magnitude;

        // 5. Convert meters per second to kilometers per hour (1 m/s = 3.6 km/h)
        float speedKMH = Mathf.Max(0f, speedMPS * 3.6f);

        // 6. Update the UI text
        speedValueLabel.text = Mathf.RoundToInt(speedKMH).ToString();
        }

    private void FindPlayerComponents()
    {
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
        {
            playerGO = GameObject.Find("PlayerCar");
        }

        if (playerGO != null)
        {
            playerGameObjectCached = playerGO;
            carRigidbody = playerGO.GetComponent<Rigidbody>();
            carController = playerGO.GetComponent<OffroadCarController>();
            playerHealth = playerGO.GetComponent<PlayerHealth>();
        }
    }

    private IEnumerator PlayIntroTransition()
    {
        if (transitionOverlay == null || titleImage == null) yield break;

        // Play the title jingle sound
        SoundManager.PlayTitleJingle();

        // Temporarily disable the player car to prevent early sound, movement, or UI activation
        if (playerGameObjectCached != null)
        {
            playerGameObjectCached.SetActive(false);
        }

        // Ensure visible at start
        transitionOverlay.style.display = DisplayStyle.Flex;
        transitionOverlay.style.opacity = 1f;

        // Scale prep
        titleImage.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));
        titleImage.style.scale = new Scale(new Vector3(3f, 3f, 1f));
        titleImage.style.opacity = 0f;

        // Slam Down (0.15 seconds)
        float slamDuration = 0.15f;
        float time = 0f;
        while (time < slamDuration)
        {
            time += Time.deltaTime;
            float t = time / slamDuration;
            float currentScale = Mathf.Lerp(3f, 1f, t * t); // Ease-In slam
            float currentOpacity = Mathf.Lerp(0f, 1f, t);
            titleImage.style.scale = new Scale(new Vector3(currentScale, currentScale, 1f));
            titleImage.style.opacity = currentOpacity;
            yield return null;
        }

        titleImage.style.scale = new Scale(Vector3.one);
        titleImage.style.opacity = 1f;

        // Shake & Shudder (0.35 seconds)
        float shakeDuration = 0.35f;
        float shakeTime = 0f;
        while (shakeTime < shakeDuration)
        {
            shakeTime += Time.deltaTime;
            float jitterX = Random.Range(-12f, 12f);
            float jitterY = Random.Range(-12f, 12f);
            titleImage.style.translate = new Translate(jitterX, jitterY, 0f);
            yield return null;
        }
        titleImage.style.translate = new Translate(0f, 0f, 0f);

        // --- ADDED: A satisfying pause/anticipation hold (0.50 seconds) before fading out ---
        yield return new WaitForSeconds(0.50f);

        // Reactivate player car right as fade-out begins
        if (playerGameObjectCached != null)
        {
            playerGameObjectCached.SetActive(true);

            // Re-bind references cleanly after reactivation
            carRigidbody = playerGameObjectCached.GetComponent<Rigidbody>();
            carController = playerGameObjectCached.GetComponent<OffroadCarController>();
            playerHealth = playerGameObjectCached.GetComponent<PlayerHealth>();
        }

        // Fade Out the entire overlay (0.5 seconds)
        float fadeDuration = 0.5f;
        float fadeTime = 0f;
        while (fadeTime < fadeDuration)
        {
            fadeTime += Time.deltaTime;
            float t = fadeTime / fadeDuration;
            float currentOpacity = Mathf.Lerp(1f, 0f, t);
            transitionOverlay.style.opacity = currentOpacity;
            yield return null;
        }

        transitionOverlay.style.opacity = 0f;
        transitionOverlay.style.display = DisplayStyle.None;
    }

    public void StartRestartTransition()
    {
        StartCoroutine(RestartTransitionCoroutine());
    }

    private IEnumerator RestartTransitionCoroutine()
    {
        if (transitionOverlay == null)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            yield break;
        }

        // Hide title image on restart fade-in
        if (titleImage != null)
        {
            titleImage.style.opacity = 0f;
        }

        transitionOverlay.style.opacity = 0f;
        transitionOverlay.style.display = DisplayStyle.Flex;

        float fadeDuration = 0.5f;
        float fadeTime = 0f;
        while (fadeTime < fadeDuration)
        {
            fadeTime += Time.deltaTime;
            float t = fadeTime / fadeDuration;
            float currentOpacity = Mathf.Lerp(0f, 1f, t);
            transitionOverlay.style.opacity = currentOpacity;
            yield return null;
        }

        transitionOverlay.style.opacity = 1f;

        // Load scene
        Debug.Log("Outro transition complete. Reloading scene...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    }
