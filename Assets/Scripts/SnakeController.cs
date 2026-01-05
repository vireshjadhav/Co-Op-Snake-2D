    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

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


    [Header("GamePlay")]
    [Tooltip("If true, moving off one edge will make the snake appear on the opposite edge")]
    [SerializeField] private bool wrapAround = true; //Enable/disable screen wrap.


    [Header("References")]
    [SerializeField] private LevelGridController levelGridController; //Handles grid, bounds, and GridToWorld conversion.
    [SerializeField] private GameObject bodySegmentPrefab; //Prefab for body segements.


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


    private void Awake()
    {
        //Initialize list (Safety)
        if (snakeMovePositionList == null) snakeMovePositionList = new List<Vector2Int>();
        if (bodySegments == null) bodySegments = new List<GameObject>();
    }

    private void OnEnable()
    {
        //Register snake to GameController
        if (GameController.instance != null) GameController.instance.RegisterSnake(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        //Start movement timer full so snake moves after one interval
        gridMoveTimer = gridMoveMaxTimer;

        //Ensure initial body size is at least the minimum.
        snakeBodySize = Mathf.Max(minBodySize, snakeBodySize);

        //Warn if wrapAround is off but we have no grid controller (cannot check bounds)
        if (!wrapAround && levelGridController == null) Debug.LogWarning($"{name}: wrapAround is false but levelGridController not assigned.");
        //Warn if we have no visual prefab (Logic still works, but no body visuals)
        if (bodySegmentPrefab == null) Debug.LogWarning($"{name}: bodySegmentPrefab not assigned. Snake will grow logically but no visuals.");

        //Ensure grow/shrink size are at least 1
        if (snakeBodyGrowSize < 1) snakeBodyGrowSize = 1;
        if (snakeBodyShrinkSize < 1) snakeBodyShrinkSize = 1;

        isAlive = true;

        // Set starting movement
        if (startMovingOnAwake) gridMoveDirection = defaultStartDirection; //Auto-start moving
        else gridMoveDirection = Vector2Int.zero; //Snake will wait for input

        //Next direction initially matches current direction.
        nextMoveDirection = gridMoveDirection;

        //Force body size to 1 if snake is not moving on start
        if (!startMovingOnAwake) snakeBodySize = 1;

        //Direction used only for placing initia body layout
        Vector2Int layoutDirection = (gridMoveDirection != Vector2Int.zero) ? gridMoveDirection : (defaultStartDirection != Vector2Int.zero ? defaultStartDirection : Vector2Int.right);

        //Position head in world
        if (levelGridController != null) transform.position = levelGridController.GridToWorld(gridPosition);
        else transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);

        //Rotate head
        spriteDirectionAngle = GetAngleFromDirection(layoutDirection);
        transform.eulerAngles = new Vector3(0f, 0f, spriteDirectionAngle);

        //Build initial body grid positions
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

        if (!wrapAround && levelGridController != null)
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
                OnHitWall();
                return;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isAlive) return; //freeze input when dead
        HandleInput();  //Read player input
        HandleGridMovement(); //Move snake on grid-based timer
    }

    //Return current grow amount for futher features
    public int GetSnakeBodyGrowSize() => snakeBodyGrowSize;

    //Return current shrink amount for futher features
    public int GetSnakeBodyShrinkSize() => snakeBodyShrinkSize;


    //Read input and set nextDirection (buffered). Do not directly override gridMoveDirection here.
    private void HandleInput()
    {
        if (controlScheme == ControlScheme.Player_01)
        {
            if (Input.GetKeyDown(KeyCode.W)) TrySetDirection(new Vector2Int(0, 1));
            if (Input.GetKeyDown(KeyCode.A)) TrySetDirection(new Vector2Int(-1, 0));
            if (Input.GetKeyDown(KeyCode.S)) TrySetDirection(new Vector2Int(0, -1));
            if (Input.GetKeyDown(KeyCode.D)) TrySetDirection(new Vector2Int(1, 0));
        }

        if (controlScheme == ControlScheme.Player_02)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) TrySetDirection(new Vector2Int(0, 1));
            if (Input.GetKeyDown(KeyCode.LeftArrow)) TrySetDirection(new Vector2Int(-1, 0));
            if (Input.GetKeyDown(KeyCode.DownArrow)) TrySetDirection(new Vector2Int(0, -1));
            if (Input.GetKeyDown(KeyCode.RightArrow)) TrySetDirection(new Vector2Int(1, 0));
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
            Vector2Int segPos = gridPosition - layoutDirection *i;

            if (wrapAround && levelGridController != null)
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

            if (wrapAround && levelGridController != null)
            {
                behindTail = WrapPosition(behindTail);
            }

            lastVacatedTailCell = behindTail;
        }
        else
        {
            //If no body segments, use cell behind the head.
            Vector2Int behindHead = gridPosition - layoutDirection;
            if (wrapAround && levelGridController != null)
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

            //if wrapArround is disabled and we go out of bounds, trigger wall hit/death
            if (!wrapAround && levelGridController != null && !levelGridController.IsInsideGrid(gridPosition))
            {
                // Stop movement and trigger wall hit
                gridMoveDirection = Vector2Int.zero;
                nextMoveDirection = Vector2Int.zero;
                OnHitWall();
            }

            gridPosition = newPos;  //ensure internal position matches final applied position

            //Move Head in world space
            if (levelGridController != null)
                transform.position = levelGridController.GridToWorld(gridPosition);
            else
                transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);

            //Insert previous head position as first body segment position.
            snakeMovePositionList.Insert(0, previousHead);

            //If list is shorter than body size, extend using old tail position
            while (snakeMovePositionList.Count < snakeBodySize)
            {
                snakeMovePositionList.Add(tailBefore);
            }

            //Update LastVacatedTailCell and tail Histoty
            lastVacatedTailCell = tailBefore;

            tailHistory.Insert(0, tailBefore);
            if (tailHistory.Count > tailHistoryLimit)
            {
                tailHistory.RemoveAt(tailHistory.Count -1);
            }

            //Trim extra position so list lenght matches snakeBodySize
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
        if (levelGridController == null || !wrapAround) return pos;

        int w = levelGridController.width;
        int h = levelGridController.height;

        int x = pos.x;
        int y = pos.y;

        //Wrap X coordinate
        if (w > 0 )
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

    //Called when snake hit a wall
    private void OnHitWall()
    {
        if (!isAlive) return;
        isAlive = false;
        gridMoveDirection = Vector2Int.zero;
        nextMoveDirection = Vector2Int.zero;

        //Run die Animation /sound.

        Die();
    }

    //Start death Process (delayed destruction)
    private void Die()
    {
        StartCoroutine(DelayDestruction(2f));
    }

    //Delay before destroying snake GameObject (for death effects)
    private IEnumerator DelayDestruction(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
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

        if (wrapAround && levelGridController != null)
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

                float angle = 0f;

                if (tailDir == Vector2Int.up) angle = 90f;
                else if (tailDir == Vector2Int.down) angle = 270f;
                else if (tailDir == Vector2Int.left) angle = 180f;
                else if (tailDir == Vector2Int.right) angle = 0f;

                seg.transform.eulerAngles = new Vector3(0, 0, angle);
                continue;
            }


            //Non-tail segments: decide staight/corner srites
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
                    seg.transform.eulerAngles = Vector3.zero;
                }

            }
        }
    }

    private void OnDisable()
    {
        //Unregister from GameController when this snake is disabled.
        if (GameController.instance != null)
        {
            GameController.instance.UnregisterSnake(this);
        }
    }
}
