using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ARLocation;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ARLocationProvider))]
public class AnchorManager : MonoBehaviour
{
    public GameObject anchorPrefab;
    [Min(1)] public int maxSavedAnchors = 10;
    [Range(0f, 1f)] public float defaultSmoothing = 0.1f;

    public TMP_Text logText;
    public Image compassArrow;
    public GameObject anchorUIPrefab;
    public GameObject infoPopup;
    public TMP_Text infoPopupText;

    private List<AnchorData> anchors = new List<AnchorData>();
    private readonly string PlayerPrefsKey = "saved_anchors_v3";
    private ARLocationProvider locationProvider;
    private PlaceAtLocation.PlaceAtOptions placeOptions;
    private Camera arCamera;

    private readonly Dictionary<GameObject, GameObject> anchorToUI = new();

    private void Awake()
    {
        locationProvider = ARLocationProvider.Instance;
        arCamera = Camera.main;

        if (!locationProvider) { Log("ARLocationProvider не найден!"); enabled = false; return; }
        if (!arCamera) { Log("Главная камера не найдена!"); enabled = false; return; }

        InitializePlaceOptions();
        SetupUI();
    }

    private void Start()
    {
        Log("Инициализация GPS...");
        LoadAnchors();

        if (anchors.Count > 0)
        {
            Log($"Загружено якорей: {anchors.Count}. Ожидание GPS...");
        }
        else
        {
            Log("Нет сохранённых якорей. Коснитесь экрана, чтобы добавить.");
        }

        SpawnSavedAnchors();
    }

    private void SpawnSavedAnchors()
    {
        foreach (var a in anchors)
            SpawnAnchor(a);
    }

    private void InitializePlaceOptions()
    {
        placeOptions = new PlaceAtLocation.PlaceAtOptions
        {
            HideObjectUntilItIsPlaced = true,
            MaxNumberOfLocationUpdates = 2,
            MovementSmoothing = defaultSmoothing,
            UseMovingAverage = true,
            ShowObjectAfterThisManyUpdates = 0
        };
    }

    private void SetupUI()
    {
        if (compassArrow) compassArrow.gameObject.SetActive(false);
        if (infoPopup) infoPopup.SetActive(false);
    }

    private void Update()
    {
        HandleTouchInput();
        UpdateCompassArrow();
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        foreach (var touch in Touchscreen.current.touches)
        {
            if (touch.press.wasPressedThisFrame)
            {
                int touchId = touch.touchId.ReadValue();

                if (EventSystem.current.IsPointerOverGameObject(touchId))
                    return;

                AddCurrentPositionAnchor($"Якорь {DateTime.Now:HH:mm:ss}");
                break;
            }
        }
    }

    public void AddCurrentPositionAnchor(string name = "Якорь")
    {
        if (!IsLocationReady())
        {
            Log("GPS ещё не готов...");
            return;
        }

        var loc = locationProvider.CurrentLocation;
        var data = new AnchorData
        {
            latitude = loc.latitude,
            longitude = loc.longitude,
            altitude = 0f,
            name = string.IsNullOrWhiteSpace(name) ? "Якорь" : name.Trim(),
            timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        anchors.Add(data);
        anchors = anchors.OrderByDescending(a => a.timestamp).Take(maxSavedAnchors).ToList();

        SaveAnchors();
        var go = SpawnAnchor(data);
        Log($"Добавлен: {data.name}");

        if (go) ShowAnchorUI(go, data);
    }

    private GameObject SpawnAnchor(AnchorData data)
    {
        if (!anchorPrefab) { Log("anchorPrefab не назначен!"); return null; }

        var location = new Location(data.latitude, data.longitude, 0f) { AltitudeMode = AltitudeMode.GroundRelative };
        var instance = PlaceAtLocation.CreatePlacedInstance(anchorPrefab, location, placeOptions);
        instance.name = data.name;

        var gh = instance.GetComponent<GroundHeight>();
        if (gh)
        {
            gh.Settings.Altitude = 0f;
            gh.Settings.DisableUpdate = false;
            gh.Settings.Smoothing = defaultSmoothing;
            gh.Settings.Precision = 0.01f;
            gh.UpdateObjectHeight(true);
        }

        if (anchorUIPrefab)
        {
            var ui = Instantiate(anchorUIPrefab, FindObjectOfType<Canvas>().transform);
            ui.SetActive(false);
            anchorToUI[instance] = ui;

            var infoBtn = ui.transform.Find("Button_Info")?.GetComponent<Button>();
            var deleteBtn = ui.transform.Find("Button_Delete")?.GetComponent<Button>();

            if (infoBtn) infoBtn.onClick.RemoveAllListeners();
            if (deleteBtn) deleteBtn.onClick.RemoveAllListeners();

            if (infoBtn) infoBtn.onClick.AddListener(() => ShowInfoPopup(data));
            if (deleteBtn) deleteBtn.onClick.AddListener(() => DeleteAnchor(instance, data));
        }
        data.gameObject = instance;
        return instance;
    }

    private void ShowAnchorUI(GameObject anchorGo, AnchorData data)
    {
        if (!anchorToUI.TryGetValue(anchorGo, out var ui) || !ui) return;

        var screenPos = arCamera.WorldToScreenPoint(anchorGo.transform.position);
        bool isVisible = screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width && screenPos.y > 0 && screenPos.y < Screen.height;

        ui.SetActive(isVisible);

        if (isVisible)
        {
            var rect = ui.GetComponent<RectTransform>();
            rect.anchoredPosition = screenPos - new Vector3(Screen.width / 2f, Screen.height / 2f);
        }
    }

    private void UpdateCompassArrow()
    {
        if (anchors.Count == 0 || !IsLocationReady() || compassArrow == null || arCamera == null)
        {
            compassArrow.gameObject.SetActive(false);
            return;
        }

        var currentLocation = locationProvider.CurrentLocation.ToLocation();
        var cameraPos = arCamera.transform.position;

        GameObject nearestAnchorGo = null;
        float minDistanceSqr = float.MaxValue;
        bool anyVisible = false;

        foreach (var anchorData in anchors)
        {
            if (anchorData.gameObject == null) continue;

            Vector3 worldPos = anchorData.gameObject.transform.position;

            float distSqr = (worldPos - cameraPos).sqrMagnitude;

            Vector3 screenPos = arCamera.WorldToScreenPoint(worldPos);
            bool isOnScreen = screenPos.z > 0 &&
                            screenPos.x >= 0 && screenPos.x <= Screen.width &&
                            screenPos.y >= 0 && screenPos.y <= Screen.height;

            if (isOnScreen)
                anyVisible = true;

            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                nearestAnchorGo = anchorToUI.Keys.FirstOrDefault(go => go.name == anchorData.name);
            }
        }

        if (anyVisible)
        {
            compassArrow.gameObject.SetActive(false);
            return;
        }


        if (nearestAnchorGo != null)
        {
            PointCompassArrowAt(nearestAnchorGo.transform.position);
            compassArrow.gameObject.SetActive(true);
        }
        else
        {
            compassArrow.gameObject.SetActive(false);
        }
    }

