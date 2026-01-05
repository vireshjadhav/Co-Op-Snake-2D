using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

/// <summary>
/// Global Manager that keeps track of all active snakes in the scene.
/// Used for inter-snake collision checks and global queries.
/// </summary>
public class GameController : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private GameObject snakePlayer01;
    [SerializeField] private GameObject snakePlayer02;


    [SerializeField] private GameResultController gameResultController;


    [Header("Grid Image")]
    [SerializeField] private GameObject levelGrid20X20; 

    [Header("Score")]
    [SerializeField] private int foodScore = 10;
    [SerializeField] private int poisonPenalty = 5;
    [SerializeField] private int winPoints = 1000;

    private Dictionary<SnakeController, bool> playerWonState = new Dictionary<SnakeController, bool>(); 
    private Dictionary<SnakeController, bool> playerLoseState = new Dictionary<SnakeController, bool>();
    private Dictionary<SnakeController, int> playerScores = new Dictionary<SnakeController, int>();

    //Internal list of registered snakes.
    private List<SnakeController> snakes = new List<SnakeController>();

    private bool headToHeadCollision =false;
    private bool isGameOver = false;

    //Singleton instance
    public static GameController instance { get; private set; }

    //Game state properties
    public bool IsGameOver => isGameOver;

  
    private void Awake()
    {
        //Destroy duplicate instance GameObjecs.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    //Start is called before the first frame update
    private void Start()
    {
        ApplyGameMode();
    }

    //Activate/deactivates snake GameObjects based on selected game mode
    private void ApplyGameMode()
    {
        if (MainMenuController.isTwoPlayerModeOn)
        {
            snakePlayer01.SetActive(true);
            snakePlayer02.SetActive(true);
        }
        else
        {
            snakePlayer01.SetActive(true);
            snakePlayer02.SetActive(false);
        }
    }

    //Register a SnakeController so it is known to the GameController.
    public void RegisterSnake (SnakeController s)
    {
        if (s == null) return;
        if (!snakes.Contains(s)) 
            snakes.Add(s);


        if (!playerScores.ContainsKey(s))
            playerScores[s] = 0;

        if (!playerWonState.ContainsKey(s))
            playerWonState[s] = false;

        if (!playerLoseState.ContainsKey(s))
            playerLoseState[s] = false;

        Debug.Log($"{s.name} Register");
    }

    //Unregister a SnakeController when it is disabled/destroyed.
    public void UnregisterSnake (SnakeController s)
    {
        if (snakes.Contains(s))
            snakes.Remove(s);

        if (playerScores.ContainsKey(s))
            playerScores.Remove(s);

    }

    //Returns a read-only snapshot view of current registered snakes.
    public  IReadOnlyList<SnakeController> GetAllSnakes()
    {
        return snakes.AsReadOnly();
    }

    /// <summary>
    /// Updates the win state for a snake
    /// </summary>
    public void UpdateGameWonState(SnakeController snake, bool state)
    {
        if (state)
        {
            //Prevent duplicate calls
            if (playerWonState.ContainsKey(snake) && playerWonState[snake])
                return;

            playerWonState[snake] = true;
            Debug.Log($"{snake.name} marked as WIN");

            //If two player mode, the othere snake should lose
            if (MainMenuController.isTwoPlayerModeOn && snakes.Count >= 2)
            {
                //Find the other snake
                foreach (var otherSnakes in snakes)
                {
                    if (otherSnakes != null && otherSnakes != snake)
                    {
                        UpdateGameLoseState(otherSnakes, true);
                        break;
                    }
                }
            }

            //In any mode, when any snake win, end the game
            EndGame();
        }
        else
        {
            playerWonState[snake] = false;
        }
    }

    public void SetHeadToHeadCollision()
    {
        headToHeadCollision = true;
    }

    public bool IsHeadToHeadCollision()
    {
        return headToHeadCollision;
    }

    public bool GetPlayerGameWonState(SnakeController snake )
    {
        return playerWonState.TryGetValue(snake, out var state) ? state : false;
    }

    /// <summary>
    /// Updates the lose state for a snake
    /// </summary>
    public void UpdateGameLoseState(SnakeController snake, bool state)
    {
        if (state)
        {
            //Don't set lose state if already set
            if (playerLoseState.ContainsKey(snake) && playerLoseState[snake]) return;


            playerLoseState[snake] = true;
            Debug.Log($"{snake.name} marked as LOSE");


            //If two player mode, the other snake should win (if not already dead)
            if (MainMenuController.isTwoPlayerModeOn && snakes.Count == 2)
            {
                SnakeController otherSnake = null;

                foreach (var s in snakes)
                {
                    if (s != null && s != snake)
                    {
                        otherSnake = s;
                        break;
                    }
                }

                if (otherSnake != null && !GetPlayerGameLoseState(otherSnake))
                {
                    // Check if head-to-head collision
                    if (headToHeadCollision)
                    {
                        //Both snakes are still in play and this is a head to head collision
                        Debug.Log("Head-to-head collision detected");

                        // In head-to-head, winner is determined by score
                        int snakeScore = GetScore(snake);
                        int otherScore = GetScore(otherSnake);

                        if (snakeScore > otherScore)
                        {
                            // This snake has higher score, so it should win
                            Debug.Log($"{snake.name} has higher score ({snakeScore} vs {otherScore}) - switching win/lose");
                            playerLoseState[snake] = false; // Remove lose state
                            UpdateGameWonState(snake, true); // Set as winner
                            UpdateGameLoseState(otherSnake, true); // Other loses
                            return;
                        }
                        else if (otherScore > snakeScore)
                        {
                            // Other snake has higher score, so it wins
                            Debug.Log($"{otherSnake.name} has higher score ({otherScore} vs {snakeScore})");
                            UpdateGameWonState(otherSnake, true);
                            // Current snake already marked as lose
                        }
                        else
                        {
                            // Equal scores - both lose
                            Debug.Log("Head-to-head draw - both lose");
                            UpdateGameLoseState(otherSnake, true);
                        }
                    }
                    else
                    {
                        // Not a head-to-head collision or other snake is already dead
                        // Normal death - other snake wins (if it exists and is alive)
                        if (otherSnake != null && !GetPlayerGameLoseState(otherSnake))
                        {
                            Debug.Log($"Normal death - {otherSnake.name} wins by default");
                            UpdateGameWonState(otherSnake, true);
                        }
                    }
                }
            }

            else if (MainMenuController.isTwoPlayerModeOn && snakes.Count == 2)
            {
                if (!headToHeadCollision)
                {
                    SnakeController otherSnake = null;

                    foreach (var s in snakes)
                    {
                        if (s != null && s != snake)
                        {
                            otherSnake = s;
                            break;
                        }
                    }

                    if (otherSnake != null && !GetPlayerGameLoseState(otherSnake))
                    {
                        Debug.Log($"Normal death - {otherSnake.name} wins by default");
                        UpdateGameWonState(otherSnake, true);
                    }
                }
                else
                {
                    Debug.LogWarning($"headToHeadCollision flag is true but not in head-to-head logic block. Resetting flag.");
                    ResetHeadToHeadCollision();
                }
            }

            //In any mode, when a snake dies, end the game
            EndGame();
        }
        else
            playerLoseState[snake] = false;
    }

    public bool GetPlayerGameLoseState(SnakeController snake)
    {
        return playerLoseState.TryGetValue(snake, out var state) ? state : false;
    }

    public void AddScore(SnakeController snake, bool scoreBoost)
    {
        if (!playerScores.ContainsKey (snake)) return;

        int finalScore = foodScore;
        if (scoreBoost)
            finalScore *= 2;


        playerScores[snake] += finalScore;

        if (playerScores[snake] >= winPoints)
        {
            //Player reached target score, end game with win
            UpdateGameWonState(snake, true);
            SoundManager.Instance.Play(Sounds.Win);
        }
    }


    /// <summary>
    /// Special handling for head-to-head collisions
    /// </summary>
    public void HandleHeadToHeadCollision(SnakeController snake1, SnakeController snake2)
    {
        if (isGameOver) return;

        Debug.Log("Handling head-to-head collision");
        headToHeadCollision = true;

        //Check if either snake has shield active
        bool snake1HasShield = snake1.IsShieldActive;
        bool snake2HasShield = snake2.IsShieldActive;

        Debug.Log($"Shield status - {snake1.name}: {snake1HasShield}, {snake2.name}: {snake2HasShield}");

        if (snake1HasShield || snake2HasShield)
        {
            //One or both snakes have shields - handle shield logic
            if (snake1HasShield && !snake2HasShield)
            {
                //Snake1 has shield, snake2 should die
                Debug.Log($"{snake1.name} has shield, {snake2.name} should die");
                UpdateGameLoseState(snake2, true);
                ResetHeadToHeadCollision();   // Reset since collision was handled with shield
                return;
            }
            else if (!snake1HasShield && snake2HasShield)
            {
                //Snake2 has shield, snake1 should die
                Debug.Log($"{snake2.name} has shield, {snake1.name} should die");
                UpdateGameLoseState(snake1, true);
                ResetHeadToHeadCollision();   // Reset since collision was handled with shield
                return;
            }
            else if (snake1HasShield && snake2HasShield)
            {
                Debug.Log("Both snakes have shields - no death, game continues");
                ResetHeadToHeadCollision();
                return;
            }

        }
        else
        {
            //No shield - compare scores as normal
            int score1 = GetScore(snake1);
            int score2 = GetScore(snake2);

            Debug.Log($"Scores - {snake1.name}: {score1}, {snake2.name}: {score2}");

            if (score1 > score2)
            {
                // Snake1 wins
                UpdateGameWonState(snake1, true);
                UpdateGameLoseState(snake2, true);
            }
            else if (score2 > score1)
            {
                // Snake2 wins
                UpdateGameWonState(snake2, true);
                UpdateGameLoseState(snake1, true);
            }
            else
            {
                // Draw - both lose
                UpdateGameLoseState(snake1, true);
                UpdateGameLoseState(snake2, true);
            }
        }
    }


    /// <summary>
    /// Resets head-to-head collision flag
    /// <summary>
    public void ResetHeadToHeadCollision()
    {
        headToHeadCollision = false;
        Debug.Log("Reset head-to-head collision flag");
    }

    /// <summary>
    /// Ends the game and stops all snakes
    /// </summary>
    public void EndGame()
    {
        if (isGameOver) return; //Prevent multiple calls

        isGameOver = true;

        //Stop all snakes
        foreach (var snake in snakes)
        {
            if (snake != null)
            {
                snake.StopSnake();
            }
        }
    }

    public void DeductScore(SnakeController snake)
    {
        if (!playerScores.ContainsKey(snake)) return;

        playerScores[snake] = Mathf.Max(0, playerScores[snake] - poisonPenalty);
    }

    public int GetScore(SnakeController snake)
    {
        return playerScores.TryGetValue(snake, out int score) ? score : 0;
    }
}
