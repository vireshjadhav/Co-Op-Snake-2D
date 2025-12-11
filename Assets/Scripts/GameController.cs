using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global Manager that keeps track of all active snakes in the scene.
/// provides a read-only view of registered snakes for other systems.
/// </summary>

public class GameController : MonoBehaviour
{
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

    //Register a SnakeController so it is known to the GameController.
    public void RegisterSnake (SnakeController s)
    {
        if (s == null) return;
        if (!snakes.Contains(s)) 
            snakes.Add(s);
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
