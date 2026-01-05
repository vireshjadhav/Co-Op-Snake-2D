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

        Debug.Log($"{s.name} Register");
    }

    //Unregister a SnakeController when it is disabled/destroyed.
    public void UnregisterSnake (SnakeController s)
    {
        if (snakes.Contains(s))
            snakes.Remove(s);
    }

    //Returns a read-only snapshot view of current registered snakes.
    public  IReadOnlyList<SnakeController> GetAllSnakes()
    {
        return snakes.AsReadOnly();
    }
}
