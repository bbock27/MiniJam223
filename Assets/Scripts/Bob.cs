using UnityEngine;

/// Slowly bobs this object up and down around where it started.
/// Moves its Kinematic Rigidbody2D with MovePosition, so don't combine it with another
/// script that moves the same object.
[RequireComponent(typeof(Rigidbody2D))]
public class Bob : MonoBehaviour
{
    public float height = 0.1f;  // how far above and below its resting point it goes
    public float period = 2f;    // seconds for one full bob up and down

    Rigidbody2D body;
    Vector2 restPosition;
    float phase;  // a random start point, so several items don't bob in step

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        restPosition = body.position;
        phase = Random.value * period;
    }

    // Physics runs in FixedUpdate, so the body is moved there too
    void FixedUpdate()
    {
        float offset = height * Mathf.Sin((Time.time + phase) / period * 2f * Mathf.PI);
        body.MovePosition(restPosition + Vector2.up * offset);
    }
}
