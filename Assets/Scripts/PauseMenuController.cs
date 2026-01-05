using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the Pause Menu during gameplay.
/// Handles:
/// - Pause / Resume logic
/// - Options menu inside pause
/// - Scene navigation
/// - Blocking pause when game is won or lost
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("PuaseMenu Reference")]
    public GameObject pauseMenuPanel; // Main pause menu panel
    // Pause menu buttons
    public Button resumeButton;
    public Button mainMenuButton;
    public Button optionButton;
    public Button quitButton;

    [Header("OptionMenu Reference")]
    public GameObject optionMenuPanel; // Option menu panel inside pause menu
    // Audio sliders (placeholders for future audio system)
    public Slider musicSlider;
    public Slider effectSlider;
    public Button backButton; // Back button (returns to pause menu)

    [Header("GamePlay")]
    private bool isPaused = false; // Tracks whether the game is currently paused
    // Cached game-over states
    private bool isWon = false;
    private bool isLose = false;

    // Snake references used to query win/lose state
    [Header("Reference")]
    [SerializeField] private SnakeController snakePlayer_01;
    [SerializeField] private SnakeController snakePlayer_02;


    private int mainMenuSceneBuildIndex  = 0; // Main menu scene build index


    /// <summary>
    /// Registers all button and slider listeners.
    /// </summary>
    private void Awake()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);
        if (optionButton != null) optionButton.onClick.AddListener(OptionMenuPopUp);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        if (backButton != null) backButton.onClick.AddListener(PauseMenuPopUp);

        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicVolumeChange);
        if (effectSlider != null) effectSlider.onValueChanged.AddListener(OnEffectVolumeChange);

    }


    /// <summary>
    /// Checks for pause input and game-over state every frame.
    /// Prevents pause menu from opening after win or lose.
    /// </summary>
    void Update()
    {
        UpdateGameState();

        if (isWon || isLose)
        {
            if(pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if(optionMenuPanel != null) optionMenuPanel.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
            Debug.Log("Escape is pressed");
        }
    }

    /// <summary>
    /// Toggles pause state.
    /// Freezes/unfreezes game time and manages cursor state.
    /// </summary>
    private void TogglePause()
    {
        if (isWon || isLose) return;

        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1.0f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (optionMenuPanel != null) optionMenuPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Resumes gameplay from paused state.
    /// </summary>
    private void ResumeGame()
    {
        Debug.LogError("Resume Button is clicked");   
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionMenuPanel != null) optionMenuPanel.SetActive(false);

        isPaused = false;
        Time.timeScale = 1.0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Loads the main menu scene.
    /// </summary>
    private void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneBuildIndex);
    }

    /// <summary>
    /// Opens option menu from pause menu.
    /// </summary>
    private void OptionMenuPopUp()
    {
        if (optionMenuPanel != null) optionMenuPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Returns from option menu back to pause menu.
    /// </summary>
    private void PauseMenuPopUp()
    {
        if (optionMenuPanel != null) optionMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Updates cached win/lose state for both players.
    /// Used to block pause menu after game ends.
    /// </summary>
    private void UpdateGameState()
    {
        isWon = GameController.instance.GetPlayerGameWonState(snakePlayer_01) || GameController.instance.GetPlayerGameWonState(snakePlayer_02);
        isLose = GameController.instance.GetPlayerGameLoseState(snakePlayer_01) || GameController.instance.GetPlayerGameLoseState(snakePlayer_02);
    }

    /// <summary>
    /// Called when music volume slider changes.
    /// </summary>
    private void OnMusicVolumeChange(float value)
    {

    }

    /// <summary>
    /// Called when effect volume slider changes.
    /// </summary>
    private void OnEffectVolumeChange(float value)
    {

    }

    /// <summary>
    /// Removes listeners to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(ResumeGame);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(LoadMainMenu);
        if (optionButton != null) optionButton.onClick.RemoveListener(OptionMenuPopUp);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);

        if (backButton != null) backButton.onClick.RemoveListener(PauseMenuPopUp);

        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChange);
        if (effectSlider != null) effectSlider.onValueChanged.RemoveListener(OnEffectVolumeChange);
    }
}
