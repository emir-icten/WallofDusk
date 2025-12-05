using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Playing,
    GameOver
}

public class FlowUI : MonoBehaviour
{
    public static FlowUI Instance { get; private set; }

    [Header("UI Panelleri")]
    public GameObject mainMenuPanel;   // Başlangıç menüsü (Play)
    public GameObject gameHudPanel;    // Oyun içi HUD
    public GameObject gameOverPanel;   // Game Over ekranı

    [Header("Sahneler")]
    [Tooltip("Ana oyun sahnesinin adı (Build Settings'teki isim ile birebir aynı olmalı)")]
    public string gameSceneName = "Zemin";   // senin sahnenin adı neyse onu yaz

    [Header("Başlangıç Ayarı")]
    public bool startInMainMenu = true;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Şimdilik sahneler arası taşımıyoruz:
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (startInMainMenu)
            SetState(GameState.MainMenu);
        else
            SetState(GameState.Playing);
    }

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log("GameState -> " + newState);

        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 0f;
                if (mainMenuPanel) mainMenuPanel.SetActive(true);
                if (gameHudPanel) gameHudPanel.SetActive(false);
                if (gameOverPanel) gameOverPanel.SetActive(false);
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                if (mainMenuPanel) mainMenuPanel.SetActive(false);
                if (gameHudPanel) gameHudPanel.SetActive(true);
                if (gameOverPanel) gameOverPanel.SetActive(false);
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                if (mainMenuPanel) mainMenuPanel.SetActive(false);
                if (gameHudPanel) gameHudPanel.SetActive(false);
                if (gameOverPanel) gameOverPanel.SetActive(true);
                break;
        }
    }

    // === UI BUTON FONKSİYONLARI ===

    public void OnPlayButton()
    {
        SetState(GameState.Playing);
    }

    public void OnGameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        SetState(GameState.GameOver);
    }

   public void OnRestartButton()
{
    Debug.Log("Restart pressed");
    Time.timeScale = 1f;

    // Aktif sahneyi BAŞTAN yükle
    Scene current = SceneManager.GetActiveScene();
    SceneManager.LoadScene(current.buildIndex);
}

    public void OnExitButton()
    {
        Debug.Log("Exit pressed");
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
