using UnityEngine;

public class LevelGridController : MonoBehaviour
{
    //[Header("References")]
    //[SerializeField] SnakeController snakeController;

    [Header("Grid")]
    [SerializeField] public int width = 20;
    [SerializeField] public int height = 20;
    [SerializeField] private float cellsize = 1f;

    //[Header("Snake Spawn (set in Inspector)")]
    //private Vector2Int[] snakeSpawnGridPosition;
    //private Vector2Int spawnDirPlayer1 = Vector2Int.right;
    //private Vector2Int spawnDirPlayer2 = Vector2Int.left;

    //[Header("Snake Prefab")]
    //[SerializeField] private GameObject snakePrefab;

    // Start is called before the first frame update
    void Start()
    {
        //if (snakePrefab == null) Debug.LogError("LevelGridController: snakeController not assigned.");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x > 0 && pos.x < width -1 && pos.y > 0 && pos.y < height -1;
    }
}
