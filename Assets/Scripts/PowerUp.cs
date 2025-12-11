using UnityEngine;

/// <summary>
/// The types of power-ups that can spawn on the grid.
/// Keep this in sync with any UI or spawn code that expects the values.
/// </summary>
public enum PowerUpType
{
    Shield,
    ScoreBoost,
    SpeedUp
}


// Simple component attached to PowerUp prefabs to expose type and duration data.
public class PowerUp : MonoBehaviour
{
    [Header("Power-Up Type")]
    public PowerUpType powerUpType;

    [Tooltip("How long this power-up effect should stay active on the snake.")]
    public float duration = 5f;
}
