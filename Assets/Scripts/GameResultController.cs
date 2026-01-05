using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [Header("Canvas Settings")]
    [SerializeField] private Canvas resultCanvas;
    [SerializeField] private GraphicRaycaster graphicRaycaster;

    //Panels used to show results
    [Header("Panels")]
    [SerializeField] private GameObject onePlayerResultPanel;
    [SerializeField] private GameObject twoPlayerResultPanel;
    [SerializeField] private GameObject gameResultUIPanel;


    //Single Player Result UI
    [Header("Single Player UI")]
    [SerializeField] private Button singlePlayerMainMenuButton;
    [SerializeField] private Button singlePlayerQuitButton;
    [SerializeField] private TextMeshProUGUI singlePlayerScoreText;
    [SerializeField] private GameObject singlePlayerWonIcon;
    [SerializeField] private GameObject singlePlayerLoseIcon;

    //Two player Result UI
    [Header("Two Player UI")]
    [SerializeField] private Button twoPlayerMainMenuButton;
    [SerializeField] private Button twoPlayerQuitButton;
    [SerializeField] private TextMeshProUGUI player01ScoreText;
    [SerializeField] private TextMeshProUGUI player02ScoreText;
    [SerializeField] private GameObject player01WinIcon;
    [SerializeField] private GameObject player01LoseIcon;
    [SerializeField] private GameObject player02WinIcon;
    [SerializeField] private GameObject player02LoseIcon;


    //Snake references used to fetch score and win/lose state
    [Header("Reference")]
    [SerializeField] private SnakeController snakeController_01;
    [SerializeField] private SnakeController snakeController_02;


    [Header("Settings")]
    [SerializeField] private int mainMenuSceneBuildIndex = 0;    //Build index for main menu scene
    [SerializeField] private float resultDelay = 1f;

    //Cached scores pulled from GameController
    private int player01Score = 0;
    private int player02Score = 0;
    private bool resultShown = false;   //Prevents result UI from opening multiple times


    /// <summary>
    /// Registers button click listeners.
    /// </summary>
    private void Awake()
    {
        if (resultCanvas == null) resultCanvas = GetComponent<Canvas>();  //Initialize canvas components

        if (graphicRaycaster == null) graphicRaycaster = GetComponent<GraphicRaycaster>(); //Initialize GraphicRaycaster

        if (resultCanvas != null)
        {
            resultCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            resultCanvas.sortingOrder = 100; //High order to be on top
        }


        //Set up button listners
        if (singlePlayerMainMenuButton != null) singlePlayerMainMenuButton.onClick.AddListener(LoadMainMenu);
        if (twoPlayerMainMenuButton != null) twoPlayerMainMenuButton.onClick.AddListener(LoadMainMenu);
        if (singlePlayerQuitButton != null) singlePlayerQuitButton.onClick.AddListener(QuitGame);
        if (twoPlayerQuitButton != null) twoPlayerQuitButton.onClick.AddListener(QuitGame);
    }


    /// <summary>
    /// Ensure result UI is hidden at game start.
    /// </summary>
    private void Start()
    {
        //Hide all panels initially
        gameResultUIPanel.SetActive(false);
        onePlayerResultPanel.SetActive(false);
        twoPlayerResultPanel.SetActive(false);
    }

    /// <summary>
    /// Polls game-over state.
    /// Triggers result UI once when game ends.
    /// </summary>
    void Update()
    {
        if (resultShown || !IsGameOver()) return;

        resultShown = true;
        StartCoroutine(OpenResultPanelWithDelay(resultDelay));
    }



    IEnumerator OpenResultPanelWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowResult();
    }

    /// <summary>
    /// Entry point for showing result UI.
    /// </summary>
    public void ShowResult()
    {
        //Get score first
        GetSnakesScore();

        //Enable cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //Activate the main panel
        gameResultUIPanel.SetActive(true);

        if (MainMenuController.isTwoPlayerModeOn)
        {
            SetUpTwoPlayerPanel();
        }
        else
        {
            SetUpSinglePlayerPanel();
        }

        //Ensure buttons are interactable
        EnsureButtonsWork();
    }


    /// <summary>
    /// Enables Single Player Result Panel And The Respective Icons
    /// </summary>
    private void SetUpSinglePlayerPanel()
    {
        twoPlayerResultPanel.SetActive(false); //Hide two player panel

        onePlayerResultPanel.SetActive(true);   //Show and setup single player panel

        singlePlayerScoreText.text = player01Score.ToString();    //Update score

        //Determin win/lose state
        bool isPlayerWon = GameController.instance.GetPlayerGameWonState(snakeController_01);
        bool isPlayerLose = GameController.instance.GetPlayerGameLoseState(snakeController_01);

        //Show appropriate icons
        if (singlePlayerWonIcon != null) singlePlayerWonIcon.SetActive(isPlayerWon);
        if (singlePlayerLoseIcon != null) singlePlayerLoseIcon.SetActive(isPlayerLose);

        //Disable other panel's buttons to prevent overlap issue
        DisableTwoPlayerButtons();
        EnableSinglePlayerButtons();
    }


    /// <summary>
    /// Enables Single Player Result Panel And The Respective Icons
    /// </summary>
    private void SetUpTwoPlayerPanel()
    {
        onePlayerResultPanel.SetActive(false);   //Hide single player panel

        twoPlayerResultPanel.SetActive(true); //Show and setup two player panel

        //Update score
        player01ScoreText.text = player01Score.ToString();
        player02ScoreText.text = player02Score.ToString();


        bool player1Lose = GameController.instance.GetPlayerGameLoseState(snakeController_01);
        bool player2Lose = GameController.instance.GetPlayerGameLoseState(snakeController_02);

        //Determine winner based on scores(head to head takes priority
        if (GameController.instance.IsHeadToHeadCollision() || (player1Lose || player2Lose)) 
        {
            Debug.Log("Result Controller: Using HEAD-TO-HEAD result logic");
            SetUpHeadToHeadResult();
        }
        else
        {
            Debug.Log("Result Controller: Using NORMAL two player result logic");
            SetUpNormalTwoPlayerResult();
        }

        DisableSinglePlayerButtons();
        EnableTwoPlayerButtons();
    }

    private void SetUpHeadToHeadResult()
    {
        Debug.Log($"Head-to-head collision detected. P1 Score: {player01Score}, P2 Score: {player02Score}");

        if (player01Score > player02Score)
        {
            Debug.Log("Player 1 wins by score");
            PlayerOneWins();
        }
        else if (player02Score > player01Score)
        {
            Debug.Log("Player 2 wins by score");
            PlayerTwoWins();
        }
        else
        {
            Debug.Log("Draw - equal scores");
            Draw();
        }
    }


    private void SetUpNormalTwoPlayerResult()
    {
        bool player1Won = GameController.instance.GetPlayerGameWonState(snakeController_01);
        bool player1Lose = GameController.instance.GetPlayerGameLoseState(snakeController_01);
        bool player2Won = GameController.instance.GetPlayerGameWonState(snakeController_02);
        bool player2Lose = GameController.instance.GetPlayerGameLoseState(snakeController_02);

        if (player1Won || player2Lose)
        {
            PlayerOneWins();
        }
        else if (player2Won || player1Lose)
        {
            PlayerTwoWins();
        }
        else 
        {
            Draw();
        }
    }

    private void PlayerOneWins()
    {
        SetPlayerIcons(true, false, false, true);
    }
    private void PlayerTwoWins()
    {
        SetPlayerIcons(false, true, true, false);
    }
    private void Draw()
    {
        SetPlayerIcons(false, true, false, true);
    }


    private void SetPlayerIcons(bool p1Win, bool p1Lose, bool p2Win, bool p2Lose)
    {
        if (player01WinIcon != null) player01WinIcon.SetActive(p1Win);
        if (player01LoseIcon != null) player01LoseIcon.SetActive(p1Lose);
        if (player02WinIcon != null) player02WinIcon.SetActive(p2Win);
        if (player02LoseIcon != null) player02LoseIcon.SetActive(p2Lose);
    }


    private void GetSnakesScore()
    {
        if (GameController.instance != null)
        {
            player01Score = GameController.instance.GetScore(snakeController_01);

            // Only get player 2 score in two player mode
            if (MainMenuController.isTwoPlayerModeOn)
            {
                player02Score = GameController.instance.GetScore(snakeController_02);
            }
        }
    }

    /// <summary>
    /// Updates and returns whether the match has ended.
    /// </summary>
    private bool IsGameOver()
    {
        if (GameController.instance == null) return false;

        bool player1Won = GameController.instance.GetPlayerGameWonState(snakeController_01);
        bool player1Lose = GameController.instance.GetPlayerGameLoseState(snakeController_01);

        if (!MainMenuController.isTwoPlayerModeOn)
        {
            return player1Won || player1Lose;
        }

        bool player2Won = GameController.instance.GetPlayerGameWonState(snakeController_02);
        bool player2Lose = GameController.instance.GetPlayerGameLoseState(snakeController_02);

        return player1Won || player2Lose || player2Won || player2Lose;

    }

    #region Button Management
    private void EnsureButtonsWork()
    {
        //Make sure EventSystem exist
        if (EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        //Ensure graphic raycaster is enabled
        if (graphicRaycaster != null) graphicRaycaster.enabled = true;
    }

    private void EnableSinglePlayerButtons()
    {
        if (singlePlayerMainMenuButton != null)
        {
            singlePlayerMainMenuButton.gameObject.SetActive(true);
            singlePlayerMainMenuButton.interactable = true;
        }
        if (singlePlayerQuitButton != null)
        {
            singlePlayerQuitButton.gameObject.SetActive(true);
            singlePlayerQuitButton.interactable = true;
        }
    }

    private void EnableTwoPlayerButtons()
    {
        if (twoPlayerMainMenuButton != null)
        {
            twoPlayerMainMenuButton.gameObject.SetActive(true);
            twoPlayerMainMenuButton.interactable = true;
        }
        if (twoPlayerQuitButton != null)
        {
            twoPlayerQuitButton.gameObject.SetActive(true);
            twoPlayerQuitButton.interactable = true;
        }
    }

    private void DisableSinglePlayerButtons()
    {
        if (singlePlayerMainMenuButton != null)
        {
            singlePlayerMainMenuButton.interactable = false;
            singlePlayerMainMenuButton.gameObject.SetActive(false);
        }
        if (singlePlayerQuitButton != null)
        {
            singlePlayerQuitButton.interactable = false;
            singlePlayerQuitButton.gameObject.SetActive(false);
        }
    }

    private void DisableTwoPlayerButtons()
    {
        if (twoPlayerMainMenuButton != null)
        {
            twoPlayerMainMenuButton.interactable = false;
            twoPlayerMainMenuButton.gameObject.SetActive(false);
        }
        if (twoPlayerQuitButton != null)
        {
            twoPlayerQuitButton.interactable = false;
            twoPlayerQuitButton.gameObject.SetActive(false);
        }
    }

    #endregion



    /// <summary>
    /// Load main menu scene.
    /// </summary>
    private void LoadMainMenu()
    {
        SoundManager.Instance.Play(Sounds.ButtonClick);

        SceneManager.LoadScene(mainMenuSceneBuildIndex);
    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    private void QuitGame()
    {
        SoundManager.Instance.Play(Sounds.ButtonClick);

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
        if (singlePlayerMainMenuButton != null) singlePlayerMainMenuButton.onClick.RemoveListener(LoadMainMenu);
        if (twoPlayerMainMenuButton != null) twoPlayerMainMenuButton.onClick.RemoveListener(LoadMainMenu);
        if (singlePlayerQuitButton != null) singlePlayerQuitButton.onClick.RemoveListener(QuitGame);
        if (twoPlayerQuitButton != null) twoPlayerQuitButton.onClick.RemoveListener(QuitGame);
    }
}
