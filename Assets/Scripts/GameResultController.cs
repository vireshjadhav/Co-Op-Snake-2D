using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// Controls the end of match result UI.
/// Resposible for detecting game over, 
/// displaying scores, determining winner/loser, 
/// and handling navigation actions.
/// </summary>
public class GameResultController : MonoBehaviour
{
    //Buttons shown on result UI(Single and dual layouts)
    [Header("GameResultMenu")]
    public Button mainMenuButton;
    public Button quitButton;
    public Button mainMenuButton_2;
    public Button quitButton_2;

    //Panels used to show results
    [Header("Panels")]
    [SerializeField] private GameObject onePlayerResultPanel;
    [SerializeField] private GameObject twoPlayerResultPanel;
    [SerializeField] private GameObject gameResultUIPanel;


    //Snake references used to fetch score and win/lose state
    [Header("Reference")]
    [SerializeField] private SnakeController snakeController_01;
    [SerializeField] private SnakeController snakeController_02;


    //Score text references (for both layouts)
    [Header("Score Text")]
    [SerializeField] private TextMeshProUGUI playerOneScore_01;
    [SerializeField] private TextMeshProUGUI playerOneScore_02;
    [SerializeField] private TextMeshProUGUI playerTwoScore;
    
    //Cached scores pulled from GameController
    private int player01_Score = 0;
    private int player02_Score = 0;

    [Header("Game Object Reference")]
    //Single-player icons
    [SerializeField] private GameObject player01_WonIcon;
    [SerializeField] private GameObject player01_LoseIcon;
    //Two-player snake result icons
    [SerializeField] private GameObject snakeOneWonIcon;
    [SerializeField] private GameObject snakeOneLoseIcon;
    [SerializeField] private GameObject snakeTwoWonIcon;
    [SerializeField] private GameObject snakeTwoLoseIcon;

    //Build index for main menu scene
    private int mainMenuSceneBuildIndex = 0;
    //Prevents result UI from opening multiple times
    private bool resultShown = false;


    //Cached win/Lose state for logic clarity
    [Header("Game Won or Lose")]
    private bool isPlayerOneWon = false;
    private bool isPlayerTwoWon = false;
    private bool isPlayerOneLose = false;
    private bool isPlayerTwoLose = false;


    /// <summary>
    /// Registers button click listeners.
    /// </summary>
    private void Awake()
    {
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);
        if (mainMenuButton_2 != null) mainMenuButton_2.onClick.AddListener(LoadMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (quitButton_2 != null) quitButton_2.onClick.AddListener(QuitGame);
    }


    /// <summary>
    /// Ensure result UI is hidden at game start.
    /// </summary>
    private void Start()
    {
        gameResultUIPanel.SetActive(false);
    }

    /// <summary>
    /// Polls game-over state.
    /// Triggers result UI once when game ends.
    /// </summary>
    void Update()
    {
        if(resultShown || !IsGameOver()) return;

        resultShown = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowResult();
    }

    /// <summary>
    /// Entry point for showing result UI.
    /// </summary>
    private void ShowResult()
    {
        gameResultUIPanel.SetActive(true);
        GetSnakesScore();
        SetActivateResultPanel();
        FinalScore();
        ChoseGameWonIconOrLose();
    }


    /// <summary>
    /// Enables correct result panel based on game mode.
    /// </summary>
    private void SetActivateResultPanel()
    {
        if (MainMenuController.isTwoPlayerModeOn)
        {
            if (onePlayerResultPanel != null) onePlayerResultPanel.SetActive(false);
            if (twoPlayerResultPanel != null) twoPlayerResultPanel.SetActive(true);
        }
        else
        {
            if (onePlayerResultPanel != null) onePlayerResultPanel.SetActive(true);
            if (twoPlayerResultPanel != null) twoPlayerResultPanel.SetActive(false);
        }
    }


    /// <summary>
    /// Fetches final scores from GameController. 
    /// </summary>
    private void GetSnakesScore()
    {
        player01_Score = GameController.instance.GetScore(snakeController_01);
        player02_Score = GameController.instance.GetScore(snakeController_02);
    }

