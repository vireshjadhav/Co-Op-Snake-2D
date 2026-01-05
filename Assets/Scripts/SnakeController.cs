using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls snake movement, body layout, sprite orientation, power-ups and collisions.
/// The script uses a grid-based movement system paired with LevelGridController for world conversions.
/// </summary>
public class SnakeController : MonoBehaviour
{
    //Set player scheme (for 2-player Co-Op)
    public enum ControlScheme { Player_01, Player_02 };


    [Header("Snake Settings")]
    [Tooltip("Starting grid cell for the snake's head. " +
             "This should be set for enough from the edges when wrapAround is OFF," +
             "so the initial body layout does not spawn outside the grid.")]
    [SerializeField] private Vector2Int gridPosition = new Vector2Int(10, 10); // initial position of snake
    [SerializeField] private float gridMoveMaxTimer = 0.5f; //Time between each grid movement. Lower = faster snake.
    [Tooltip("Initial number of body segments.\n" +
        "Note: If 'Start Moving On Awake' is FALSE, this value is automatically forced to 1(head + tail only)")]
    [SerializeField] private int snakeBodySize = 1;  //Initial number of body segments (excluding head visual, logic only)
    [Tooltip("If Body Grow size is below 1 it will be forced to 1")]
    [SerializeField] private int snakeBodyGrowSize = 1;  //How many segments to grow at a time (per grow event)
    [Tooltip("If Body Shrink size is below 1 it will be forced to 1")]
    [SerializeField] private int snakeBodyShrinkSize = 1; //How many segments to shrink at a time (per shrink event)
    [SerializeField] private int tailHistoryLimit = 10; //How many previous tail positions we remember for smoother growth placement


    [Header("Input")]
    public ControlScheme controlScheme = ControlScheme.Player_01; //Toggle between input controller scheme (WASD or Arrow keys)


    [Header("Start Direction")] //Choose whether snake starts moving automatically or waits for input
    [Tooltip("If FALSE, SnakeBodySize initially Forced to 1")]
    [SerializeField] private bool startMovingOnAwake = true; //If true, snake starts moving automatically, if fasle, it waits for player input. 
    [SerializeField] private Vector2Int defaultStartDirection = new Vector2Int(1, 0); //Default movement direction at the start(right).


    [Header("References")]
    [SerializeField] private LevelGridController levelGridController; //Handles grid, bounds, and GridToWorld conversion.
    [SerializeField] private GameObject bodySegmentPrefab; //Prefab for body segements.
    [SerializeField] private GamePlayUIController gamePlayUIController;


    [Header("Animators")]
    [Tooltip("Animator for green snake (Player 01)")]
    [SerializeField] private RuntimeAnimatorController greenSnakeAnimator;
    [Tooltip("Animator for blue snake (Player 02)")]
    [SerializeField] private RuntimeAnimatorController blueSnakeAnimator;
    [SerializeField] private Animator headAnimator;   //Reference to the snake head's Animator Component
    [SerializeField] private float headHitAnimationDuration = 0.5f;


    [Header("Sprites (head handled by rotation onthe head GameObject)")]
    [Tooltip("Single straight body sprite (rotate for horizontal/vertical)")]
    [SerializeField] private Sprite bodyStraightSprite;
    [Tooltip("Corner sprites: assume each is drawn in a consistent orientation")]
    [SerializeField] private Sprite cornerRightUpSprite;
    [SerializeField] private Sprite cornerLeftUpSprite;
    [SerializeField] private Sprite cornerLeftDownSprite;
    [SerializeField] private Sprite cornerRightDownSprite;
    [Tooltip("Single tail sprite (will rotate automatically)")]
    [SerializeField] private Sprite tailSprite;


    [Header("Power-Up Settings")]
    [Tooltip("How long the shield stays active.")]
    [SerializeField] private float shieldDurationDefault = 3f;

    [Tooltip("How long the score boost stays active.")]
    [SerializeField] private float scoreBoostDurationDefault = 3f;

    [Tooltip("How long the speed boost stays active.")]
    [SerializeField] private float speedUpDurationDefault = 3f;

    [Tooltip("Multiplier for speed up. < 1 = faster snake.")]
    [SerializeField] private float speedUpFactor = 0.5f;


    [Header("Visual Effects")]
    [SerializeField] private DizzyStarsEffect dizzyStarsEffect;

    //Powr-up state
    private bool shieldActive;
    private float shieldTimer;
    private bool scoreBoostActive;
    private float scoreBoostTimer;
    private bool speedUpActive;
    private float speedUpTimer;
    private float baseGridMoveMaxTimer;

    //Public Power-Up flags for other systems (like LevelGridController)
    public bool IsShieldActive => shieldActive;
    public bool IsScoreBoostActive => scoreBoostActive;
    public bool IsSpeedUpActive => speedUpActive;
    public float ScoreMultiplier => scoreBoostActive ? 2f : 1f;

    //Movement / body management
    private List<Vector2Int> snakeMovePositionList;  //Store position of snake 
    private List<GameObject> bodySegments; //Game object of body segments, sync with snakeMovePositionList by index
    private int minBodySize = 1; //Minimum allowed body size
    private Vector2Int lastVacatedTailCell; //Last Position where the tail was before it moved
    private List<Vector2Int> tailHistory = new List<Vector2Int>(); //Recent tail position (from newest to oldest). Used when spawning new segments on grow.


