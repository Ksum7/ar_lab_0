using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class MazeController : MonoBehaviour
{
    [Header("Размеры лабиринта")]
    public int width = 31;
    public int height = 31;

    [Header("Внешний вид")]
    public int cellSize = 32;
    public Color floorColor = new Color(0.95f, 0.95f, 0.95f);
    public Color wallColor = new Color(0.1f, 0.1f, 0.15f);
    public int wallThickness = 6;

    [Header("Игрок")]
    public float moveDuration = 0.2f;

    [Header("Объекты")]
    public GameObject playerPrefab;
    public GameObject exitPrefab;
    public GameObject treasurePrefab;

    private SpriteRenderer mazeRenderer;

    private GameObject playerObject;
    private GameObject exitObject;
    private List<GameObject> treasureObjects = new List<GameObject>();

    private bool[,] visited;
    private bool[,] horizontalWalls;
    private bool[,] verticalWalls;

    private Vector2Int playerCell;
    private Coroutine moveCoroutine;

    private readonly Vector2Int[] directions = {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };
    private int score = 0;
    private GameManager gameManager;

    private ARPlaneManager planeManager;

    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private bool isSwiping = false;
    public float minSwipeDistance = 50f;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        planeManager = FindObjectOfType<ARPlaneManager>();
        gameObject.AddComponent<BoxCollider>();
        BoxCollider collider = GetComponent<BoxCollider>();
        collider.size = new Vector3(width, 0.1f, height);
        collider.center = Vector3.zero;
    }

    public void Initialize(int w, int h)
    {
        width = h;
        height = w;

        mazeRenderer = GetComponent<SpriteRenderer>();
        GenerateMazeAndPlayer();
    }

    void Update()
    {
        Vector2Int inputDir = Vector2Int.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.iKey.wasPressedThisFrame)
                inputDir = Vector2Int.up;
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.kKey.wasPressedThisFrame)
                inputDir = Vector2Int.down;
            else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.jKey.wasPressedThisFrame)
                inputDir = Vector2Int.left;
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.lKey.wasPressedThisFrame)
                inputDir = Vector2Int.right;
        }

        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                touchStartPos = touch.position.ReadValue();
                isSwiping = true;
            }

            if (touch.press.wasReleasedThisFrame && isSwiping)
            {
                Vector2 touchEndPos = touch.position.ReadValue();
                Vector2 delta = touchEndPos - touchStartPos;
                float distance = delta.magnitude;

                if (distance > minSwipeDistance)
                {
                    inputDir = GetSwipeDirection(delta);
                }

                isSwiping = false;
            }
        }

        if (inputDir != Vector2Int.zero && moveCoroutine == null)
        {
            Vector2Int targetCell = playerCell + inputDir;
            if (CanMoveTo(playerCell, targetCell))
            {
                moveCoroutine = StartCoroutine(MovePlayerSmooth(targetCell));
                targetCell = playerCell + inputDir;
            }
            while (CanMoveTo(playerCell, targetCell) && !HasSidePath(playerCell, inputDir))
            {
                moveCoroutine = StartCoroutine(MovePlayerSmooth(targetCell));
                targetCell = playerCell + inputDir;
            }
        }
    }

    private Vector2Int GetSwipeDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        }
        else
        {
            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }
    }

    bool HasSidePath(Vector2Int cell, Vector2Int forwardDir)
    {
        Vector2Int left = Rotate90CCW(forwardDir);
        Vector2Int right = Rotate90CW(forwardDir);

        return CanMoveTo(cell, cell + left) || CanMoveTo(cell, cell + right);
    }

    Vector2Int Rotate90CW(Vector2Int v) => new Vector2Int(v.y, -v.x);
    Vector2Int Rotate90CCW(Vector2Int v) => new Vector2Int(-v.y, v.x);

    public void GenerateMazeAndPlayer()
    {
        score = 0;

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (playerObject) Destroy(playerObject);
        if (exitObject) Destroy(exitObject);
        foreach (var t in treasureObjects) if (t) Destroy(t);
        treasureObjects.Clear();

        GenerateMaze();
        CreateMazeTexture();

        playerCell = new Vector2Int(0, 0);
        Vector2Int exitCell = new Vector2Int(width - 1, height - 1);

        playerObject = Instantiate(playerPrefab, transform);
        PlaceObjectAtCell(playerObject, playerCell);

        exitObject = Instantiate(exitPrefab, transform);
        PlaceObjectAtCell(exitObject, exitCell);

        int totalCells = width * height;
        int treasureCount = Mathf.RoundToInt(totalCells * 0.1f);
        treasureCount = Mathf.Max(1, treasureCount);

        List<Vector2Int> availableCells = new List<Vector2Int>();
        for (int x = 0; x < width - 1; x++)
            for (int y = 0; y < height - 1; y++)
                if (!(x == 0 && y == 0) && !(x == width - 1 && y == height - 1))
                    availableCells.Add(new Vector2Int(x, y));

        for (int i = 0; i < treasureCount && availableCells.Count > 0; i++)
        {
            int idx = Random.Range(0, availableCells.Count);
            Vector2Int cell = availableCells[idx];
            availableCells.RemoveAt(idx);

            GameObject treasure = Instantiate(treasurePrefab, transform);
            PlaceObjectAtCell(treasure, cell);
            treasureObjects.Add(treasure);
        }
    }

    void PlaceObjectAtCell(GameObject obj, Vector2Int cell)
    {
        Vector3 worldPos = new Vector3(
            (cell.x - width / 2f + 0.5f) * 1,
            (cell.y - height / 2f + 0.5f) * 1,
            0
        );
        obj.transform.localPosition = worldPos;
    }

    void GenerateMaze()
    {
        visited = new bool[width, height];
        horizontalWalls = new bool[width, height + 1];
        verticalWalls = new bool[width + 1, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y <= height; y++)
                horizontalWalls[x, y] = true;

        for (int x = 0; x <= width; x++)
            for (int y = 0; y < height; y++)
                verticalWalls[x, y] = true;

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int start = new Vector2Int(width / 2, height / 2);
        visited[start.x, start.y] = true;
        stack.Push(start);

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> neighbors = new List<Vector2Int>();

            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;
                if (next.x >= 0 && next.x < width && next.y >= 0 && next.y < height && !visited[next.x, next.y])
                    neighbors.Add(dir);
            }

            if (neighbors.Count > 0)
            {
                Vector2Int chosenDir = neighbors[Random.Range(0, neighbors.Count)];
                Vector2Int nextCell = current + chosenDir;

                if (chosenDir == Vector2Int.up)
                    horizontalWalls[current.x, current.y + 1] = false;
                else if (chosenDir == Vector2Int.right)
                    verticalWalls[current.x + 1, current.y] = false;
                else if (chosenDir == Vector2Int.down)
                    horizontalWalls[current.x, current.y] = false;
                else if (chosenDir == Vector2Int.left)
                    verticalWalls[current.x, current.y] = false;

                visited[nextCell.x, nextCell.y] = true;
                stack.Push(nextCell);
            }
            else
            {
                stack.Pop();
            }
        }
    }

    void CreateMazeTexture()
    {
        int texWidth = width * cellSize;
        int texHeight = height * cellSize;

        Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false);
        tex.filterMode = FilterMode.Point;

        Color[] floorPixels = new Color[texWidth * texHeight];
        for (int i = 0; i < floorPixels.Length; i++) floorPixels[i] = floorColor;
        tex.SetPixels(0, 0, texWidth, texHeight, floorPixels);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                if (horizontalWalls[x, y])
                {
                    int px = x * cellSize;
                    int py = y * cellSize;
                    tex.FillRect(px - wallThickness / 2, py - wallThickness / 2, cellSize + wallThickness, wallThickness, wallColor);
                }
            }
        }

        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (verticalWalls[x, y])
                {
                    int px = x * cellSize;
                    int py = y * cellSize;
                    tex.FillRect(px - wallThickness / 2, py - wallThickness / 2, wallThickness, cellSize + wallThickness, wallColor);
                }
            }
        }

        tex.Apply();

        if (mazeRenderer.sprite != null)
            DestroyImmediate(mazeRenderer.sprite);

        mazeRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f), cellSize);
    }

    bool CanMoveTo(Vector2Int cellFrom, Vector2Int cellTo)
    {
        if (cellTo.x < 0 || cellTo.x >= width || cellTo.y < 0 || cellTo.y >= height) return false;

        Vector2Int dir = cellTo - cellFrom;

        if (dir == Vector2Int.up) return !horizontalWalls[cellFrom.x, cellFrom.y + 1];
        if (dir == Vector2Int.right) return !verticalWalls[cellFrom.x + 1, cellFrom.y];
        if (dir == Vector2Int.down) return !horizontalWalls[cellFrom.x, cellFrom.y];
        if (dir == Vector2Int.left) return !verticalWalls[cellFrom.x, cellFrom.y];

        return false;
    }

    IEnumerator MovePlayerSmooth(Vector2Int targetCell)
    {
        playerCell = targetCell;

        Vector3 startPos = playerObject.transform.localPosition;
        Vector3 targetPos = new Vector3(
            targetCell.x - width / 2f + 0.5f,
            targetCell.y - height / 2f + 0.5f,
            0
        );

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            playerObject.transform.localPosition = Vector3.Lerp(startPos, targetPos, EaseOutCubic(t));
            yield return null;
        }

        playerObject.transform.localPosition = targetPos;
        moveCoroutine = null;

        CheckTreasurePickup(targetCell);

        if (playerCell == new Vector2Int(width - 1, height - 1))
        {
            EndGame();
        }
    }

    void CheckTreasurePickup(Vector2Int cell)
    {
        for (int i = treasureObjects.Count - 1; i >= 0; i--)
        {
            GameObject treasure = treasureObjects[i];
            if (treasure == null) continue;

            Vector2Int treasureCell = WorldToCell(treasure.transform.localPosition);
            if (treasureCell == cell)
            {
                score += 5;
                Destroy(treasure);
                treasureObjects.RemoveAt(i);
            }
        }
    }

    Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x + width / 2f - 0.5f);
        int y = Mathf.RoundToInt(worldPos.y + height / 2f - 0.5f);
        return new Vector2Int(x, y);
    }

    void EndGame()
    {

        int exitBonus = width * height;
        score += exitBonus;

        long finalScore = (long)score * score;
        Debug.Log($"=== ИГРА ЗАВЕРШЕНА ===\nОчки: {score} (сокровища: {score - exitBonus}, бонус выхода: {exitBonus})\nФинальный результат: {finalScore}");

        gameManager.OnGameEnd(score, width, height);
    }

    float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    public void CloseMaze()
    {
        if (planeManager) planeManager.enabled = true;
        Destroy(gameObject);
    }
}

public static class Texture2DExtensions
{
    public static void FillRect(this Texture2D tex, int x, int y, int width, int height, Color color)
    {
        int xEnd = Mathf.Min(x + width, tex.width);
        int yEnd = Mathf.Min(y + height, tex.height);

        for (int py = Mathf.Max(y, 0); py < yEnd; py++)
        {
            for (int px = Mathf.Max(x, 0); px < xEnd; px++)
            {
                tex.SetPixel(px, py, color);
            }
        }
    }
}