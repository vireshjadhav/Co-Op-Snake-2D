using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGridController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private GameObject poisonPrefab;

    [Header("Grid")]
    [SerializeField] public int width = 20;
    [SerializeField] public int height = 20;
    [SerializeField] private float cellSize = 1f;

    [SerializeField] private bool isGreedCentered = true;
    private Vector3 bottomLeftOrigin;

    [Header("Food Setting")]
    [SerializeField] private float foodSpawnInterval = 1f;
    private Vector2Int foodGridPosition = new Vector2Int(-1, -1);
    private GameObject foodObject;
    private float foodSpawnTimer= 0f;
    private bool isFoodOnGrid = false;
    [SerializeField]private float foodDestroyDelayTime = 50f;
    private Coroutine foodDestroyCoroutine;


    [Header("Poison Setting")]
    [SerializeField] private float poisonSpawnInterval = 5f;
    private Vector2Int poisonGridPosition = new Vector2Int(-1, -1);
    private GameObject poisonObject;
    private float poisonSpawnTimer = 0f;
    private bool isPoisonOnGrid = false;
    [SerializeField] private float poisonDestroyDelayTime = 50f;
    private Coroutine poisonDestroyCoroutine;


    private void Awake()
    {
        if (isGreedCentered)
        {
            float offsetX = (width - 1) / 2f * cellSize;
            float offsetY = (height - 1) / 2f * cellSize;
            bottomLeftOrigin = transform.position - new Vector3(offsetX, offsetY, 0);
        }
        else
        {
            bottomLeftOrigin = transform.position;
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (foodPrefab == null) Debug.LogError("LevelGridController: foodPrefab not assigned.");
        if (poisonPrefab == null) Debug.LogError("LevelGridController: poisonPrefab not assigned.");
    }

    private void Update()
    {
        if (!isFoodOnGrid)
        {
            foodSpawnTimer += Time.deltaTime;
            if (foodSpawnTimer >= foodSpawnInterval)
            {
                SpawnFood();
                foodSpawnTimer = 0f;
            }
        }

        if (!isPoisonOnGrid)
        {
            poisonSpawnTimer += Time.deltaTime;
            if (poisonSpawnTimer >= poisonSpawnInterval)
            {
                SpawnPoison();
                poisonSpawnTimer = 0f;
            }
        }
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return bottomLeftOrigin + new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, bottomLeftOrigin.z);
    }

    public bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width  && pos.y >= 0 && pos.y < height;
    }

    private void SpawnFood()
    {
        if (foodPrefab == null) return;

        if (foodDestroyCoroutine != null)
        {
            StopCoroutine(foodDestroyCoroutine);
            foodDestroyCoroutine = null;
        }

        Vector2Int spawnFoodCell = PickRandomFreeCell();

        if (spawnFoodCell.x < 0)
        {
            Debug.LogWarning("LevelGridController.SpawnFood: No Free Cell to spawn Food.");
            return;
        }

        if (foodObject != null)
        {
            Destroy(foodObject);
            foodObject = null;
        }

        foodGridPosition = spawnFoodCell;

        Vector3 worldPos = GridToWorld(foodGridPosition);

        foodObject = Instantiate(foodPrefab, worldPos, Quaternion.identity);

        foodObject.name = "Food";

        isFoodOnGrid = true;

        foodDestroyCoroutine = StartCoroutine(DelayDestroyFood(foodDestroyDelayTime));
    }

    private IEnumerator DelayDestroyFood(float delay)
    {
        yield return new WaitForSeconds(delay);
        DestroyFood();
    }

    private void DestroyFood()
    {
        if (foodDestroyCoroutine != null)
        {
            StopCoroutine(foodDestroyCoroutine);
            foodDestroyCoroutine = null;
        }
        if (foodObject != null)
        {
            Destroy(foodObject);
            foodObject = null;
        }

        foodGridPosition = new Vector2Int(-1, -1);
        isFoodOnGrid = false;
    }

    private void SpawnPoison()
    {
        if (poisonPrefab  == null) return;

        if (poisonDestroyCoroutine != null)
        {
            StopCoroutine(poisonDestroyCoroutine);
            poisonDestroyCoroutine = null;
        }

        Vector2Int spawnPoisonCell = PickRandomFreeCell();

        if (spawnPoisonCell.x < 0)
        {
            Debug.LogWarning("LevelGridController.SpawnPoison: No Free Cell to Spawn Poison.");
            return;
        }

        poisonSpawnTimer = 0f;

        if (poisonObject != null)
        {
            Destroy(poisonObject);
            poisonObject = null;
        }

        poisonGridPosition = spawnPoisonCell;

        Vector3 worldPos = GridToWorld(poisonGridPosition);

        poisonObject = Instantiate(poisonPrefab, worldPos, Quaternion.identity);

        poisonObject.name = "Poison";

        isPoisonOnGrid = true;
        poisonDestroyCoroutine = StartCoroutine(DelayDestroyPoison(poisonDestroyDelayTime));
    }

    private IEnumerator DelayDestroyPoison(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Delay: " + delay);
        DestroyPoison();
    }

    private void DestroyPoison()
    {
        if (poisonDestroyCoroutine != null)
        {
            StopCoroutine(poisonDestroyCoroutine);
            poisonDestroyCoroutine = null;
        }

        if (poisonObject != null)
        {
            Destroy(poisonObject);
            poisonObject = null;
        }

        poisonGridPosition = new Vector2Int(-1, -1);
        isPoisonOnGrid= false;
    }

    private Vector2Int PickRandomFreeCell()
    {
        var free = GetAllFreeCells();

        if (free.Count ==  0)
        {
            Debug.LogWarning("LevelGridController: No Free Cells Available.");
            return new Vector2Int(-1, -1);
        }

        return free[Random.Range(0, free.Count)];
    }

    private List<Vector2Int> GetAllFreeCells()
    {
        var occupiedCells = new HashSet<Vector2Int>();

        if (GameController.instance != null)
        {
            var all = GameController.instance.GetAllSnakes();

            foreach (var s in all)
            {
                if (s == null) continue;

                var occ = s.GetOccupiedGridPositions();

                if (occ == null) continue;

                foreach (var p in occ) occupiedCells.Add(p);
            }
        }
        else
        {
            var snakes = FindObjectsOfType<SnakeController>();

            foreach (var s in snakes)
            {
                if (s == null) continue;
                var occ = s.GetOccupiedGridPositions();

                if (occ == null) continue;
                foreach (var p in occ) occupiedCells.Add(p);
            }
        }

        if (foodObject != null) occupiedCells.Add(foodGridPosition);
        if (poisonObject != null) occupiedCells.Add(poisonGridPosition);

        List<Vector2Int> free = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (!occupiedCells.Contains(cell))
                {
                    free.Add(cell);
                }
            }
        }

        return free;
    }

    public void CheckSnakeItemCollisions(SnakeController snake, Vector2Int snakeGridPosition)
    {
        if (snake == null) return;

        if (snakeGridPosition == foodGridPosition && foodObject != null)
        {
            DestroyFood();

            snake.SnakeGrow(snake.GetSnakeBodyGrowSize());
        }

        if (snakeGridPosition == poisonGridPosition && poisonObject != null)
        {
            DestroyPoison();

            snake.SnakeShrink(snake.GetSnakeBodyShrinkSize());
        }
    }
}
