using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float bounceHeight = 4f;   // world units
    [SerializeField] private float timeToApex = 0.55f;  // seconds
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private Vector2 feetSize = new Vector2(0.9f, 0.02f);
    public Transform footPoint;

    private Vector2 velocity;
    private float moveInput;
    private LayerMask platformLayer;

    public float BounceHeight => bounceHeight;

    void Awake() => platformLayer = LayerMask.GetMask("Platform");

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>().x;

    void Update()
    {
        float dt = Time.deltaTime;
        float gravity = 2f * bounceHeight / (timeToApex * timeToApex);
        float bounceSpeed = 2f * bounceHeight / timeToApex;

        velocity.x = moveInput * moveSpeed;
        float deltaY = velocity.y * dt - 0.5f * gravity * dt * dt;
        velocity.y -= gravity * dt;

        Vector3 pos = transform.position;
        pos.x += velocity.x * dt;

        if (deltaY < 0f && TryLand(-deltaY, out float surfaceY))
        {
            pos.y += surfaceY - footPoint.position.y;
            velocity.y = bounceSpeed;
        }
        else
        {
            pos.y += deltaY;
        }
        transform.position = pos;
    }

    public void Die() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    bool TryLand(float distance, out float surfaceY)
    {
        Vector2 feet = footPoint.position;
        RaycastHit2D hit = Physics2D.BoxCast(feet, feetSize, 0f, Vector2.down, distance, platformLayer);
        surfaceY = hit ? hit.collider.bounds.max.y : 0f;
        return hit && feet.y >= surfaceY - 0.01f; // only land from above
    }
}
