using UnityEngine;
using UnityEngine.InputSystem;

/// Moves the player side to side from input and bounces it off platforms it lands on.
/// Its Rigidbody2D is Kinematic: this script works out where the player goes and moves it there with MovePosition.
/// Set every value in the Inspector; there are no defaults in code.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float bounceHeight;  // world units
    [SerializeField] private float timeToApex;    // seconds
    [SerializeField] private float moveSpeed;
    [SerializeField] private Vector2 feetSize;
    public Transform footPoint;

    private Rigidbody2D body;
    private Vector2 footOffset;  // from the body's position to the feet
    private Vector2 velocity;
    private float moveInput;
    private LayerMask platformLayer;

    public float BounceHeight
    {
        get { return bounceHeight; }
    }

    // Powerups scale this to change how high the player bounces; 1 is normal
    public float BounceHeightMultiplier { get; set; } = 1f;

    void Awake()
    {
        if (bounceHeight <= 0f || timeToApex <= 0f || footPoint == null)
        {
            Debug.LogError("PlayerMovement needs Bounce Height and Time To Apex above 0, and a Foot Point, set in the Inspector.", this);
            enabled = false;
            return;
        }

        body = GetComponent<Rigidbody2D>();
        platformLayer = LayerMask.GetMask("Platform");
        footOffset = footPoint.position - transform.position;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>().x;
    }

    // Physics runs in FixedUpdate, so the body is moved there too
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float gravity = 2f * bounceHeight / (timeToApex * timeToApex);
        // Gravity ignores the multiplier, so a higher bounce also stays in the air longer
        float bounceSpeed = Mathf.Sqrt(2f * gravity * bounceHeight * BounceHeightMultiplier);

        velocity.x = moveInput * moveSpeed;
        float deltaY = velocity.y * dt - 0.5f * gravity * dt * dt;
        velocity.y -= gravity * dt;

        Vector2 feet = body.position + footOffset;
        Vector2 next = body.position;
        next.x += velocity.x * dt;

        if (deltaY < 0f && TryLand(feet, -deltaY, out float surfaceY))
        {
            next.y += surfaceY - feet.y;
            velocity.y = bounceSpeed;
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play(AudioManager.SoundType.Jump);
        }
        else
        {
            next.y += deltaY;
        }
        body.MovePosition(next);
    }

    bool TryLand(Vector2 feet, float distance, out float surfaceY)
    {
        RaycastHit2D hit = Physics2D.BoxCast(feet, feetSize, 0f, Vector2.down, distance, platformLayer);
        surfaceY = hit ? hit.collider.bounds.max.y : 0f;
        return hit && feet.y >= surfaceY - 0.01f; // only land from above
    }
}
