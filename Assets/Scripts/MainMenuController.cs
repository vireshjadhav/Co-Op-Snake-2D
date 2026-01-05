using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles main menu UI flow:
/// - start menu
/// - player mode selection
/// - options menu
/// - scene loading
/// - quit game
/// stores the selected game mode 
/// (single/two - player) for use in gameplay.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject startMenuPanel; //Root start menu panel.
    //Main menu buttons.
    public Button startButton;
    public Button optionButton;
    public Button quitButton;


    [Header("PlayerMode Pop-Up")]
    public GameObject playerModePanel; //Player mode selection panel.
    //Buttons for choosing game mode.
    public Button player_01_Button;
    public Button player_02_Button;
    public Button backButton; //Back button from player mode panel.


    [Header("OptionMenu Pop-Up")]
    public GameObject optionMenuPanel; //Option menu panel.
    public Button backButton_2; //Back button from option menu.
    //Audio sliders (currently placeholders)
    public Slider musicSlider;
    public Slider effectSlider;

    [Header("Scene")]
    [Tooltip("Build index or scene to load for gameplay")]
    [SerializeField] private int gameSceneBuildIndex = 1; //Gameplay scene build index

    /// <summary>
    /// Stores selected game mode globally
    /// false = single-player
    /// true = two-player
    /// </summary>
    [Header("PlayerMode Selection")]
    public static bool isTwoPlayerModeOn = false;


    /// <summary>
    /// Register all button and slider listeners.
    /// </summary>
    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(DirectToPlayerModePanel);
        if (optionButton != null) optionButton.onClick.AddListener(DirectToOptionMenuPanel);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        if (backButton != null) backButton.onClick.AddListener(CloseOptionPopUp);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicVolumeChange);
        if (effectSlider != null) effectSlider.onValueChanged.AddListener(OnEffectVolumeChange);


        if (player_01_Button != null) player_01_Button.onClick.AddListener(ChoosePlayerOneControlScheme);
        if (player_02_Button != null) player_02_Button.onClick.AddListener(ChoosePlayerTwoControlScheme);
        if (backButton_2 != null) backButton_2.onClick.AddListener(ClosePlayerModePopUp);
    }

    /// <summary>
    /// Ensures option and player mode panels
    /// are hidden when the menu loads.
    /// </summary>
    private void Start()
    {
        if (optionMenuPanel != null) optionMenuPanel.SetActive(false);
        if (playerModePanel != null) playerModePanel.SetActive(false);
    }


    /// <summary>
    /// Closes the player mode panel
    /// and returns to the start menu.
    /// </summary>
    private void ClosePlayerModePopUp()
    {
        playerModePanel.SetActive(false);
        startMenuPanel.SetActive(true);

    }


    /// <summary>
    /// Closes the option menu
    /// and returns to the start menu.
    /// </summary>
    private void CloseOptionPopUp()
    {
        optionMenuPanel.SetActive(false);
        startMenuPanel.SetActive(true);
    }


    /// <summary>
    /// Opens the option menu
    /// and hides the start menu.
    /// </summary>
    private void DirectToOptionMenuPanel()
    {
        optionMenuPanel.SetActive(true);
        startMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Opens the player mode selection panel
    /// and hides the start menu.
    /// </summary>
    private void DirectToPlayerModePanel()
    {
        playerModePanel.SetActive(true);
        startMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Sets single-player mode
    /// and loads the gameplay scene.
    /// </summary>
    private void  ChoosePlayerOneControlScheme()
    {
        isTwoPlayerModeOn = false;
        LoadGameScene();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    /// <summary>
    /// Sets two-player mode
    /// and loads the gameplay scene.
    /// </summary>
    private void ChoosePlayerTwoControlScheme()
    {
        isTwoPlayerModeOn = true;
        LoadGameScene();
        Cursor.lockState= CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Loads the gameplay scene.
    /// Uses a fallback scene index if invalid.
    /// </summary>
    private void LoadGameScene()
    {
        if (gameSceneBuildIndex >= 0 && gameSceneBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(gameSceneBuildIndex);
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }

    /// <summary>
    /// Called when music volume slider changes.
    /// (Audio system not implemented yet.)
    /// </summary>
    private void OnMusicVolumeChange(float value)
    {
    }

    /// <summary>
    /// Called when effect volume slider changes.
    /// (Audio system not implemented yet.)
    /// </summary>
    private void OnEffectVolumeChange(float value)
    {

    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    private void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Removes all listeners to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(DirectToPlayerModePanel);
        if (optionButton != null) optionButton.onClick.RemoveListener(DirectToOptionMenuPanel);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);

        if (backButton != null) backButton.onClick.RemoveListener(CloseOptionPopUp);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChange);
        if (effectSlider != null) effectSlider.onValueChanged.RemoveListener(OnEffectVolumeChange);


        if (player_01_Button != null) player_01_Button.onClick.RemoveListener(ChoosePlayerOneControlScheme);
        if (player_02_Button != null) player_02_Button.onClick.RemoveListener(ChoosePlayerTwoControlScheme);
        if (backButton_2 != null) backButton_2.onClick.RemoveListener(ClosePlayerModePopUp);
    }
}
