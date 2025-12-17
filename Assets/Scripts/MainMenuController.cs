using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles main menu input and sets the game mode
/// before loading the gameplay scene.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public Button player_01_Button;
    public Button player_02_Button;

    private int gameSceneBuildIndex = 1;

    public static bool isTwoPlayerModeOn = false;


    //Initialize button event listeners on Awake
    private void Awake()
    {
        if (player_01_Button != null) player_01_Button.onClick.AddListener(ChoosePlayerOneControlScheme);
        if (player_02_Button != null) player_02_Button.onClick.AddListener(ChoosePlayerTwoControlScheme);
    }

    //Sets single-player mode and loads the gameplay scene
    private void  ChoosePlayerOneControlScheme()
    {
        isTwoPlayerModeOn = false;
        LoadGameScene();
    }


    //Sets two-player mode and loads the gameplay scene
    private void ChoosePlayerTwoControlScheme()
    {
        isTwoPlayerModeOn = true;
        LoadGameScene();
    }

    //Load the gameplay scene with fallback to scene index 1 if the configured index is invalid
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

    //Removes button event listeners to prevent memory leaks
    private void OnDestroy()
    {
        if (player_01_Button != null) player_01_Button.onClick.RemoveListener(ChoosePlayerOneControlScheme);
        if (player_02_Button != null) player_02_Button.onClick.RemoveListener(ChoosePlayerTwoControlScheme);
    }
}