    private float gridMoveTimer = 0f; //Timer to handle movement intervals.
    private bool isAlive = false; //Flag to check whether snake is currently alive or dead.
    private Vector2Int nextMoveDirection; //requested direction from input (applied on next grid move)
    private Vector2Int gridMoveDirection;  //current move direction (applied every grid step)
    private float spriteDirectionAngle = 90f;  //Used to store last valid sprite angle for the head.


    //Initializes essential components and validates references on object creation
    private void Awake()
    {
        //Initialize list (Safety)
        if (snakeMovePositionList == null) snakeMovePositionList = new List<Vector2Int>();
        if (bodySegments == null) bodySegments = new List<GameObject>();
    }


    // Start is called before the first frame update
    void Start()
    {
        //Register snake to GameController
        if (GameController.instance != null) GameController.instance.RegisterSnake(this);


        if (levelGridController == null)
        {
            Debug.LogError($"{name}: LevelGridController not assigned.");
            enabled = false;
            return;
        }

        if (MainMenuController.isTwoPlayerModeOn)
        {
            if (controlScheme == ControlScheme.Player_01)
            {
                gridPosition = new Vector2Int(3, 10);
                gridMoveDirection = Vector2Int.right;
            }

            if (controlScheme == ControlScheme.Player_02)
            {
                gridPosition = new Vector2Int(16, 10);
                gridMoveDirection = Vector2Int.left;
            }
        }
        else
        {
            //gridPosition = new Vector2Int(10, 10);
            gridMoveDirection = defaultStartDirection;
        }

        //Initialize animator 
        InitializeAnimator();

        //Save original speed for SpeedUp revert
        baseGridMoveMaxTimer = gridMoveMaxTimer;

        //Start movement timer full so snake moves after one interval
        gridMoveTimer = gridMoveMaxTimer;

        //Ensure initial body size is at least the minimum.
        snakeBodySize = Mathf.Max(minBodySize, snakeBodySize);

        //Warn if wrapAround is off but we have no grid controller (cannot check bounds)
        if (!levelGridController.isWrapAround && levelGridController == null) Debug.LogWarning($"{name}: wrapAround is false but levelGridController not assigned.");
        //Warn if we have no visual prefab (Logic still works, but no body visuals)
        if (bodySegmentPrefab == null) Debug.LogWarning($"{name}: bodySegmentPrefab not assigned. Snake will grow logically but no visuals.");

        //Ensure grow/shrink size are at least 1
        if (snakeBodyGrowSize < 1) snakeBodyGrowSize = 1;
        if (snakeBodyShrinkSize < 1) snakeBodyShrinkSize = 1;

        isAlive = true;

        // Set starting movement
        if (startMovingOnAwake && gridMoveDirection == Vector2Int.zero)
            gridMoveDirection = defaultStartDirection; //Auto-start moving
        else
            gridMoveDirection = Vector2Int.zero; //Snake will wait for input

        //Next direction initially matches current direction.
        nextMoveDirection = gridMoveDirection;

        //Force body size to 1 if snake is not moving on start
        if (!startMovingOnAwake) snakeBodySize = 1;

        //Direction used only for placing initia body layout
        Vector2Int layoutDirection = (gridMoveDirection != Vector2Int.zero) ? gridMoveDirection : (defaultStartDirection != Vector2Int.zero ? defaultStartDirection : Vector2Int.right);

        //Position head in world
        if (levelGridController != null)
            transform.position = levelGridController.GridToWorld(gridPosition);
        else
            transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);

        //Rotate head
        spriteDirectionAngle = GetAngleFromDirection(layoutDirection);
        transform.eulerAngles = new Vector3(0f, 0f, spriteDirectionAngle);

        //Build initial body layout
        BuildInitialBodyLayout(layoutDirection);

        //Spawn visual body segments
        if (bodySegmentPrefab != null)
        {
            for (int i = 0; i < snakeBodySize; i++)
            {
                Vector2Int segGrid = snakeMovePositionList[i];
                Vector3 world = (levelGridController != null) ? levelGridController.GridToWorld(segGrid) : new Vector3(segGrid.x, segGrid.y, 0f);
                GameObject seg = Instantiate(bodySegmentPrefab, world, Quaternion.identity);
                seg.name = $"Body_{i}";
                bodySegments.Add(seg);
            }
            UpdateBodySprite();
        }

        //If wrapAround disabled, verify initial Layout is inside grid
        if (!levelGridController.isWrapAround && levelGridController != null)
        {
            bool layoutOutside = false;

            if (!levelGridController.IsInsideGrid(gridPosition))
            {
                layoutOutside = true;
            }

            for (int i = 0; i < snakeMovePositionList.Count; i++)
            {
                if (!levelGridController.IsInsideGrid(snakeMovePositionList[i]))
                {
                    layoutOutside = true;
                }
            }

            if (layoutOutside)
            {
                Debug.Log($"{name}: Initial snake layout is outside the grid. " +
                    "Check gridPosition, snakeBodySize, defaultStartDirection or enable wrapAround.");
                OnFatalObstacleHit();
                return;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Check if game is over before processing anything
        if (GameController.instance != null && GameController.instance.IsGameOver) return;

        if (!isAlive) return; //freeze input when dead
        HandleInput();  //Read player input
        HandleGridMovement(); //Move snake on grid-based timer
        HandlePowerUpTimers(); //Update power-up durations
    }

    #region Helper

    //Return current grow amount for further features
    public int GetSnakeBodyGrowSize() => snakeBodyGrowSize;

    //Return current shrink amount for further features
    public int GetSnakeBodyShrinkSize() => snakeBodyShrinkSize;

    #endregion

    #region Input

    //Read input and set nextDirection (buffered). Do not directly override gridMoveDirection here.
    private void HandleInput()
    {

        bool allowWASD = false;
        bool allowArrows = false;

        if (MainMenuController.isTwoPlayerModeOn)
        {
            if (controlScheme == ControlScheme.Player_01)
                allowWASD = true;
            if (controlScheme == ControlScheme.Player_02)
                allowArrows = true;
        }
        else
        {

            allowWASD = true;
            allowArrows = true;
        }

        if (allowWASD)
        {
            if (Input.GetKeyDown(KeyCode.W)) TrySetDirection(new Vector2Int(0, 1));
            if (Input.GetKeyDown(KeyCode.A)) TrySetDirection(new Vector2Int(-1, 0));
            if (Input.GetKeyDown(KeyCode.S)) TrySetDirection(new Vector2Int(0, -1));
            if (Input.GetKeyDown(KeyCode.D)) TrySetDirection(new Vector2Int(1, 0));
        }

        if (allowArrows)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) TrySetDirection(new Vector2Int(0, 1));
            if (Input.GetKeyDown(KeyCode.LeftArrow)) TrySetDirection(new Vector2Int(-1, 0));
            if (Input.GetKeyDown(KeyCode.DownArrow)) TrySetDirection(new Vector2Int(0, -1));
            if (Input.GetKeyDown(KeyCode.RightArrow)) TrySetDirection(new Vector2Int(1, 0));
        }