    /// <summary>
    /// Writes final scores to UI.
    /// </summary>
    public void FinalScore()
    {
        playerOneScore_01.text = player01_Score.ToString();
        playerOneScore_02.text = player01_Score.ToString();
        playerTwoScore.text = player02_Score.ToString();
    }

    /// <summary>
    /// Determines winner/loser visuals.
    /// Head-to-head collision take priority.
    /// </summary>
    private void ChoseGameWonIconOrLose()
    {
        if (MainMenuController.isTwoPlayerModeOn)
        {
            if (GameController.instance.IsHeadToHeadCollision())
            {
                if (player01_Score > player02_Score)
                {
                    PlayerOneWon();
                }
                else if (player02_Score > player01_Score)
                {
                    PlayerTwoWon();
                }
                else
                {
                    NoPlayerWon();
                }
                return;
            }

            if (isPlayerOneWon)
            {
                PlayerOneWon();
            }
            else if (isPlayerOneLose)
            {
                PlayerTwoWon();
            }
            else if (isPlayerTwoWon)
            {
                PlayerTwoWon();
            }
            else if (isPlayerTwoLose)
            {
                PlayerOneWon();
            }

            return;
        }
        else
        {
            if (isPlayerOneLose)
            {
                player01_WonIcon.SetActive(!isPlayerOneLose);
                player01_LoseIcon.SetActive(isPlayerOneLose);
            }
            else if (isPlayerOneWon)
            {
                player01_WonIcon.SetActive(isPlayerOneWon);
                player01_LoseIcon.SetActive(!isPlayerOneWon);
            }
            else
            {
                NoPlayerWon();
            }
        }
    }

    /*============================= Icon Helpers =============================*/
    /// <summary>
    /// Marks player one as winner.
    /// </summary>
    private void PlayerOneWon()
    {
        snakeOneWonIcon.SetActive(true);
        snakeOneLoseIcon.SetActive(false);

        snakeTwoWonIcon.SetActive(false);
        snakeTwoLoseIcon.SetActive(true);
    }

    /// <summary>
    /// Marks player two as winner.
    /// </summary>
    private void PlayerTwoWon()
    {
        snakeOneWonIcon.SetActive(false);
        snakeOneLoseIcon.SetActive(true);

        snakeTwoWonIcon.SetActive(true);
        snakeTwoLoseIcon.SetActive(false);
    }

    /// <summary>
    /// Draw condition: both players lose.
    /// </summary>
    private void NoPlayerWon()
    {
        snakeOneWonIcon.SetActive(false);
        snakeOneLoseIcon.SetActive(true);

        snakeTwoWonIcon.SetActive(false);
        snakeTwoLoseIcon.SetActive(true);
    }
    /*========================================================================*/
    
    /// <summary>
    /// Updates and returns whether the match has ended.
    /// </summary>
    private bool IsGameOver()
    {
        isPlayerOneWon = GameController.instance.GetPlayerGameWonState(snakeController_01);
        isPlayerOneLose = GameController.instance.GetPlayerGameLoseState(snakeController_01);
        isPlayerTwoWon = GameController.instance.GetPlayerGameWonState(snakeController_02);
        isPlayerTwoLose = GameController.instance.GetPlayerGameLoseState(snakeController_02);

        if (!MainMenuController.isTwoPlayerModeOn)
        {
            return isPlayerOneLose || isPlayerOneWon;
        }

        return isPlayerOneLose || isPlayerOneWon || isPlayerTwoLose || isPlayerTwoWon;
    }


    /// <summary>
    /// Load main menu scene.
    /// </summary>
    private void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneBuildIndex);
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
    /// Cleans up button listeners to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(LoadMainMenu);
        if (mainMenuButton_2 != null) mainMenuButton_2.onClick.RemoveListener(LoadMainMenu);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        if (quitButton_2 != null) quitButton_2.onClick.RemoveListener(QuitGame);
    }
}
