using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private Button changeColorButton;
    [SerializeField] private Button deleteButton;

    [SerializeField] private LayerMask cubeLayerMask = -1;

    private XROrigin xrOrigin;
    private ARCube currentSelectedCube;
    private Camera arCamera;
    private ARPlaneManager planeManager;

    void Start()
    {
        xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("XROrigin not found!");
            return;
        }

        arCamera = xrOrigin.Camera;
        if (arCamera == null || arCamera.tag != "MainCamera")
        {
            Debug.LogError("AR Camera not found or not tagged 'MainCamera'!");
            return;
        }

        planeManager = GetComponent<ARPlaneManager>();
        if (planeManager == null)
        {
            Debug.LogError("ARPlaneManager not found! Add to XROrigin.");
            return;
        }

        if (raycastManager == null)
        {
            Debug.LogError("ARRaycastManager not assigned!");
            return;
        }

        if (changeColorButton != null)
            changeColorButton.onClick.AddListener(ChangeSelectedCubeColor);
        if (deleteButton != null)
            deleteButton.onClick.AddListener(DeleteSelectedCube);

        if (changeColorButton != null)
            changeColorButton.gameObject.SetActive(false);
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(false);

        Debug.Log("SpawnManager started. Planes enabled: " + planeManager.enabled);
    }

    void Update()
    {
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.touches[0];
            if (touch.press.wasPressedThisFrame)
            {
                Vector2 inputPos = touch.position.ReadValue();
                Debug.Log("Touch input detected at: " + inputPos);

                if (IsTouchOverUI(inputPos))
                {
                    Debug.Log("Touch over UI - ignored.");
                    return;
                }

                Debug.Log("Planes count: " + planeManager.trackables.count);

                Ray ray = arCamera.ScreenPointToRay(inputPos);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, cubeLayerMask))
                {
                    ARCube hitCube = hit.collider.GetComponent<ARCube>();
                    if (hitCube != null)
                    {
                        Debug.Log("Cube selected: " + hitCube.name);
                        SelectCube(hitCube);
                        return;
                    }
                }

                var planeHits = new List<ARRaycastHit>();
                bool hitPlane = raycastManager.Raycast(inputPos, planeHits, TrackableType.PlaneWithinPolygon);
                Debug.Log("AR Raycast hit plane? " + hitPlane + " (hits: " + planeHits.Count + ")");

                if (hitPlane && planeHits.Count > 0)
                {
                    Pose pose = planeHits[0].pose;
                    GameObject newCube = Instantiate(cubePrefab, pose.position, pose.rotation, xrOrigin.transform);
                    Debug.Log("Cube spawned at: " + pose.position);
                    DeselectCube();
                    return;
                }

                Debug.Log("No hit - deselect.");
                DeselectCube();
            }
        }
    }

    private bool IsTouchOverUI(Vector2 touchPos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current)
        {
            position = touchPos
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    private void SelectCube(ARCube cube)
    {
        currentSelectedCube = cube;
        if (changeColorButton != null)
        {
            changeColorButton.gameObject.SetActive(true);
            changeColorButton.interactable = true;
        }
        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(true);
            deleteButton.interactable = true;
        }
        Debug.Log("Куб выбран!");
    }

    private void DeselectCube()
    {
        currentSelectedCube = null;
        if (changeColorButton != null)
            changeColorButton.gameObject.SetActive(false);
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(false);
        Debug.Log("Выбор сброшен.");
    }

    private void ChangeSelectedCubeColor()
    {
        if (currentSelectedCube != null)
        {
            currentSelectedCube.ChangeColor();
            Debug.Log("Цвет куба изменён!");
        }
    }

    private void DeleteSelectedCube()
    {
        if (currentSelectedCube != null)
        {
            Destroy(currentSelectedCube.gameObject);
            currentSelectedCube = null;
            if (changeColorButton != null)
                changeColorButton.gameObject.SetActive(false);
            if (deleteButton != null)
                deleteButton.gameObject.SetActive(false);
            Debug.Log("Куб удалён!");
        }
    }
}