        if (gridMoveDirection == Vector2Int.zero && nextMoveDirection != Vector2Int.zero)
        {
            //We are about to start moving from a stopped state, pre update the sprite for the new direction
            UpdateBodySprite();
        }
        
    }

    //Prevent 180° turn and buffer direction
    private void TrySetDirection(Vector2Int desired)
    {
        //If desired is reverse of current move, ignore
        if (desired == -gridMoveDirection) return;
        //If desired is same as current, nothing to change
        if (desired == gridMoveDirection) return;
        //Buffer it, it will be applied on the next movement tick
        nextMoveDirection = desired;
    }

    #endregion

    #region Power-Ups Handling

    /// <summary>
    /// Called by LevelGridController when a pick-up is collected.
    /// Activates the appropriate state and timers on the snakes.
    /// </summary>
    public void ActivatePowerUP(PowerUpType type, float duration)
    {
        switch (type)
        {
            case PowerUpType.Shield:
                shieldActive = true;
                shieldTimer = duration > 0 ? duration : shieldDurationDefault;
                break;

            case PowerUpType.ScoreBoost:
                scoreBoostActive = true;
                scoreBoostTimer = duration > 0 ? duration : scoreBoostDurationDefault;
                break;

            case PowerUpType.SpeedUp:
                if (!speedUpActive)
                {
                    baseGridMoveMaxTimer = gridMoveMaxTimer;
                }
                speedUpActive = true;
                speedUpTimer = duration > 0 ? duration : speedUpDurationDefault;

                if (speedUpFactor <= 0f) speedUpFactor = 0.5f;
                gridMoveMaxTimer = baseGridMoveMaxTimer * speedUpFactor;
                break;
        }
        Debug.Log($"{name} activated power-up: {type} for {duration} seconds");
    }

    //Upadate timers each frame and disable effects when timers end
    private void HandlePowerUpTimers()
    {
        float dt = Time.deltaTime;

        if (shieldActive)
        {
            shieldTimer -= dt;
            if (shieldTimer <= 0f)
            {
                shieldActive = false;
                shieldTimer = 0f;
            }
        }

        if (scoreBoostActive)
        {
            scoreBoostTimer -= dt;
            if (scoreBoostTimer <= 0f)
            {
                scoreBoostActive = false;
                scoreBoostTimer = 0f;
            }
        }

        if (speedUpActive)
        {
            speedUpTimer -= dt;
            if (speedUpTimer <= 0f)
            {
                speedUpActive = false;
                speedUpTimer = 0f;
                gridMoveMaxTimer = baseGridMoveMaxTimer;  //Back to normal speed
            }
        }
    }


    //Use and remove shield once, returns true if shield consumed and the hit should be ignored.
    private bool TryConsumeShield()
    {
        //Always trigger hit Animation on collission, regartless of Shiedl status
        TriggerHeadHitAnimation();


        // Play shield hit sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(Sounds.ShieldBlock);
        }

        if (!shieldActive) return false;

        shieldActive = false;
        shieldTimer = 0f;
        Debug.Log($"{name} shield consumed!");

        //Reset head to head collision flag when shield prevents death
        if (GameController.instance != null && GameController.instance.IsHeadToHeadCollision())
        {
            GameController.instance.ResetHeadToHeadCollision();
            Debug.Log($"Reset head to head collision flag after shield consumption");
        }

        //This ensures sprites are Properly oriented after Collision
        UpdateBodySprite();
        return true;
    }

    #endregion

    #region Body Layout And Movement

    //Create initial body positions behind the head, and set up tail history
    private void BuildInitialBodyLayout(Vector2Int layoutDirection)
    {
        if (layoutDirection == Vector2Int.zero)
        {
            layoutDirection = Vector2Int.right;
        }
        snakeMovePositionList.Clear();

        //Place segments one-by-one behind the head along layoutDirection.
        for (int i = 1; i <= snakeBodySize; i++)
        {
            Vector2Int segPos = gridPosition - layoutDirection * i;

            if (levelGridController.isWrapAround && levelGridController != null)
            {
                segPos = WrapPosition(segPos);
            }
            snakeMovePositionList.Add(segPos);
        }


        //Set starting LastVacatedTailCell based on the last segment
        if (snakeMovePositionList.Count > 0)
        {
            Vector2Int tailGrid = snakeMovePositionList[snakeMovePositionList.Count - 1];
            Vector2Int behindTail = tailGrid - layoutDirection;

            if (levelGridController.isWrapAround && levelGridController != null)
            {
                behindTail = WrapPosition(behindTail);
            }

            lastVacatedTailCell = behindTail;
        }
        else
        {
            //If no body segments, use cell behind the head.
            Vector2Int behindHead = gridPosition - layoutDirection;
            if (levelGridController.isWrapAround && levelGridController != null)
            {
                behindHead = WrapPosition(behindHead);
            }

            lastVacatedTailCell = behindHead;
        }

        //Fill  tailHistory with the latest few segment position (from tail backward)
        tailHistory.Clear();
        int available = snakeMovePositionList.Count;
        int limit = Mathf.Min(tailHistoryLimit, available);

        for (int i = 0; i < limit; i++)
        {
            tailHistory.Add(snakeMovePositionList[available - 1 - i]);
        }
    }


    //Handle snake movement based on time and gridMoveMaxTimer
    private void HandleGridMovement()
    {
        gridMoveTimer += Time.deltaTime;
        if (gridMoveTimer >= gridMoveMaxTimer)
        {
            gridMoveTimer -= gridMoveMaxTimer;

            if (gridMoveDirection == Vector2Int.zero && nextMoveDirection == Vector2Int.zero) return;

            Vector2Int previousHead = gridPosition;

            //Apply buffered direction at the moment of movement.
            if (nextMoveDirection != gridMoveDirection) gridMoveDirection = nextMoveDirection;

            //Remember where the tail was before we move
            Vector2Int tailBefore = (snakeMovePositionList != null && snakeMovePositionList.Count > 0) ? snakeMovePositionList[snakeMovePositionList.Count - 1] : (gridPosition - gridMoveDirection);

            //move by the current gridMoveDirection (may be zero if still waiting)
            gridPosition += gridMoveDirection;
            Vector2Int newPos = WrapPosition(gridPosition);

            //if wrapArround is disabled and we go out of bounds, trigger fatal obstacle hit
            if (!levelGridController.isWrapAround && levelGridController != null && !levelGridController.IsInsideGrid(gridPosition))
            {
                //Trigger hit animation for wall collision
                TriggerHeadHitAnimation();

                //ALWAYS trigger dizzy stars effect on collision
                TriggerDizzyEffect();

                //Cancel this move: stay on previous cell
                gridPosition = previousHead;

                transform.position = levelGridController.GridToWorld(gridPosition);

                //Try shield first
                if (TryConsumeShield())
                {
                    //Stop current movement, player can choose a new direction
                    gridMoveDirection = Vector2Int.zero;
                    nextMoveDirection = Vector2Int.zero;
                    return;
                }
                // Stop movement and trigger fatal obstacle hit
                gridMoveDirection = Vector2Int.zero;
                nextMoveDirection = Vector2Int.zero;
                OnFatalObstacleHit();
                return;
            }

            gridPosition = newPos;  //ensure internal position matches final applied position

            if (levelGridController != null)
                transform.position = levelGridController.GridToWorld(gridPosition);
            else
                transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);

            bool collisionHandled = HandleSelfAndSnakeCollisions(gridPosition, previousHead, tailBefore);
            if (collisionHandled)
            {
                return;
            }

            //Insert previous head position as first body segment position.
            snakeMovePositionList.Insert(0, previousHead);

            //Move Head in world space
            if (levelGridController != null)
                transform.position = levelGridController.GridToWorld(gridPosition);
            else
                transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);


            //If list is shorter than body size, extend using old tail position
            while (snakeMovePositionList.Count < snakeBodySize)
            {
                snakeMovePositionList.Add(tailBefore);
            }

            //Update LastVacatedTailCell and tail histoty
            lastVacatedTailCell = tailBefore;

            tailHistory.Insert(0, tailBefore);
            if (tailHistory.Count > tailHistoryLimit)
            {
                tailHistory.RemoveAt(tailHistory.Count - 1);
            }

            //Trim extra position so list length matches snakeBodySize
            if (snakeMovePositionList.Count > snakeBodySize)
            {
                snakeMovePositionList.RemoveRange(snakeBodySize, snakeMovePositionList.Count - snakeBodySize);
            }

            //Move body segments to match updated grid positions
            UpdateBodyPositions();

            //Rotate head sprite to match move Direction.
            spriteDirectionAngle = GetAngleFromDirection(gridMoveDirection);
            transform.eulerAngles = new Vector3(0, 0, spriteDirectionAngle);

            //Update sprites (Straight / Corner / tail) and rotations
            UpdateBodySprite();


            if (levelGridController != null)
            {
                levelGridController.CheckSnakeItemCollisions(this, gridPosition);
            }
        }
    }


    //Update body segment world positions based on snakeMovePositionList
    private void UpdateBodyPositions()
    {
        for (int i = 0; i < bodySegments.Count; i++)
        {
            if (i >= bodySegments.Count) continue;
            if (bodySegments[i] == null) continue;

            //Use recorded position if available, else fall back to head position
            Vector2Int segGridPos = (i < snakeMovePositionList.Count) ? snakeMovePositionList[i] : gridPosition;

            Vector3 world = levelGridController != null ? levelGridController.GridToWorld(segGridPos) : new Vector3(segGridPos.x, segGridPos.y, 0);

            bodySegments[i].transform.position = world;
        }
    }

    //Convert direction to angle for head rotation
    private float GetAngleFromDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return 180f;
        if (direction == Vector2Int.down) return 0f;
        if (direction == Vector2Int.left) return 270f;
        if (direction == Vector2Int.right) return 90f;
        return spriteDirectionAngle;
    }


    //Handle wrapping around the grid, if enabled
    private Vector2Int WrapPosition(Vector2Int pos)
    {
        if (levelGridController == null || !levelGridController.isWrapAround) return pos;

        int w = levelGridController.width;
        int h = levelGridController.height;

        int x = pos.x;
        int y = pos.y;

        //Wrap X coordinate
        if (w > 0)
        {
            x = ((x % w) + w) % w;
        }

        //Wrap Y coordinate
        if (h > 0)
        {
            y = ((y % h) + h) % h;
        }

        return new Vector2Int(x, y);

    }

    #endregion

    #region Death / Collisions

    //Called when snake hits a fatal obstacle (wall or another snake)
    private void OnFatalObstacleHit()
    {
        if (!isAlive) return;

        // Play death sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(Sounds.PlayerDeath);
        }

        Die();
    }

    //Start death Process (delayed destruction)
    private void Die()
    {
        if (!isAlive) return; //Prevent multiple death calls

        isAlive = false;
        gridMoveDirection = Vector2Int.zero;
        nextMoveDirection = Vector2Int.zero;

        //Play death sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play(Sounds.PlayerDeath);
        }

        if (GameController.instance != null)
        {
            GameController.instance.UpdateGameLoseState(this, true);
        }

        StartCoroutine(DelayDestruction(2f));
    }

    //Delay before destroying snake GameObject (for death effects)
    private IEnumerator DelayDestruction(float delay)
    {
        yield return new WaitForSeconds(delay);

        //Ensure snake is completely stopped
        StopSnake();

        gameObject.SetActive(false);
        DestroyAllBodySegments();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StopSnake()
    {
        isAlive = false;
        gridMoveDirection = Vector2Int.zero;
        nextMoveDirection = Vector2Int.zero;

        StopAllCoroutines();
    }

    //Get all grid Positions occupied by the snake (head + body)
    public List<Vector2Int> GetOccupiedGridPositions()
    {
        List<Vector2Int> occupied = new List<Vector2Int>();
        occupied.Add(gridPosition); //Head
        for (int i = 0; i < snakeMovePositionList.Count && i < snakeBodySize; i++)
        {
            occupied.Add(snakeMovePositionList[i]); //Body
        }

        return occupied;
    }


    //Get the direction from position 'a' to position 'b', taking wrapAround into account
    private Vector2Int DirectionFromTo(Vector2Int a, Vector2Int b)
    {
        Vector2Int delta = b - a;

        if (levelGridController.isWrapAround && levelGridController != null)
        {
            int w = levelGridController.width;
            int h = levelGridController.height;

            //Adjust for the shortest wrapped distance on X
            if (w > 0)
            {
                if (delta.x > w / 2) delta.x -= w;
                if (delta.x < -w / 2) delta.x += w;
            }

            //Adjust for the shortest wrapped distance on Y
            if (h > 0)
            {
                if (delta.y > h / 2) delta.y -= h;
                if (delta.y < -h / 2) delta.y += h;
            }
        }

        //If smae cell, no direction
        if (delta.x == 0 && delta.y == 0) return Vector2Int.zero;


        //Choose the dominant axis (horizontal vs vertical)
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return new Vector2Int(Mathf.Clamp(delta.x, -1, 1), 0);
        }
        else if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
        {
            return new Vector2Int(0, Mathf.Clamp(delta.y, -1, 1));
        }

        return Vector2Int.zero;

    }

    //Returns true if collision was handled (move cancelled or snake died) and caller should stop processing the move.
    private bool HandleSelfAndSnakeCollisions(Vector2Int newHeadPos, Vector2Int previousHead, Vector2Int tailBefore)
    {

        //Self-collision: check own body
        int checkCount = Mathf.Min(snakeMovePositionList.Count, snakeBodySize);
        for (int i = 0; i < checkCount; i++)
        {
            var pos = snakeMovePositionList[i];

            if (pos == tailBefore)
                continue;

            if (pos == newHeadPos)
            {
                Debug.Log($"{name}: Self-collision at {newHeadPos}");

                //Trigger hit animation for any collision
                TriggerHeadHitAnimation();

                //Always trigger dizzy stats effect on collision
                TriggerDizzyEffect();

                //Cancel this move: stay on previous cell
                gridPosition = previousHead;
                if (levelGridController != null)
                    transform.position = levelGridController.GridToWorld(gridPosition);
                else
                    transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);

                //If shield available, consume it and cancel the move
                if (TryConsumeShield())
                {
                    Debug.Log($"Shield consumed successfully, cancelling move");

                    //Stop current movement, player can choose a new direction
                    gridMoveDirection = Vector2Int.zero;
                    nextMoveDirection = Vector2Int.zero;

                    return true;
                }

                //Stop movement and trigger fatal obstacle hit
                gridMoveDirection = Vector2Int.zero;
                nextMoveDirection = Vector2Int.zero;
                OnFatalObstacleHit();  //No shield - fatal obstacle hit
                return true;
            }
        }

        //Collision with other snakes(Co-Op). Check all other snakes' occupied cell.
        if (GameController.instance == null) return false;

        var allSnakes = GameController.instance.GetAllSnakes();

        foreach (var other in allSnakes)
        {
            if (other == null || other == this) continue;

            List<Vector2Int> otherOccupied = other.GetOccupiedGridPositions();
            if (otherOccupied.Count == 0) continue;

            Vector2Int otherHeadPos = otherOccupied[0];
            Debug.Log("Snake:  " + this.gridPosition + "other snake head pos: " + otherOccupied[0]);

            if (otherHeadPos == newHeadPos)
            {
                Debug.Log("Head to head collision");

                //Trigger hit animation for head to head collision
                TriggerHeadHitAnimation();

                //Always trigger dizzy stats effect on collision
                TriggerDizzyEffect();

                if (other != null)
                {
                    other.TriggerHeadHitAnimation();
                    other.TriggerDizzyEffect();
                }

                GameController.instance.SetHeadToHeadCollision();

                //Cancel this move: stay on previous cell
                gridPosition = previousHead;
                if (levelGridController != null)
                    transform.position = levelGridController.GridToWorld(gridPosition);
                else
                    transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);

                //Shield consumes and cancels move
                if (TryConsumeShield())
                {
                    Debug.Log($"Shield consumed successfully, cancelling move");

                    //Other snake should die as this snake has shield
                    if(GameController.instance != null && other != null)
                    {
                        GameController.instance.UpdateGameLoseState(other, true);
                    }

                    //Stop current movement, player can choose a new direction
                    gridMoveDirection = Vector2Int.zero;
                    nextMoveDirection = Vector2Int.zero;
                    return true;
                }

                //Stop movement and trigger fatal obstacle hit
                gridMoveDirection = Vector2Int.zero;
                nextMoveDirection = Vector2Int.zero;

                //Use gamecontroller to handle head to head collision
                if (GameController.instance != null)
                {
                    GameController.instance.HandleHeadToHeadCollision(this, other);
                }
                else
                {
                    //Fallback
                    OnFatalObstacleHit();
                }
                return true;
            }

            //Fallback: scene query if GameController isn't present
            for (int i = 0; i < otherOccupied.Count; i++)
            {
                if (otherOccupied[i] != newHeadPos) continue;

                Debug.Log($"{name}: collided (fallback) with snake '{other.name}' at {newHeadPos}");

                //Trigger hit animation
                TriggerHeadHitAnimation();

                //Always trigger dizzy stats effect on collision
                TriggerDizzyEffect();

                //Cancel this move: stay on previous cell
                gridPosition = previousHead;
                if (levelGridController != null)
                    transform.position = levelGridController.GridToWorld(gridPosition);
                else
                    transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);

                if (TryConsumeShield())
                {
                    Debug.Log($"Shield consumed successfully, cancelling move");

                    //Stop current movement, player can choose a new direction
                    gridMoveDirection = Vector2Int.zero;
                    nextMoveDirection = Vector2Int.zero;
                    return true;
                }

                //Stop movement and trigger fatal obstacle hit
                gridMoveDirection = Vector2Int.zero;
                nextMoveDirection = Vector2Int.zero;
                OnFatalObstacleHit();
                return true;
            }
        }
        return false; //no collision handled
    }

    #endregion

    #region Grow / Shrink / Sprites


    /// <summary>
    ///  Increase snake length by 'amount segments.
    ///  Uses tailHistory to place new segments where the tail recently was,
    ///  so the growth looks smooth and natural.
    /// </summary>
    public void SnakeGrow(int amount)
    {
        //do not grow more than we have tail history for
        amount = Mathf.Min(amount, tailHistoryLimit);
        if (amount <= 0) return;

        for (int i = 0; i < amount; i++)
        {
            Vector2Int spawnGridPos;

            //prefer using positions from tailHistory
            if (i < tailHistory.Count)
            {
                spawnGridPos = tailHistory[i];
            }
            else
            {
                //Fallback: place new segment behind the last segemnt
                Vector2Int fallbackDir = (gridMoveDirection != Vector2Int.zero) ? -gridMoveDirection : Vector2Int.right;
                spawnGridPos = snakeMovePositionList[snakeMovePositionList.Count - 1] + fallbackDir;
            }

            //Convert grid to world position
            Vector3 spawnWorldPos = (levelGridController != null) ? levelGridController.GridToWorld(spawnGridPos) : new Vector3(spawnGridPos.x, spawnGridPos.y, 0f);
            GameObject newSegment;
            if (bodySegmentPrefab != null)
            {
                newSegment = Instantiate(bodySegmentPrefab, spawnWorldPos, Quaternion.identity);
            }
            else
            {
                newSegment = new GameObject("BodySegment", typeof(SpriteRenderer));
                newSegment.transform.position = spawnWorldPos;
                newSegment.transform.SetParent(transform, worldPositionStays: true);
            }

            newSegment.name = $"Body_{bodySegments.Count}";
            var sr = newSegment.GetComponent<SpriteRenderer>();

            if (sr == null)
            {
                sr = newSegment.AddComponent<SpriteRenderer>();
            }

            sr.sortingOrder = 5;

            if (bodyStraightSprite != null)
            {
                sr.sprite = bodyStraightSprite;
            }
            Debug.Log("Name: " + newSegment.name + " Position: " + newSegment.transform.position);
            bodySegments.Add(newSegment);
            snakeMovePositionList.Add(spawnGridPos);
            snakeBodySize++;
        }
        //Update lists and size
        UpdateBodyPositions();
        UpdateBodySprite();
    }


    /// <summary>
    /// Decrease snake length by 'amount' segments, removing from the tail side.
    /// Will not shrink below minBodySize.
    /// </summary>
    public void SnakeShrink(int amount)
    {
        //Cannot shrink below minimum size
        amount = Mathf.Min(amount, snakeBodySize - minBodySize);
        if (amount <= 0) return;

        for (int i = 0; i < amount; i++)
        {
            if (bodySegments.Count <= minBodySize) break;
            if (snakeBodySize <= minBodySize) break;

            int lastIndex = bodySegments.Count - 1;
            GameObject seg = bodySegments[lastIndex];

            //Remove last segment GameObject
            bodySegments.RemoveAt(lastIndex);
            Destroy(seg);

            //Decrease logical size, but not below minimum
            snakeBodySize = Mathf.Max(minBodySize, snakeBodySize - 1);

            //Remove extra position from snakeMovePositionList if it exists
            if (snakeMovePositionList.Count > snakeBodySize)
            {
                snakeMovePositionList.RemoveAt(snakeMovePositionList.Count - 1);
            }
        }
        //Keep visuals and flags in sync
        UpdateBodyPositions();
        UpdateBodySprite();
    }

    /// <summary>
    /// Update sprite and rotations for each body segment
    /// to show straight parts, corners, and tail with correct orientation.
    /// </summary>
    private void UpdateBodySprite()
    {

        //Determine which direction to use calculations
        Vector2Int effctiveMoveDirection = gridMoveDirection;
        if (effctiveMoveDirection == Vector2Int.zero && nextMoveDirection != Vector2Int.zero)
        {
            //If we're stopped but have a queued direction, use that for sprite orientation
            effctiveMoveDirection = nextMoveDirection;
        }

        for (int i = 0; i < bodySegments.Count; i++)
        {
            if (bodySegments[i] == null) continue;

            var seg = bodySegments[i];
            var sr = seg.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            sr.sortingOrder = 5;


            //Get current segment grid position(or head position as fallback)
            Vector2Int currentPos;
            if (i < snakeMovePositionList.Count)
            {
                currentPos = snakeMovePositionList[i];
            }
            else
            {
                currentPos = gridPosition;
            }

            //Handle tail sprite (last segment)
            if (i == bodySegments.Count - 1)
            {
                sr.sprite = tailSprite;

                Vector2Int tailDir = Vector2Int.zero;

                if (i > 0 && i - 1 < snakeMovePositionList.Count)
                {
                    //Direction from tail towards segment before it
                    Vector2Int segmentBefore = (i - 1 < snakeMovePositionList.Count) ? snakeMovePositionList[i - 1] : gridPosition;
                    tailDir = DirectionFromTo(currentPos, segmentBefore);
                }
                else if (snakeMovePositionList.Count > 0)
                {
                    //If no valid segment before, use head direction.
                    tailDir = DirectionFromTo(currentPos, gridPosition);
                }

                //If tailDir is zero(when stopped), use opposite of head movement direction
                if (tailDir == Vector2Int.zero && gridMoveDirection != Vector2Int.zero)
                {
                    tailDir = -effctiveMoveDirection;
                }

                float angle = 0f;

                if (tailDir == Vector2Int.up) angle = 90f;
                else if (tailDir == Vector2Int.down) angle = 270f;
                else if (tailDir == Vector2Int.left) angle = 180f;
                else if (tailDir == Vector2Int.right) angle = 0f;

                seg.transform.eulerAngles = new Vector3(0, 0, angle);
                continue;
            }


            //Non-tail segments: decide straight/corner sprites
            Vector2Int posCloserToHead = (i == 0) ? gridPosition : snakeMovePositionList[i - 1];
            Vector2Int posFurtherFromHead;

            if (i + 1 < snakeMovePositionList.Count)
            {
                posFurtherFromHead = snakeMovePositionList[i + 1];
            }
            else if (i + 1 < bodySegments.Count)
            {
                posFurtherFromHead = currentPos;
            }
            else
            {
                posFurtherFromHead = currentPos;
            }

            //Incoming direction: from this segment towards head
            Vector2Int incoming = DirectionFromTo(currentPos, posCloserToHead);

            //Outgoing direction: from this segment towards tail
            Vector2Int outgoing = DirectionFromTo(currentPos, posFurtherFromHead);

            if (incoming == Vector2Int.zero && i >0)
            {
                //When stopped, incoming direction should be from current to head
                incoming = DirectionFromTo(currentPos, gridPosition);
            }

            if (outgoing == Vector2Int.zero && i < bodySegments.Count -1)
            {
                outgoing = DirectionFromTo(currentPos, snakeMovePositionList[snakeMovePositionList.Count - 1]);
            }

            //If incoming and outgoing points opposite, it's straight peice
            if (incoming == -outgoing)
            {
                sr.sprite = bodyStraightSprite;
                if (incoming.x != 0)
                {
                    //Horizontal straight (Left-Right)
                    seg.transform.eulerAngles = new Vector3(0, 0, 90f);
                }
                else
                {
                    //Vertical straight(Up-Down)
                    seg.transform.eulerAngles = Vector3.zero;
                }
            }
            else
            {
                //Corners based on incoming/outgoing directions
                if ((incoming == Vector2Int.right && outgoing == Vector2Int.up) || (incoming == Vector2Int.up && outgoing == Vector2Int.right))
                {
                    sr.sprite = cornerRightUpSprite;
                    seg.transform.eulerAngles = Vector3.zero;
                }
                else if ((incoming == Vector2Int.right && outgoing == Vector2Int.down) || (incoming == Vector2Int.down && outgoing == Vector2Int.right))
                {
                    sr.sprite = cornerRightDownSprite;
                    seg.transform.eulerAngles = Vector3.zero;
                }
                else if ((incoming == Vector2Int.left && outgoing == Vector2Int.down) || (incoming == Vector2Int.down && outgoing == Vector2Int.left))
                {
                    sr.sprite = cornerLeftDownSprite;
                    seg.transform.eulerAngles = Vector3.zero;
                }
                else if ((incoming == Vector2Int.left && outgoing == Vector2Int.up) || (incoming == Vector2Int.up && outgoing == Vector2Int.left))
                {
                    sr.sprite = cornerLeftUpSprite;
                    seg.transform.eulerAngles = Vector3.zero;
                }
                else
                {
                    //Fallback to straight sprite if corner doesn't match any pattern.
                    sr.sprite = bodyStraightSprite;
                    if (incoming != Vector2Int.zero)
                    {
                        if (incoming.x != 0)
                        {
                            seg.transform.eulerAngles = new Vector3(0, 0, 90f);
                        }
                        else
                        {
                            seg.transform.eulerAngles = Vector3.zero;
                        }
                    }
                    else
                    {
                        seg.transform.eulerAngles = new Vector3(0, 0, 90f);
                    }
                }

            }
        }
    }

    public void FoodCollection()
    {
        if (GameController.instance == null) return;

        GameController.instance.AddScore(this, scoreBoostActive);

        if (gamePlayUIController != null) gamePlayUIController.UpdateScore();

    }

    public void PoisonCollection()
    {
        if (GameController.instance == null) return;

        GameController.instance.DeductScore(this);
        if (gamePlayUIController != null) gamePlayUIController.UpdateScore();

    }


    /// <summary>
    /// Destroys all visual body segments and clears internal position lists.
    /// Called when the snake dies.
    /// </summary>
    private void DestroyAllBodySegments()
    {
        foreach (var seg in bodySegments)
        {
            if (seg != null)
                seg.SetActive(false);
        }

        bodySegments.Clear();
        snakeMovePositionList?.Clear();
        tailHistory?.Clear();
    }

    #endregion

    #region Particle System

    /// <summary>
    /// Triggers the dizzy stars effect when snake collides with anything
    /// </summary>
    private void TriggerDizzyEffect()
    {
        if (dizzyStarsEffect != null)
        {
            Debug.Log($"{name}: Starting dizzy stars effect on collision");
            dizzyStarsEffect.StartEffect(transform); 
        }
        else
        {
            Debug.Log($"{name}: dizzyStarsEffect is NULL!");
        }
    }

    #endregion

    #region Animator

    /// <summary>
    /// Initialize the appropriate animator based on control scheme
    /// </summary>
    private void InitializeAnimator()
    {
        //Try to get the animator component if not already assigned
        if (headAnimator == null)
        {
            headAnimator = GetComponent<Animator>();
            if (headAnimator == null)
            {
                //Try to find it in the children in not on this GameObject
                headAnimator = GetComponentInChildren<Animator>();
            }
        }

        if (headAnimator != null)
        {
            //Assign the appropriate animator controller based on control scheme
            if (controlScheme == ControlScheme.Player_01 && greenSnakeAnimator != null)
            {
                headAnimator.runtimeAnimatorController = greenSnakeAnimator;
                Debug.Log($"${name}: Assigned Green Snake Animator");
            }
            else if (controlScheme == ControlScheme.Player_02 && blueSnakeAnimator != null)
            {
                headAnimator.runtimeAnimatorController = blueSnakeAnimator;
                Debug.Log($"${name}: Assigned Blue Snake Animator");
            }
            else
            {
                Debug.LogWarning($"${name}: Missing animator controller for {controlScheme}");
            }


            //Debut: Print all states are availablel
            Debug.Log($"Animator Controller assigned: {headAnimator.runtimeAnimatorController.name}");
            Debug.Log($"Layer count: {headAnimator.layerCount}");

            // Get all state names (this requires reflection or checking manually)
            // For now, just check what state we start in
            var stateInfo = headAnimator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"Starting state hash: {stateInfo.fullPathHash}");

            // Common state names to try
            string[] possibleIdleNames = { "Idle", "SnakeIdle", "GreenSnakeIdle", "BlueSnakeIdle", "Base Layer" };
            foreach (var name in possibleIdleNames)
            {
                if (stateInfo.IsName(name))
                {
                    Debug.Log($"Starting state is: {name}");
                    break;
                }
            }
        }
        else
        {
            Debug.LogWarning($"{name}: No Animator component found on snake head");
        }


    }

    /// <summary>
    /// Triggers the head hit animation
    /// </summary>
    public void TriggerHeadHitAnimation()
    {
        if (headAnimator != null && headAnimator.isActiveAndEnabled)
        {
            headAnimator.SetTrigger("HeadHit");
            Debug.Log($"{name}: Triggered Head Hit animation");


            StartCoroutine(ReturnToIdleAfterDelay(headHitAnimationDuration));
        }
        else
        {
            Debug.LogWarning($"{name}: No Animator found or animator is disabled");
        }
    }


    /// <summary>
    /// Returns to idle state 
    /// </summary>
    private IEnumerator ReturnToIdleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (headAnimator != null && headAnimator.isActiveAndEnabled)
        {
            headAnimator.ResetTrigger("HeadHit");
            headAnimator.SetTrigger("Idle");
            Debug.Log($"{name}:  Resetting HeadHit trigger - letting state machine handle transition");
        }
    }

    #endregion

    private void OnDisable()
    {
        //Unregister from GameController when this snake is disabled.
        if (GameController.instance != null)
        {
            GameController.instance.UnregisterSnake(this);
        }
    }
}

