using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(ARPlane))]
public class ARPlaneMazeDrawer : MonoBehaviour
{
    public float gridStep = 0.05f;
    private ARPlane arPlane;
    public GameObject visualizerInstance;

    public Color lineColor = Color.green;
    public float lineWidth = 0.02f;
    public GameObject mazeControllerPrefab;
    private LineRenderer lineRenderer;
    public GameObject textObj;
    public GameObject buttonObj;
    private int mazeWidth;
    private int mazeHeight;
    private ARPlaneManager planeManager;

    private void Awake()
    {
        planeManager = FindObjectOfType<ARPlaneManager>();

        arPlane = GetComponent<ARPlane>();
        lineRenderer = visualizerInstance.GetComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = lineColor;
        lineRenderer.startWidth = lineRenderer.endWidth = lineWidth;
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;
        buttonObj.GetComponent<Button>().onClick.AddListener(OnStartButtonClicked);
        buttonObj.SetActive(false);
    }

    private void OnStartButtonClicked()
    {
        if (mazeControllerPrefab == null)
        {
            Debug.LogError("MazeController Prefab не назначен в LargestRectVisualizer!");
            return;
        }

        GameObject mazeInstance = Instantiate(mazeControllerPrefab, new Vector3(0f, 0.005f, 0f), Quaternion.Euler(90f, 0f, 90f));
        mazeInstance.transform.SetParent(transform, false);
        mazeInstance.transform.localScale = Vector3.one * gridStep;
        MazeController controller = mazeInstance.GetComponent<MazeController>();

        controller?.Initialize(mazeWidth, mazeHeight);
        if (planeManager) planeManager.enabled = false;

        Hide();
    }

    public void Update()
    {
        if (planeManager && !planeManager.enabled) Hide();
    }

    public void Hide()
    {
        if (buttonObj != null) buttonObj.SetActive(false);
        visualizerInstance.SetActive(false);
    }

    public void Show(Vector3[] corners)
    {
        lineRenderer.positionCount = corners.Length;
        lineRenderer.SetPositions(corners);
        textObj.GetComponent<TextMeshProUGUI>().text = $"{mazeWidth} × {mazeHeight}";

        buttonObj.SetActive(true);
        visualizerInstance.SetActive(true);
    }

    private void OnEnable() => arPlane.boundaryChanged += OnBoundaryChanged;
    private void OnDisable() => arPlane.boundaryChanged -= OnBoundaryChanged;

    private void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs args) => UpdateVisualization();

    private void UpdateVisualization()
    {
        var boundary = arPlane.boundary;
        if (!boundary.IsCreated || boundary.Length < 3)
        {
            Hide();
            return;
        }

        var rect = FindLargestAxisAlignedRectangle(arPlane, gridStep);

        if (rect.width * rect.height < 0.01f)
        {
            Hide();
            return;
        }

        Vector3[] corners = new Vector3[5];
        corners[0] = arPlane.transform.TransformPoint(rect.xMin, 0f, rect.yMax);
        corners[1] = arPlane.transform.TransformPoint(rect.xMax, 0f, rect.yMax);
        corners[2] = arPlane.transform.TransformPoint(rect.xMax, 0f, rect.yMin);
        corners[3] = arPlane.transform.TransformPoint(rect.xMin, 0f, rect.yMin);
        corners[4] = corners[0];

        mazeWidth = Mathf.RoundToInt(rect.width / gridStep);
        mazeHeight = Mathf.RoundToInt(rect.height / gridStep);

        Show(corners);
    }

    private Rect FindLargestAxisAlignedRectangle(ARPlane plane, float step)
    {
        Vector2 size = plane.size;
        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;

        int w = Mathf.CeilToInt(size.x / step) + 1;
        int h = Mathf.CeilToInt(size.y / step) + 1;

        bool[,] grid = new bool[w, h];

        for (int x = 0; x < w; x++)
        {
            float lx = -hx + x * step;
            for (int y = 0; y < h; y++)
            {
                float ly = -hy + y * step;
                grid[x, y] = PointInPolygon(new Vector2(lx, ly), plane.boundary);
            }
        }

        int maxArea = 0;
        int bl = 0, br = 0, bt = 0, bb = 0;
        int[] height = new int[w];
        int[] left = new int[w];
        int[] right = new int[w];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
                height[x] = grid[x, y] ? height[x] + 1 : 0;

            // left
            Stack<int> s = new Stack<int>();
            for (int x = 0; x < w; x++)
            {
                while (s.Count > 0 && height[s.Peek()] >= height[x]) s.Pop();
                left[x] = s.Count == 0 ? -1 : s.Peek();
                s.Push(x);
            }

            // right
            s = new Stack<int>();
            for (int x = w - 1; x >= 0; x--)
            {
                while (s.Count > 0 && height[s.Peek()] >= height[x]) s.Pop();
                right[x] = s.Count == 0 ? w : s.Peek();
                s.Push(x);
            }

            for (int x = 0; x < w; x++)
            {
                if (height[x] == 0) continue;
                int area = (right[x] - left[x] - 1) * height[x];
                if (area > maxArea)
                {
                    maxArea = area;
                    bl = left[x] + 1;
                    br = right[x] - 1;
                    bt = y - height[x] + 1;
                    bb = y;
                }
            }
        }

        if (maxArea == 0) return Rect.zero;

        float xMin = -hx + bl * step;
        float xMax = -hx + (br + 1) * step;
        float yMin = -hy + bt * step;
        float yMax = -hy + (bb + 1) * step;

        Rect rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        return rect;
    }

    private bool PointInPolygon(Vector2 p, NativeArray<Vector2> verts)
    {
        bool inside = false;
        int n = verts.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if ((verts[i].y > p.y) != (verts[j].y > p.y) &&
                p.x < verts[i].x + (verts[j].x - verts[i].x) * (p.y - verts[i].y) / (verts[j].y - verts[i].y + 1e-8f))
            {
                inside = !inside;
            }
        }
        return inside;
    }
}

