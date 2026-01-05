using TMPro;
using UnityEngine;

/// <summary>
/// Handles in-game HUD (Heads-Up Display).
/// Responsible for:
/// - Switching between one-player and two-player UI layouts.
/// - Displaying and updating player scores during gameplay.
/// </summary>
public class GamePlayUIController : MonoBehaviour
{
    //Snake references used to fetch score data from GameController
    [Header("Reference")]
    [SerializeField] private SnakeController snakePlayer_01;
    [SerializeField] private SnakeController snakePlayer_02;

    [Header("ScorePanel")]
    [SerializeField] private GameObject onePlayerPanel;  //UI panel for single-player mode
    [SerializeField] private GameObject twoPlayerPanel;  //UI panel for two-player mode

    [Header("ScoreText")]
    [SerializeField] private TextMeshProUGUI player_01_Score;  //Player 1 score text (single-player panel)
    [SerializeField] private TextMeshProUGUI player_01_PanelTwo_Score;  //Player 1 score text (two-player panel)
    [SerializeField] private TextMeshProUGUI player_02_Score;  //Player 1 score text (two-player panel)

    /// <summary>
    /// Initializes the HUD when gameplay starts.
    /// Choose the correct UI layout based on selected game mode.
    /// </summary>
    void Start()
    {
        SetUpHUD();
    }


    /// <summary>
    /// Enables the correct score panel based on game mode.
    /// Single-player -> onePlayerPanel
    /// Two-player -> twoPlayerPanel
    /// </summary>
    private void SetUpHUD()
    {

        if (MainMenuController.isTwoPlayerModeOn)
        {
            onePlayerPanel.SetActive(false);
            twoPlayerPanel.SetActive(true);
        }
        else
        {
            onePlayerPanel.SetActive(true);
            twoPlayerPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Updates score values displayed on the HUD.
    /// Called whenever score changes (food/poison).
    /// </summary>
    public void UpdateScore()
    {
        int sP1 = GameController.instance.GetScore(snakePlayer_01);
        int sP2 = GameController.instance.GetScore(snakePlayer_02);

        //Update player 1 score (both layouts)
        player_01_Score.text = sP1.ToString();

        //Update player 2 score (two-player layout only)
        player_01_PanelTwo_Score.text = sP1.ToString();
        player_02_Score.text = sP2.ToString();
    }
}
