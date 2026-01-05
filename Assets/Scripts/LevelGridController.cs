using UnityEngine;

public class LevelGridController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] SnakeController snakeController;
    [SerializeField] private GameObject snakePrefab;

    [Header("Grid")]
    [SerializeField] public int width = 20;
    [SerializeField] public int height = 20;
    [SerializeField] private float cellsize = 1f;

    [Header("Snake Spawn (set in Inspector)")]
    private Vector2Int[] snakeSpawnGridPosition;
    private Vector2Int spawnDirPlayer1 = Vector2Int.right;
    private Vector2Int spawnDirPlayer2 = Vector2Int.left;


    [SerializeField] private bool gridIsCentered = true;
    private Vector3 bottomLeftOrigin;

    private void Awake()
    {
        if (gridIsCentered)
        {
            float offsetX = (width - 1) / 2f * cellsize;
            float offsetY = (height - 1) / 2f * cellsize;
            bottomLeftOrigin = transform.position - new Vector3(offsetX, offsetY, 0);
        }
        else
        {
            bottomLeftOrigin = transform.position;
        }
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return bottomLeftOrigin + new Vector3(gridPos.x * cellsize, gridPos.y * cellsize, bottomLeftOrigin.z);
    }

    public bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width  && pos.y >= 0 && pos.y < height;
    }
}
