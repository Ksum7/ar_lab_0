using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTrackingHandler : MonoBehaviour
{
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private GameObject arModel;

    private Dictionary<TrackableId, GameObject> spawnedPrefabs = new Dictionary<TrackableId, GameObject>();

    void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        if (arModel != null) arModel.SetActive(false);
    }

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            SpawnModel(trackedImage);
        }

        foreach (var trackedImage in eventArgs.removed)
        {
            DestroySpawnedModel(trackedImage);
        }

        // foreach (var trackedImage in eventArgs.updated)
        // {
        //     UpdateModelPosition(trackedImage);
        // }
    }

    private void SpawnModel(ARTrackedImage trackedImage)
    {
        var imageName = trackedImage.referenceImage.name;
        if (imageName == "img")
        {
            Debug.Log("Found image, model spawned");
            var newModel = Instantiate(arModel, trackedImage.transform);
            newModel.SetActive(true);
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            spawnedPrefabs[trackedImage.trackableId] = newModel;
        }
    }

    private void DestroySpawnedModel(ARTrackedImage trackedImage)
    {
        if (spawnedPrefabs.TryGetValue(trackedImage.trackableId, out var model))
        {
            Debug.Log("Model despawned");
            Destroy(model);
            spawnedPrefabs.Remove(trackedImage.trackableId);
        }
    }

    private void UpdateModelPosition(ARTrackedImage trackedImage)
    {
        if (spawnedPrefabs.TryGetValue(trackedImage.trackableId, out var model))
        {
            model.transform.SetPositionAndRotation(trackedImage.transform.position, trackedImage.transform.rotation);
        }
    }
}