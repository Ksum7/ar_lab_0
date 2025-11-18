using UnityEngine;
using ARLocation;

public class DistanceCulling : MonoBehaviour
{
    public float maxDistance = 200f;
    private PlaceAtLocation placeAt;

    void Start()
    {
        placeAt = GetComponent<PlaceAtLocation>();
        if (placeAt == null)
        {
            return;
        }
    }

    void Update()
    {
        if (placeAt == null) return;

        float dist = placeAt.SceneDistance;
        bool shouldBeVisible = dist <= maxDistance;

        gameObject.SetActive(shouldBeVisible);
        Vector3 objWorldPos = transform.position;
        Vector3 camWorldPos = Camera.main.transform.position;

        transform.position = new Vector3(objWorldPos.x, camWorldPos.y - 10, objWorldPos.z);
    }
}