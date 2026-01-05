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

    private float gridMoveTimer = 0f; 

    private Vector2Int nextMoveDirection; //requested direction from input (applied on next grid move)

    [Header("Start Direction")] //choose whether snake starts moving automatically or waits for input
    [SerializeField] private bool startMovingOnAwake = true;
    [SerializeField] private Vector2Int defaultStartDirection = new Vector2Int(1, 0);

    // Start is called before the first frame update
    void Start()
    {
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

    //centralised check to avoid 190 degree turn and to queue direction
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
            transform.position = new Vector3(gridPosition.x, gridPosition.y);
        }
    }
}
