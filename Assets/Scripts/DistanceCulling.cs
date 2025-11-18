using UnityEngine;
using ARLocation;
using UnityEngine.Events;

public class DistanceCulling : MonoBehaviour
{
    [Header("Culling Settings")]
    public float maxDistance = 200f; // метров, в пределах которых метка видима

    private PlaceAtLocation placeAt;
    private bool isVisible = true;

    void Start()
    {
        placeAt = GetComponent<PlaceAtLocation>();
        if (placeAt == null)
        {
            Debug.LogError("[DistanceCulling] PlaceAtLocation component not found on " + gameObject.name);
            return;
        }

        placeAt.ObjectPositionUpdated.AddListener(OnPositionUpdated);
    }

    void OnDestroy()
    {
        if (placeAt != null)
        {
            placeAt.ObjectPositionUpdated.RemoveListener(OnPositionUpdated);
        }
    }

    private void OnPositionUpdated(GameObject go, Location loc, int updates)
    {
        if (placeAt == null) return;

        float dist = placeAt.SceneDistance;

        bool shouldBeVisible = dist <= maxDistance;
        if (shouldBeVisible != isVisible)
        {
            gameObject.SetActive(shouldBeVisible);
            isVisible = shouldBeVisible;
            Debug.Log($"[DistanceCulling] {gameObject.name} visibility changed to {shouldBeVisible} (dist: {dist:F1}m)");
        }
    }
}