using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the game grid, spawns consumable items (food/poison) and power-ups,
/// converts grid coordinates to world positions, and resolves snake <-> item pickups.
/// 
/// Important notes:
/// - This class does not handle snake movement or physics; it only spawns items and reports collisions
///   via CheckSnakeItemCollisions called by SnakeController after a head move.
/// - Several fields (power-up prefabs, foodPrefab, poisonPrefab) must be assigned in the Inspector.
/// - The script intentionally preserves existing field and method names (including typos like isGreedCentered
///   and TryGetPowerUpAtGridPow) to avoid modifying behaviour. These names could be cleaned up later for readability.
/// </summary>
public class LevelGridController : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private SnakeController snakeController;


    //Prefabs for itmes spawned on the grid
    [Header("Prefabs")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private GameObject poisonPrefab;

    //Prefabs for each power-up type. If null, that type will never be considered as a spawn candidate.
    [Header("Power-Up Prefabs")]
    [SerializeField] private GameObject shieldPowerUpPrefab;
    [SerializeField] private GameObject scoreBoostPowerUpPrefab;
    [SerializeField] private GameObject speedUpPowerUpPrefab;

    //Grid dimensions (width x height). Grid coordinates use integer cells in [0, width-1] x [0, height-1].
    [Header("Grid Settings")]
    [SerializeField] public int width = 20;
    [SerializeField] public int height = 20;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool isGridCentered = true;  //When true, bottomLeftOrigin is computed so the grid is centered at this GameObject's transform position.
    private Vector3 bottomLeftOrigin;
    [Tooltip("If true, moving off one edge will make the snake appear on the opposite edge")]
    [SerializeField] public bool isWrapAround = true; //Enable/disable screen wrap.


    [Header("Food Setting")]
    [SerializeField] private float foodSpawnInterval = 1f; //Time between attempts to spawn food (if no food currently on grid).
    private Vector2Int foodGridPosition = new Vector2Int(-1, -1); //Tracks runtime state for the currently spawned food instance.
    private GameObject foodObject;
    private float foodSpawnTimer= 0f;
    private bool isFoodOnGrid = false;
    [SerializeField]private float foodDestroyDelayTime = 50f; //If food stays uncollected this long, it is automatically destroyed.
    private Coroutine foodDestroyCoroutine;


    [Header("Poison Setting")]
    [SerializeField] private float poisonSpawnInterval = 5f;   //Time between attempts to spawn poison (if no poison currently on grid).
    private Vector2Int poisonGridPosition = new Vector2Int(-1, -1);  //Tracks runtime state for the currently spawned poison instance.
    private GameObject poisonObject;
    private float poisonSpawnTimer = 0f;
    private bool isPoisonOnGrid = false;
    [SerializeField] private float poisonDestroyDelayTime = 50f;  //If food stays uncollected this long, it is automatically destroyed.
    private Coroutine poisonDestroyCoroutine;

    //Randomized spawn window between min and max determines when the system will try to spawn one power-up.
    [Header("Power-Up Spawn Settings")]
    [Tooltip("Min time between spawn checks.")]
    [SerializeField] private float powerUpMinSpawnInterval = 5f;
    [Tooltip("Max time between spawn checks.")]
    [SerializeField] private float powerUpMaxSpawnInterval = 15f;

    //How long a spawned power-up remains on the grid before being auto-removed.
    [Tooltip("How long a spawned power-up stays on the grid before disappearing")]
    [SerializeField] private float powerUpLifetime = 10f;

    //Cooldown per power-up TYPE after collection; prevents the same type from appearing immediately again.
    [Tooltip("Cooldown time (seconds) after a power-up is collected before that TYPE can spawn again.")]
    [SerializeField] private float PowerUpCooldown = 20f;

    //Active power-ups currently on the grid, indexed by type.
    private Dictionary<PowerUpType, GameObject> activePowerUps = new Dictionary<PowerUpType, GameObject>();
    //Active power-up grid positions, used by collision checks and GetAllFreeCells.
    private Dictionary<PowerUpType, Vector2Int> activePowerUpGridPos = new Dictionary<PowerUpType, Vector2Int>();

    //When each power-up type was last collected. Used to enforce per-type cooldowns.
    private Dictionary<PowerUpType, float> lastCollectedTime = new Dictionary<PowerUpType, float>();

    //Internal timers for randomized power-up spawn scheduling.
    private float powerUpSpawnTimer = 0f;
    private float powerUpNextSpawnTime = 0f;

    private void Awake()
    {
        //If isGreedCentered is true, the GameObject transform position is treated as the grid center.
        //Otherwise the transform position is treated as the bottom-left origin.
        if (isGridCentered)
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

    //Start is called before the first frame update
    private void Start()
    {
        //Sanity checks for required prefabs
        if (foodPrefab == null) Debug.LogError("LevelGridController: foodPrefab not assigned.");
        if (poisonPrefab == null) Debug.LogError("LevelGridController: poisonPrefab not assigned.");

        //Make power-ups spawn quickly while testing
        powerUpSpawnTimer = 0f;
        powerUpNextSpawnTime = Random.Range(powerUpMinSpawnInterval, powerUpMaxSpawnInterval);
        Debug.Log($"PowerUp will start spawning between {powerUpMinSpawnInterval} - {powerUpMaxSpawnInterval} seconds.");
    }

    private void Update()
    {
        //Continuously evaluate spawn timers for food/poison and power-ups.
        FoodAndPoisonSpawnHandler();
        PowerUpSpawnHandler();
    }

    #region Food And Poison

    /// <summary>
    /// Increment timers and spawn food/poison when their respective timers reach threshold
    /// and there isn't already an item of that type on the grid.
    /// </summary>
    private void FoodAndPoisonSpawnHandler()
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

    /// <summary>
    /// Spawn food at a random free cell. If there is no free cell, do nothing.
    /// Starts a delayed coroutine to destroy the food after foodDestroyDelayTime seconds (if uncollected).
    /// </summary>
    private void SpawnFood()
    {
        if (foodPrefab == null) return;

        //Cancel any pending destroy coroutine for food (safety).
        if (foodDestroyCoroutine != null)
        {
            StopCoroutine(foodDestroyCoroutine);
            foodDestroyCoroutine = null;
        }

        Vector2Int spawnFoodCell = PickRandomFreeCell();

        if (spawnFoodCell.x < 0)
        {
            Debug.LogWarning("LevelGridController.SpawnFood: No Free Cell to spawn Food.");
            if (GameController.instance != null && snakeController != null)
            {
                GameController.instance.UpdateGameWonState(snakeController, true);
            }
            return;
        }
        else
        {
            if (GameController.instance != null && snakeController != null)
            {
                GameController.instance.UpdateGameWonState(snakeController, false);
            }
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

        //Schedule automatic removal if uncollected after configured delay.
        foodDestroyCoroutine = StartCoroutine(DelayDestroyFood(foodDestroyDelayTime));
    }

    //Coroutine used to auto-destroy food after a delay.
    private IEnumerator DelayDestroyFood(float delay)
    {
        yield return new WaitForSeconds(delay);
        DestroyFood();
    }

    /// <summary>
    /// Remove current food (cancel coroutine, destroy instance, reset state).
    /// Safe to call even if no food is present.
    /// </summary>
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

    /// <summary>
    /// Spawn poison similar to SpawnFood. Separate timers and instance tracking are used so poison/spawn intervals are independent.
    /// </summary>
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
        //Schedule automatic removal if uncollected after configured delay.
        poisonDestroyCoroutine = StartCoroutine(DelayDestroyPoison(poisonDestroyDelayTime));
    }

    //Coroutine used to auto-destroy poison after a delay.
    private IEnumerator DelayDestroyPoison(float delay)
    {
        yield return new WaitForSeconds(delay);
        DestroyPoison();
    }

    /// <summary>
    /// Remove current poison instance and reset poison tracking state.
    /// </summary>
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

    #endregion

    #region Power Ups
    /// <summary>
    /// Timer-driven handler that attempts to spawn a power-up at a randomized interval.
    /// The actual type chosen respects three constraints:
    ///  1) its prefab is assigned,
    ///  2) it is not already on the grid,
    ///  3) the last collection time for that type is older than PowerUpCooldown.
    /// </summary>
    private void PowerUpSpawnHandler()
    {
        powerUpSpawnTimer += Time.deltaTime;
        if (powerUpSpawnTimer >= powerUpNextSpawnTime)
        {
            SpawnRandomPowerUps();
            powerUpSpawnTimer = 0;
            powerUpNextSpawnTime = Random.Range(powerUpMinSpawnInterval, powerUpMaxSpawnInterval);
        }
    }

    /// <summary>
    /// Build a candidate list of power-up types that may spawn and pick one at random.
    /// Instantiates the chosen prefab and records its grid cell and GameObject reference.
    /// Also schedules automatic removal after powerUpLifetime.
    /// </summary>
    private void SpawnRandomPowerUps()
    {
        List<PowerUpType> candidate = new List<PowerUpType>();
        float now = Time.time;

        //Local helper to check whether a type is eligible to be added as a candidate.
        void AddCandidate(PowerUpType type, GameObject prefab)
        {
            if (prefab == null) return;  // No prefab assigned, cannot spawn this type.

            // If an active instance exists for this type, do not add.
            if (activePowerUps.ContainsKey(type) && activePowerUps[type] != null) return;

            // If this type was collected recently, enforce cooldown.
            if (lastCollectedTime.TryGetValue(type, out float lastTime))
            {
                if (now - lastTime < PowerUpCooldown)
                    return;
            }

            candidate.Add(type);
        }

        //Check each power-up type using the helper (order does not matter).
        AddCandidate(PowerUpType.Shield, shieldPowerUpPrefab);
        AddCandidate(PowerUpType.SpeedUp, speedUpPowerUpPrefab);
        AddCandidate(PowerUpType.ScoreBoost, scoreBoostPowerUpPrefab);

        if (candidate.Count == 0) return; //Nothing allowed to spawn now

        //Choose a random type from allowed candidates.
        PowerUpType chosenType = candidate[Random.Range(0, candidate.Count)];

        GameObject prefab = null;
        switch (chosenType)
        {
            case PowerUpType.Shield:
                prefab = shieldPowerUpPrefab;
                break;

            case PowerUpType.SpeedUp:
                prefab = speedUpPowerUpPrefab;
                break;


            case PowerUpType.ScoreBoost:
                prefab = scoreBoostPowerUpPrefab;
                break;
        }

        if (prefab == null) return;  //Defensive guard; should not happen because AddCandidate checked null.

        // Attempt to place the power-up on a free cell.
        Vector2Int spawnCell = PickRandomFreeCell();
        if (spawnCell.x < 0)
        {
            Debug.LogWarning("LevelGridController: No free cell for Power-Up.");
            return;
        }

        Vector3 worldPos = GridToWorld(spawnCell);
        GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
        go.name = $"PowerUp_{chosenType}";

        // Ensure the spawned object has a PowerUp component with the correct type/duration.
        PowerUp pu = go.GetComponent<PowerUp>();
        if (pu == null)
        {
            pu = go.AddComponent<PowerUp>();
        }
        pu.powerUpType = chosenType;

        // Record active instance and grid position for lookup and collision detection.
        activePowerUps[chosenType] = go;
        activePowerUpGridPos[chosenType] = spawnCell;
        
        // Schedule auto-destruction of this spawned power-up after the configured lifetime.
        if (powerUpLifetime > 0f)
        {
            StartCoroutine(DestroyPowerUpAfterDelay(chosenType, powerUpLifetime));
        }

        Debug.Log($"Spawned PowerUp {chosenType} at {powerUpLifetime}");
    }

    // Coroutine wrapper to destroy a particular power-up after delay.  
    private IEnumerator DestroyPowerUpAfterDelay(PowerUpType type, float delay)
    {
        yield return new WaitForSeconds(delay);
        DestroyPowerUp(type, false);
    }

    /// <summary>
    /// Remove power-up of the given type from the grid.
    /// If 'collected' is true, record the collection timestamp to enforce cooldowns.
    /// </summary>
    private void DestroyPowerUp(PowerUpType type, bool collected)
    {
        if (activePowerUps.TryGetValue(type, out GameObject go))
        {
            if (go != null) Destroy(go);
        }

        activePowerUps.Remove(type);
        activePowerUpGridPos.Remove(type);

        if (collected)
        {
            lastCollectedTime[type] = Time.time;
        }
    }

    /// <summary>
    /// Lookup helper: returns true if a power-up exists at the requested grid cell.
    /// Note: method name TryGetPowerUpAtGridPow is retained verbatim from existing code.
    /// </summary>
    private bool TryGetPowerUpAtGridPos(Vector2Int gridPos, out PowerUpType type, out GameObject go)
    {
        foreach (var kvp in activePowerUpGridPos)
        {
            if (kvp.Value == gridPos)
            {
                type = kvp.Key;
                go = activePowerUps[type];
                return true; 
            }
        }

        type = default;
        go = null;
        return false;
    }
    #endregion

    #region Grid Helpers

    /// <summary>
    /// Convert integer grid coordinate -> world position using bottomLeftOrigin and cellSize.
    /// The z channel is preserved from bottomLeftOrigin.z so you can adjust layer depth via transform.
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return bottomLeftOrigin + new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, bottomLeftOrigin.z);
    }

    //Return whether the provided grid coordinate lies within [0,width-1] x [0,height-1].
    public bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    /// <summary>
    /// Choose a random cell from the list of currently free cells (not occupied by snakes/items/power-ups).
    /// Returns (-1,-1) when no free cell exists.
    /// </summary>
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


    /// <summary>
    /// Build a full list of free cells by:
    ///  - collecting occupied cells from all snakes
    ///  - adding any existing food/poison/power-up positions
    ///  - returning the complement set of all grid coords
    /// 
    /// Uses GameController.instance.GetAllSnakes() when available; otherwise falls back to FindObjectsOfType.
    /// </summary>
    private List<Vector2Int> GetAllFreeCells()
    {
        var occupiedCells = new HashSet<Vector2Int>();

        // Prefer GameController singleton (keeps a single source of truth).
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
            // Fallback: discover snakes directly in the scene.
            var snakes = FindObjectsOfType<SnakeController>();

            foreach (var s in snakes)
            {
                if (s == null) continue;
                var occ = s.GetOccupiedGridPositions();

                if (occ == null) continue;
                foreach (var p in occ) occupiedCells.Add(p);
            }
        }

        // Mark food and poison cells as occupied if the objects are present.
        if (foodObject != null) occupiedCells.Add(foodGridPosition);
        if (poisonObject != null) occupiedCells.Add(poisonGridPosition);

        // Mark all active power-ups as occupied.
        foreach (var kvp in activePowerUpGridPos)
        {
            occupiedCells.Add(kvp.Value);
        }

        // Build free list by checking all grid coordinates.
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

    #endregion

    #region Collision Check

    /// <summary>
    /// Called by SnakeController after the head has moved to 'snakeGridPosition'.
    /// If an item exists at that grid location, apply its effect and clean up the spawned object.
    /// Order of checks:
    ///  - food -> grow
    ///  - poison -> shrink
    ///  - power-ups -> ActivatePowerUP on the snake and remove power-up with cooldown record
    /// </summary>
    public void CheckSnakeItemCollisions(SnakeController snake, Vector2Int snakeGridPosition)
    {
        if (snake == null) return;


        //Food pickup
        if (snakeGridPosition == foodGridPosition && foodObject != null)
        {
            DestroyFood();

            snake.SnakeGrow(snake.GetSnakeBodyGrowSize());

            snake.FoodCollection();
        }


        //Poison pickup
        if (snakeGridPosition == poisonGridPosition && poisonObject != null)
        {
            DestroyPoison();

            snake.SnakeShrink(snake.GetSnakeBodyShrinkSize());

            snake.PoisonCollection();
        }


        //Power-up pickup 
        if (TryGetPowerUpAtGridPos(snakeGridPosition, out PowerUpType type, out GameObject powerUpObj) && powerUpObj != null)  
        {
            PowerUp p = powerUpObj.GetComponent<PowerUp>();
            float duration = (p != null) ? p.duration : 3f;

            // Activate the effect directly on the snake (SnakeController handles timers/effects).
            snake.ActivatePowerUP(type, duration);

            // Destroy the power-up and record the collection time for cooldown enforcement.
            DestroyPowerUp(type, true);
        }
    }

    #endregion
}
