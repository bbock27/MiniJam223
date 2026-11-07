using UnityEngine;

/// Falls under gravity, and is destroyed once it drops below the screen.
/// Moves its Kinematic Rigidbody2D with MovePosition.
[RequireComponent(typeof(Rigidbody2D))]
public class Falling : MonoBehaviour
{
    public float gravity = 15f;

    Rigidbody2D body;
    float speed;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    // Physics runs in FixedUpdate, so the body is moved there too
    void FixedUpdate()
    {
        speed += gravity * Time.fixedDeltaTime;
        body.MovePosition(body.position + Vector2.down * speed * Time.fixedDeltaTime);

        Camera view = Camera.main;
        float screenBottom = view.transform.position.y - view.orthographicSize;
        if (body.position.y < screenBottom - 1f)
            Destroy(gameObject);
    }
}
