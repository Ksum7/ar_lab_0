using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject statsPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;

    [Header("Stats Text (единственное поле)")]
    public TextMeshProUGUI statsText; // Одно текстовое поле для всей статистики

    [Header("Game Over Text")]
    public TextMeshProUGUI gameScoreText;

    [Header("Buttons")]
    public Button startButton; // Main menu start
    public Button statsButton; // Main menu stats
    public Button exitButton; // Main menu exit
    public Button backFromStatsButton; // Stats back to main
    public Button resetButton; // Stats reset
    public Button pauseButton; // In-game pause (top-left)
    public Button resumeButton; // Pause resume
    public Button toMainFromPauseButton; // Pause to main
    public Button resumeFromGameOverButton; // Game over to main
    public Button mazeCloseButton; // Close maze button

    private int highScore = 0;
    private long totalScore = 0;
    private int largestField = 0;
    private int maxTreasures = 0;

    private ARPlaneManager arPlaneManager;
    private ARCameraManager arCameraManager;
    private ARSession arSession;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        arPlaneManager = FindObjectOfType<ARPlaneManager>();
        arCameraManager = FindObjectOfType<ARCameraManager>();
        arSession = FindObjectOfType<ARSession>();
        if (arPlaneManager) arPlaneManager.enabled = false;

        LoadStats();

        // Setup button listeners
        startButton.onClick.AddListener(OnStart);
        statsButton.onClick.AddListener(OnStats);
        exitButton.onClick.AddListener(OnExit);
        backFromStatsButton.onClick.AddListener(MainMenu);
        resetButton.onClick.AddListener(OnReset);
        pauseButton.onClick.AddListener(OnPause);
        resumeButton.onClick.AddListener(OnResume);
        toMainFromPauseButton.onClick.AddListener(ToMainMenu);
        resumeFromGameOverButton.onClick.AddListener(ResumeFromGameOver);
        mazeCloseButton.onClick.AddListener(OnMazeCloseButtonClicked);

        // Initial state
        MainMenu();
        mazeCloseButton.gameObject.SetActive(false);
    }

    private void LoadStats()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        totalScore = PlayerPrefs.GetInt("TotalScore", 0);
        largestField = PlayerPrefs.GetInt("LargestField", 0);
        maxTreasures = PlayerPrefs.GetInt("MaxTreasures", 0);

        UpdateStatsText(); // Обновляем текст сразу после загрузки
    }

    private void SaveStats()
    {
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.SetInt("TotalScore", (int)totalScore);
        PlayerPrefs.SetInt("LargestField", largestField);
        PlayerPrefs.SetInt("MaxTreasures", maxTreasures);
        PlayerPrefs.Save();
    }

    private void UpdateStatsText()
    {
        if (statsText != null)
        {
            statsText.text =
                $"<b>Рекорд:</b> {highScore}\n" +
                $"<b>Всего очков:</b> {totalScore}\n" +
                $"<b>Самое большое поле:</b> {largestField} клеток\n" +
                $"<b>Макс. сокровищ за игру:</b> {maxTreasures}";
        }
    }

    public void MainMenu()
    {
        mainMenuPanel.SetActive(true);
        statsPanel.SetActive(false);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        pauseButton.gameObject.SetActive(false);
        mazeCloseButton.gameObject.SetActive(false);
        if (arPlaneManager) arPlaneManager.enabled = false;
        Time.timeScale = 1f;
    }

    private void OnStart()
    {
        mainMenuPanel.SetActive(false);
        pauseButton.gameObject.SetActive(true);
        if (arPlaneManager) arPlaneManager.enabled = true;
    }

    private void OnStats()
    {
        mainMenuPanel.SetActive(false);
        statsPanel.SetActive(true);
        UpdateStatsText(); // Обновляем при открытии статистики
    }

    private void OnReset()
    {
        highScore = 0;
        totalScore = 0;
        largestField = 0;
        maxTreasures = 0;
        SaveStats();
        UpdateStatsText();
    }

    private void OnExit()
    {
        Application.Quit();
    }

    private void OnPause()
    {
        pauseButton.gameObject.SetActive(false);
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        if (arCameraManager != null)
        {
            arCameraManager.enabled = false;
        }
        if (arSession != null)
        {
            arSession.gameObject.SetActive(false);
        }
    }

    private void OnResume()
    {
        pauseButton.gameObject.SetActive(true);
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        if (arCameraManager != null)
        {
            arCameraManager.enabled = true;
        }
        if (arSession != null)
        {
            arSession.gameObject.SetActive(true);
        }
    }

    private void ToMainMenu()
    {
        MazeController mazeController = FindObjectOfType<MazeController>();
        if (mazeController != null)
        {
            Destroy(mazeController.gameObject);
        }

        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        MainMenu();
    }

    private void ResumeFromGameOver()
    {
        MazeController mazeController = FindObjectOfType<MazeController>();
        if (mazeController != null)
        {
            Destroy(mazeController.gameObject);
        }
        pauseButton.gameObject.SetActive(true);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (arPlaneManager) arPlaneManager.enabled = true;

        ARPlaneMazeDrawer[] allDrawers = FindObjectsOfType<ARPlaneMazeDrawer>(includeInactive: true);
        foreach (ARPlaneMazeDrawer drawer in allDrawers)
        {
            drawer.visualizerInstance.SetActive(true);
            drawer.buttonObj.SetActive(true);
        }
    }


    public void OnGameStart()
    {
        mazeCloseButton.gameObject.SetActive(true);
    }

    public void OnGameEnd(int score, int w, int h)
    {
        int fieldSize = w * h;
        int collected = (score - fieldSize) / 5;
        long finalScore = (long)score * score;

        if ((int)finalScore > highScore) highScore = (int)finalScore;
        totalScore += finalScore;
        if (fieldSize > largestField) largestField = fieldSize;
        if (collected > maxTreasures) maxTreasures = collected;

        SaveStats();
        UpdateStatsText(); // Обновляем статистику после завершения игры

        gameScoreText.text = finalScore.ToString();
        gameOverPanel.SetActive(true);
        pauseButton.gameObject.SetActive(false);
        mazeCloseButton.gameObject.SetActive(false);
    }

    private void OnMazeCloseButtonClicked()
    {
        MazeController mazeController = FindObjectOfType<MazeController>();
        if (mazeController != null)
        {
            mazeController.CloseMaze();
        }
        mazeCloseButton.gameObject.SetActive(false);
        if (arPlaneManager) arPlaneManager.enabled = true;

        ARPlaneMazeDrawer[] allDrawers = FindObjectsOfType<ARPlaneMazeDrawer>(includeInactive: true);
        foreach (ARPlaneMazeDrawer drawer in allDrawers)
        {
            drawer.visualizerInstance.SetActive(true);
            drawer.buttonObj.SetActive(true);
        }
    }
}