using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class VehicleHUD : MonoBehaviour
{
    private Rigidbody carRigidbody;
    private OffroadCarController carController;
    private Label speedValueLabel;
    private Label lockOnStatusLabel;

    private void Start()
    {
        // 1. Get the UIDocument and query elements
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            speedValueLabel = uiDocument.rootVisualElement.Q<Label>("speedValue");
            lockOnStatusLabel = uiDocument.rootVisualElement.Q<Label>("lockOnStatus");
        }

        // 2. Find the Player Car and cache components
        FindPlayerRigidbody();
    }

    private void Update()
    {
        // Try to re-detect player if lost or not found initially
        if (carRigidbody == null)
        {
            FindPlayerRigidbody();
            if (speedValueLabel != null) speedValueLabel.text = "0";
            if (lockOnStatusLabel != null) lockOnStatusLabel.style.display = DisplayStyle.None;
            return;
        }

        if (carController == null && carRigidbody != null)
        {
            carController = carRigidbody.GetComponent<OffroadCarController>();
        }

        // 3. Update Lock-on HUD status display
        if (lockOnStatusLabel != null)
        {
            bool isLockedOn = carController != null && carController.lockedTarget != null;
            lockOnStatusLabel.style.display = isLockedOn ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (speedValueLabel == null) return;

        // 4. Compute current forward velocity in meters per second
        Vector3 forward = carRigidbody.transform.forward;
        float speedMPS = Vector3.Dot(carRigidbody.linearVelocity, forward);

        // 5. Convert meters per second to kilometers per hour (1 m/s = 3.6 km/h)
        float speedKMH = Mathf.Max(0f, speedMPS * 3.6f);

        // 6. Update the UI text
        speedValueLabel.text = Mathf.RoundToInt(speedKMH).ToString();
    }

    private void FindPlayerRigidbody()
    {
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
        {
            playerGO = GameObject.Find("PlayerCar");
        }

        if (playerGO != null)
        {
            carRigidbody = playerGO.GetComponent<Rigidbody>();
            carController = playerGO.GetComponent<OffroadCarController>();
        }
    }
}
