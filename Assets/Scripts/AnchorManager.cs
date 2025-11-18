using System.Collections.Generic;
using UnityEngine;
using ARLocation;
using UnityEngine.InputSystem;
using System;
using UnityEngine.XR.ARFoundation; // Для ARAnchor
using UnityEngine.XR.ARSubsystems; // Для Pose (если нужно, но теперь не используется)

[System.Serializable]
public class AnchorData
{
    public double latitude;
    public double longitude;
    public double altitude;
    public string name;
    public long timestamp;
}

[System.Serializable]
public class SerializableList { public List<AnchorData> list = new List<AnchorData>(); }

public class AnchorManager : MonoBehaviour
{
    public GameObject anchorPrefab;
    public int maxSavedAnchors = 10;
    private List<AnchorData> anchors = new List<AnchorData>();
    private PlaceAtLocation.PlaceAtOptions defaultOptions;

    void Start()
    {
        defaultOptions = new PlaceAtLocation.PlaceAtOptions
        {
            HideObjectUntilItIsPlaced = true,
            MaxNumberOfLocationUpdates = 1,
            MovementSmoothing = 0.1f,
            UseMovingAverage = true,
        };

        LoadAnchors();
        Debug.Log($"[AnchorManager] Start: Загружено {anchors.Count} сохранённых меток.");
        SpawnSavedAnchors();
        Debug.Log($"[AnchorManager] Start: Спавн завершён. Всего активных: {transform.root.GetComponentsInChildren<PlaceAtLocation>().Length}");
    }

    public void AddCurrentPositionAnchor(string name = "Anchor")
    {

        if (ARLocationProvider.Instance == null)
        {
            return;
        }

        var loc = ARLocationProvider.Instance.CurrentLocation;
        var data = new AnchorData
        {
            latitude = loc.latitude,
            longitude = loc.longitude,
            altitude = loc.altitude,
            name = name,
            timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        anchors.Add(data);
        anchors.Sort((a, b) => b.timestamp.CompareTo(a.timestamp));
        if (anchors.Count > maxSavedAnchors)
        {
            var removed = anchors[maxSavedAnchors];
            anchors.RemoveAt(maxSavedAnchors);
        }
        SaveAnchors();
        SpawnAnchor(data);
    }

    void SpawnSavedAnchors()
    {
        foreach (var a in anchors)
        {
            SpawnAnchor(a);
        }
    }

    GameObject SpawnAnchor(AnchorData data)
    {
        var loc = new Location(data.latitude, data.longitude, data.altitude);

        var instance = PlaceAtLocation.CreatePlacedInstance(anchorPrefab, loc, defaultOptions);
        instance.SetActive(true);
        instance.name = data.name;

        instance.AddComponent<ARAnchor>();

        return instance;
    }

    void SaveAnchors()
    {
        var serial = new SerializableList { list = anchors };
        string json = JsonUtility.ToJson(serial, true);
        PlayerPrefs.SetString("saved_anchors", json);
        PlayerPrefs.Save();
    }

    void LoadAnchors()
    {

        if (PlayerPrefs.HasKey("saved_anchors"))
        {
            string json = PlayerPrefs.GetString("saved_anchors");
            anchors = JsonUtility.FromJson<SerializableList>(json).list;
        }
    }

    void Update()
    {
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.touches[0];
            if (touch.press.wasPressedThisFrame)
            {
                AddCurrentPositionAnchor("Anchor " + System.DateTime.Now.ToShortTimeString());
            }
        }
    }
}