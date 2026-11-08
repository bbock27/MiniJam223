using UnityEngine;

/// Moves this object to the other side of the level's play area when it goes off one side.
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(-10)]  // before scripts that move the body, so they carry on from the wrapped position
public class ScreenWrap : MonoBehaviour
{
    public LevelGenerator level;

    Rigidbody2D body;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();

        if (level == null)
        {
            Debug.LogError("ScreenWrap needs a level to know where the sides are.", this);
            enabled = false;
        }
    }

    void FixedUpdate()
    {
        float offset = body.position.x - level.transform.position.x;
        if (Mathf.Abs(offset) > level.playWidth * 0.5f)
        {
            // A jump to the other side is a teleport, so set the position directly instead of using MovePosition
            body.position += Vector2.left * Mathf.Sign(offset) * level.playWidth;
        }
    }
}
