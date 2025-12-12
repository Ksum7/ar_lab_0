using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(LineRenderer))]
public class LargestRectVisualizer : MonoBehaviour
{
    [Header("Настройки визуализации")]
    public Color textColor = Color.green;
    public Color lineColor = Color.green;
    public float lineWidth = 0.02f;

    [Header("Префаб лабиринта")]
    public GameObject mazeControllerPrefab;

    private LineRenderer lineRenderer;
    private TextMeshPro areaTextMesh;
    private GameObject textObj;
    private GameObject buttonObj;
    private int width;
    private int height;

    public void Initialize()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = lineColor;
        lineRenderer.startWidth = lineRenderer.endWidth = lineWidth;
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;

        // === Текст с размером ===
        textObj = new GameObject("Area Text");
        textObj.transform.SetParent(transform, false);

        areaTextMesh = textObj.AddComponent<TextMeshPro>();
        areaTextMesh.alignment = TextAlignmentOptions.Center;
        areaTextMesh.color = textColor;
        areaTextMesh.fontSize = 0.15f;
        areaTextMesh.enableAutoSizing = false;

        // === Кнопка "Начать генерацию" ===
        buttonObj = CreateWorldSpaceButton();
        buttonObj.SetActive(false);

        gameObject.SetActive(false);
    }

    private GameObject CreateWorldSpaceButton()
    {
        GameObject buttonGO = new GameObject("StartButton");
        buttonGO.transform.SetParent(textObj.transform, false);

        Canvas canvas = buttonGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        buttonGO.AddComponent<GraphicRaycaster>();

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.6f, 0.1f, 0.95f);

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = bg;

        ColorBlock cb = button.colors;
        cb.normalColor = new Color(0.1f, 0.6f, 0.1f, 0.95f);
        cb.highlightedColor = new Color(0.15f, 0.8f, 0.15f, 1f);
        cb.pressedColor = new Color(0.05f, 0.5f, 0.05f, 1f);
        button.colors = cb;

        GameObject textChild = new GameObject("Text");
        textChild.transform.SetParent(buttonGO.transform, false);

        TextMeshProUGUI buttonText = textChild.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Начать генерацию";
        buttonText.fontSize = 0.12f;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(0.6f, 0.12f);

        RectTransform textRect = textChild.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        buttonRect.anchoredPosition = new Vector2(0, -0.18f);

        button.onClick.AddListener(OnStartButtonClicked);

        return buttonGO;
    }

    public void Show(Vector3[] corners, int width, int height)
    {
        this.width = width;
        this.height = height;
        lineRenderer.positionCount = corners.Length;
        lineRenderer.SetPositions(corners);

        areaTextMesh.text = $"{width} × {height}";

        buttonObj.SetActive(true);
        gameObject.SetActive(true);
    }

    private void OnStartButtonClicked()
    {
        if (mazeControllerPrefab == null)
        {
            Debug.LogError("MazeController Prefab не назначен в LargestRectVisualizer!");
            return;
        }

        GameObject mazeInstance = Instantiate(mazeControllerPrefab);
        mazeInstance.transform.SetParent(transform, false);
        MazeController controller = mazeInstance.GetComponent<MazeController>();

        controller?.Initialize(width, height);

        Hide();
    }

    public void Hide()
    {
        if (buttonObj != null) buttonObj.SetActive(false);
        gameObject.SetActive(false);
    }
}