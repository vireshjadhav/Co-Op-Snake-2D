using System.Collections.Generic;
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


    [Header("Score")]
    [SerializeField] private int foodScore = 10;
    [SerializeField] private int poisonPenalty = 5;


    private Dictionary<SnakeController, int> playerScores = new Dictionary<SnakeController, int>();


    //Singleton instance
    public static GameController instance { get; private set; }

    //Internal list of registered snakes.
    private List<SnakeController> snakes = new List<SnakeController>();

        
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


    public void AddScore(SnakeController snake, bool scoreBoost)
    {
        if (!playerScores.ContainsKey (snake)) return;

        int finalScore = foodScore;
        if (scoreBoost)
            finalScore *= 2;


        playerScores[snake] += finalScore;

        Debug.Log($"{snake.name} score = {playerScores[snake]}");
    }


    public void DeductScore(SnakeController snake)
    {
        if (!playerScores.ContainsKey(snake)) return;

        playerScores[snake] = Mathf.Max(0, playerScores[snake] - poisonPenalty);

        Debug.Log($"{snake.name} score = {playerScores[snake]}");
    }

    public int GetScore(SnakeController snake)
    {
        return playerScores.TryGetValue(snake, out int score) ? score : 0;
    }

}
