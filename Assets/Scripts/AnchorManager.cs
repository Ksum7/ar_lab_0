using System.Collections.Generic;
using UnityEngine;
using ARLocation;
using System;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[System.Serializable]
public class AnchorData
{
    public double latitude;
    public double longitude;
    public float altitude = 0f; // Игнорируем raw GPS altitude
    public string name;
    public long timestamp;
}

[System.Serializable]
public class SerializableList { public List<AnchorData> list = new List<AnchorData>(); }

public class AnchorManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject anchorPrefab;
    public int maxSavedAnchors = 10;
    public float defaultSmoothing = 0.1f;

    private List<AnchorData> anchors = new List<AnchorData>();
    private PlaceAtLocation.PlaceAtOptions defaultOptions;
    private ARLocationProvider locationProvider;

    void Start()
    {
        locationProvider = ARLocationProvider.Instance;
        if (locationProvider == null)
        {
            Debug.LogError("[AnchorManager] ARLocationProvider not found!");
            return;
        }

        defaultOptions = new PlaceAtLocation.PlaceAtOptions
        {
            HideObjectUntilItIsPlaced = true,
            MaxNumberOfLocationUpdates = 2,
            MovementSmoothing = defaultSmoothing,
            UseMovingAverage = true,
            ShowObjectAfterThisManyUpdates = 2
        };

        LoadAnchors();
        SpawnSavedAnchors();

        if (locationProvider.IsEnabled)
        {
            locationProvider.ForceLocationUpdate();
        }
    }

    public void AddCurrentPositionAnchor(string name = "Метка")
    {
        if (locationProvider == null || !locationProvider.IsEnabled)
        {
            Debug.LogWarning("[AnchorManager] Location provider not ready!");
            return;
        }

        var loc = locationProvider.CurrentLocation;
        var data = new AnchorData
        {
            latitude = loc.latitude,
            longitude = loc.longitude,
            altitude = 0f,
            name = name,
            timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        anchors.Add(data);
        anchors.Sort((a, b) => b.timestamp.CompareTo(a.timestamp)); // Новые сверху
        if (anchors.Count > maxSavedAnchors) anchors.RemoveAt(anchors.Count - 1);

        SaveAnchors();
        SpawnAnchor(data);
    }

    void SpawnSavedAnchors()
    {
        foreach (var a in anchors)
            SpawnAnchor(a);
    }

    GameObject SpawnAnchor(AnchorData data)
    {
        var loc = new Location(data.latitude, data.longitude, 0f);
        loc.AltitudeMode = AltitudeMode.GroundRelative;

        var instance = PlaceAtLocation.CreatePlacedInstance(anchorPrefab, loc, defaultOptions);

        var gh = instance.GetComponent<GroundHeight>();
        if (gh != null)
        {
            gh.Settings.Altitude = 0f;
            gh.Settings.DisableUpdate = false;
            gh.Settings.Smoothing = defaultSmoothing;
            gh.Settings.Precision = 0.01f;
            gh.UpdateObjectHeight(true);
        }

        instance.name = data.name;
        Debug.Log($"[AnchorManager] Spawned anchor: {data.name} at {loc}");
        return instance;
    }

    void SaveAnchors()
    {
        var serial = new SerializableList { list = anchors };
        string json = JsonUtility.ToJson(serial);
        PlayerPrefs.SetString("saved_anchors", json);
        PlayerPrefs.Save();
        Debug.Log($"[AnchorManager] Saved {anchors.Count} anchors");
    }

    void LoadAnchors()
    {
        if (PlayerPrefs.HasKey("saved_anchors"))
        {
            string json = PlayerPrefs.GetString("saved_anchors");
            anchors = JsonUtility.FromJson<SerializableList>(json).list ?? new List<AnchorData>();
            Debug.Log($"[AnchorManager] Loaded {anchors.Count} anchors");
        }
        else
        {
            anchors = new List<AnchorData>();
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