    private void PointCompassArrowAt(Vector3 worldTarget)
    {
        Vector3 screenPoint = arCamera.WorldToScreenPoint(worldTarget);

        if (screenPoint.z < 0)
            screenPoint *= -1;

        Vector2 direction = new Vector2(
            screenPoint.x - Screen.width / 2f,
            screenPoint.y - Screen.height / 2f
        );

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        compassArrow.rectTransform.rotation = Quaternion.Euler(0, 0, angle);
        compassArrow.rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void LateUpdate()
    {
        foreach (var pair in anchorToUI)
        {
            if (pair.Key != null) ShowAnchorUI(pair.Key, GetAnchorData(pair.Key));
        }
    }

    private AnchorData GetAnchorData(GameObject go)
    {
        return anchors.FirstOrDefault(a => a.name == go.name);
    }

    private void DeleteAnchor(GameObject go, AnchorData data)
    {
        anchors.RemoveAll(a => a.timestamp == data.timestamp);
        SaveAnchors();
        if (anchorToUI.TryGetValue(go, out var ui) && ui) Destroy(ui);
        anchorToUI.Remove(go);
        Destroy(go);
        Log($"Удалён якорь: {data.name}");
    }

    private void ShowInfoPopup(AnchorData data)
    {
        if (!infoPopup || !infoPopupText) return;

        var time = DateTimeOffset.FromUnixTimeSeconds(data.timestamp).ToLocalTime();
        infoPopupText.text =
            $"<b>{data.name}</b>\n" +
            $"Широта: {data.latitude:F6}\n" +
            $"Долгота: {data.longitude:F6}\n" +
            $"Время: {time:dd.MM.yyyy HH:mm:ss}";

        infoPopup.SetActive(true);
    }

    private bool IsLocationReady()
    {
        return locationProvider?.IsEnabled == true;
    }

    private void Log(string message)
    {
        Debug.Log($"[Anchor] {message}");
        if (logText) logText.text = message;
    }
    private void SaveAnchors()
    {
        var wrapper = new SerializableList { list = anchors };
        PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(wrapper, true));
        PlayerPrefs.Save();
    }

    private void LoadAnchors()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            anchors = new List<AnchorData>();
            return;
        }

        try
        {
            var wrapper = JsonUtility.FromJson<SerializableList>(PlayerPrefs.GetString(PlayerPrefsKey));
            anchors = wrapper?.list ?? new List<AnchorData>();
            anchors = anchors.Where(a => a.latitude != 0 || a.longitude != 0)
                             .OrderByDescending(a => a.timestamp)
                             .Take(maxSavedAnchors).ToList();
        }
        catch (Exception e)
        {
            Debug.LogError("Ошибка загрузки якорей: " + e.Message);
            anchors = new List<AnchorData>();
        }
    }
}

[System.Serializable] public class AnchorData { public double latitude, longitude; public float altitude = 0f; public string name = "Якорь"; public long timestamp; [System.NonSerialized] public GameObject gameObject = null; }
[System.Serializable] public class SerializableList { public List<AnchorData> list = new List<AnchorData>(); }