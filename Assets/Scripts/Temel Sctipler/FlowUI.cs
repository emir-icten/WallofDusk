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
    public GameObject mainMenuPanel;        // Başlangıç menüsü (Play butonu)
    public GameObject characterSelectPanel; // Play'den sonra açılacak panel
    public GameObject gameHUDPanel;         // Oyun içi HUD (kaynaklar, butonlar)
    public GameObject gameOverPanel;        // Game Over ekranı

    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // İstersen başka sahnelerde de kullanacaksan:
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Oyun açıldığında her şeyi temizce ayarla
        SetState(GameState.MainMenu);
    }

    /// <summary>
    /// Oyun durumunu değiştirip panelleri / Time.timeScale'i ayarlar.
    /// </summary>
    public void SetState(GameState newState)
    {
        CurrentState = newState;

        bool isMainMenu = newState == GameState.MainMenu;
        bool isPlaying  = newState == GameState.Playing;
        bool isGameOver = newState == GameState.GameOver;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(isMainMenu);

        // Karakter seçimi sadece Play'e basınca açılacak,
        // bu yüzden SetState içinde her zaman kapalı tutuyoruz.
        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(false);

        if (gameHUDPanel != null)
            gameHUDPanel.SetActive(isPlaying);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(isGameOver);

        // Sadece Playing durumunda oyun akar, diğerlerinde durur
        Time.timeScale = isPlaying ? 1f : 0f;
    }

    // ---------------- UI BUTONLARI ----------------

    /// <summary>
    /// Ana menüdeki Play butonu.
    /// </summary>
    public void OnPlayButton()
    {
        // Ana menüyü kapat, karakter seçme panelini aç.
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(true);

        // Seçim ekranında oyun akmasın
        Time.timeScale = 0f;
    }

    /// <summary>
    /// PlayerSelectionManager bir karakter seçtiğinde çağırır.
    /// </summary>
    public void OnCharacterSelected()
    {
        // Artık oyun başlasın
        SetState(GameState.Playing);
    }

    /// <summary>
    /// Base öldüğünde Health script'i burayı çağırıyor.
    /// </summary>
    public void OnGameOver()
    {
        Debug.Log("FlowUI: Game Over durumu alındı.");
        SetState(GameState.GameOver);
    }

    /// <summary>
    /// Game Over ekranındaki Restart butonu.
    /// </summary>
    public void OnRestartButton()
    {
        Debug.Log("Restart pressed");
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    /// <summary>
    /// İstersen Game Over veya Main Menu'de "Quit" butonuna bağla.
    /// </summary>
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
