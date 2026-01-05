using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    //Set player scheme (for 2-player Co-Op)
    public enum ControlScheme { Player_01, Player_02 };

    [Header("Snake Position")]
    [SerializeField] private Vector2Int gridPosition = new Vector2Int(10, 10); // initial position of snake
    private Vector2Int gridMoveDirection;  //current move direction (applied every grid step)
    [SerializeField] private float gridMoveMaxTimer = 0.5f; //movement speed (The lower it is the faster the speed of snake)

    [Header("Input")]
    public ControlScheme controlScheme = ControlScheme.Player_01; //current controller scheme

    private Vector2Int nextMoveDirection; //requested direction from input (applied on next grid move)

    [Header("Start Direction")] //choose whether snake starts moving automatically or waits for input
    [SerializeField] private bool startMovingOnAwake = true;
    [SerializeField] private Vector2Int defaultStartDirection = new Vector2Int(1, 0);

    [Header("GamePlay")]
    [Tooltip("If true, moving off one edge will make the snake appear on the opposite edge")]
    [SerializeField] private bool wrapAround = true;

    [Header("References")]
    [SerializeField] private LevelGridController levelGridController;

    private float gridMoveTimer = 0f;
    private bool isAlive = false;

    // Start is called before the first frame update
    void Start()
    {
        if (!wrapAround && levelGridController == null) Debug.LogWarning($"{name}: wrapAround is false but levelGridController not assigned.");

        isAlive = true;

        // Set starting movement
        if (startMovingOnAwake)
        {
            gridMoveDirection = defaultStartDirection;
        }
        else
        {
            gridMoveDirection = Vector2Int.zero;
        }

        nextMoveDirection = gridMoveDirection;
        transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isAlive) return; //freeze input when dead

        HandleInput();
        HandleGridMovement();
    }


    private void HandleInput()
    {
        //read input and set nextDirection (buffered). Do not directly override gridMoveDirection here.
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

    //prevent 180° turn and buffer direction
    private void TrySetDirection(Vector2Int desired)
    {
        //if desired is reverse of current move, ignore
        if(desired == -gridMoveDirection) return;

        //if desired is same as current, nothing to change
        if (desired == gridMoveDirection) return;

        //buffer it, it will be applied on the next movement tick
        nextMoveDirection = desired;
    }

    private void HandleGridMovement()
    {
        gridMoveTimer += Time.deltaTime;
        if (gridMoveTimer >= gridMoveMaxTimer)
        {
            gridMoveTimer -= gridMoveMaxTimer;

            //Apply buffered direction at the moment of movement.
            if (nextMoveDirection != gridMoveDirection)
            {
                gridMoveDirection = nextMoveDirection;
            }

            //move by the current gridMoveDirection (may be zero if still waiting)
            gridPosition += gridMoveDirection;
            Vector2Int newPos = WrapPosition(gridPosition);

            if (!wrapAround && levelGridController != null)
            {
                if (!levelGridController.IsInsideGrid(gridPosition))
                {
                    // Stop movement and trigger wall hit
                    gridMoveDirection = Vector2Int.zero;
                    nextMoveDirection = Vector2Int.zero;
                    OnHitWall();
                }
            }

            gridPosition = newPos;  //ensure internal position matches final applied position

            transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);  //update transform
        }
    }

    private Vector2Int WrapPosition(Vector2Int pos)
    {
        if (levelGridController ==  null || !wrapAround) return pos;

        int w = levelGridController.width;
        int h = levelGridController.height;

        int x = pos.x;
        int y = pos.y;

        if (x < 0) x = w - 1;
        else if (x >= w) x = 0;

        if (y < 0) y = h - 1;
        else if (y >= h) y = 0;

        return new Vector2Int(x, y);

    }

    private void OnHitWall()
    {
        if (!isAlive) return;

        isAlive = false;

        gridMoveDirection = Vector2Int.zero;
        nextMoveDirection = Vector2Int.zero;

        //Run die Animation /sound.

        Die();
    }

    private void Die()
    {
        StartCoroutine(DelayDestruction(2f));
    }

    private IEnumerator DelayDestruction(float delay)
    {
        yield  return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
