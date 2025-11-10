using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class RotationHandler : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 100f;

    private bool isRotating = false;
    private Vector2 lastTouchPosition;

    void Update()
    {
        if (Touchscreen.current != null && Touchscreen.current.enabled && Touchscreen.current.touches.Count > 0)
        {
            TouchControl touch = Touchscreen.current.touches[0];
            Vector2 currentPosition = touch.position.ReadValue();

            Debug.Log($"Touch detected: Phase={touch.phase.ReadValue()}, Position={currentPosition}");

            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                lastTouchPosition = currentPosition;
                isRotating = true;
                Debug.Log("Rotation started!");
            }
            else if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved && isRotating)
            {
                Vector2 deltaPosition = currentPosition - lastTouchPosition;
                float rotation = deltaPosition.x * rotationSpeed * Time.deltaTime;
                transform.Rotate(0, 0, rotation);
                Debug.Log($"Rotating by {rotation} degrees");
                lastTouchPosition = currentPosition;
            }
            else if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended ||
                     touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                isRotating = false;
                Debug.Log("Rotation ended!");
            }
        }
    }
}