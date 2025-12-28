using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using static UnityEditor.Experimental.GraphView.GraphView;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject _player;

    [Header("UI")]
    public GameObject MainMenuUI;
    public GameObject YouWinUI;
    public GameObject GameOverUI;

    [Header("UI Text")]
    public TMP_Text TimerTMP;
    public TMP_Text CubesLeftTMP;

    [Header("Game Rules")]
    public float gameDuration = 120f; // 2 minutes
    public int totalCubes = 5;

    private float timer;
    private int cubesLeft;
    private bool gameRunning;

    // ?? Persist start state across reload
    private static bool skipMainMenu;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 0;
        cubesLeft = totalCubes;
        timer = gameDuration;

        UpdateCubesUI();
        UpdateTimerUI();

        YouWinUI.SetActive(false);
        GameOverUI.SetActive(false);

        if (skipMainMenu)
        {
            MainMenuUI.SetActive(false);
            StartGame();
        }
        else
        {
            MainMenuUI.SetActive(true);
            gameRunning = false;
        }
    }

    private void Update()
    {
        if (!gameRunning) return;

        timer -= Time.deltaTime;
        UpdateTimerUI();

        if (timer <= 0f)
        {
            timer = 0f;
            GameOver();
        }
    }

    // ----------------------------------
    // GAME FLOW
    // ----------------------------------
    public void StartGame()
    {
        AudioManager.Instance.PlayButtonClick();
        Time.timeScale = 1;
        MainMenuUI.SetActive(false);
        YouWinUI.SetActive(false);
        GameOverUI.SetActive(false);

        gameRunning = true;
        skipMainMenu = true;
    }

    public void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GameOver()
    {
        _player.SetActive(false);    
        gameRunning = false;
        GameOverUI.SetActive(true);
        AudioManager.Instance.PlayGameOver();
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private void WinGame()
    {
        AudioManager.Instance.PlayGameWin();
        Time.timeScale = 0;
        gameRunning = false;
        YouWinUI.SetActive(true);
    }

    // ----------------------------------
    // CUBES
    // ----------------------------------
    public void CollectCube()
    {
        AudioManager.Instance.PlayCubeCollected();
        if (!gameRunning) return;

        cubesLeft--;
        cubesLeft = Mathf.Max(cubesLeft, 0);

        UpdateCubesUI();

        if (cubesLeft == 0)
        {
            WinGame();
        }
    }

    // ----------------------------------
    // UI HELPERS
    // ----------------------------------
    private void UpdateTimerUI()
    {
        int totalSeconds = Mathf.CeilToInt(timer);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        TimerTMP.text = string.Format("{0:00}:{1:00}", minutes,seconds);
    }

    private void UpdateCubesUI()
    {
        CubesLeftTMP.text = cubesLeft.ToString();
    }
}
