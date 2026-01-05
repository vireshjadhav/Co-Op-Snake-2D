using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance { get; private set; }

    private List<SnakeController> snakes = new List<SnakeController>();


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public void RegisterSnake (SnakeController s)
    {
        if (!snakes.Contains(s)) 
            snakes.Add(s);
    }

    public void UnregisterSnake (SnakeController s)
    {
        if (snakes.Contains(s))
            snakes.Remove(s);
    }

    public  IReadOnlyList<SnakeController> GetAllSnakes()
    {
        return snakes.AsReadOnly();
    }
}
