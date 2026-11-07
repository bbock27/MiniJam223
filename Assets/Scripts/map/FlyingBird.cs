using UnityEngine;

/// Flies across the top of the screen carrying cargo, and drops it either when it passes
/// over a target or when it reaches a set spot. It has no collider, so it's purely visual and
/// harmless, which is why it moves by Transform instead of with a Rigidbody2D.
[DefaultExecutionOrder(100)]  // after the camera's LateUpdate, so the bird uses where the camera ends up this frame
public class FlyingBird : MonoBehaviour
{
    public float speed = 4f;
    public float belowTop = 1f;  // how far below the top of the screen it flies

    GameObject cargo;  // hidden until it's dropped
    Transform target;  // drop over this when there is one
    float dropX;       // otherwise drop here
    float endX;
    float direction;   // 1 when flying right, -1 when flying left

    /// Sends it from startX to endX, dropping cargo as it passes over target.
    public void Launch(float startX, float endX, GameObject cargo, Transform target)
    {
        this.target = target;
        Begin(startX, endX, cargo);
    }

    /// Sends it from startX to endX, dropping cargo when it reaches dropX.
    public void Launch(float startX, float endX, GameObject cargo, float dropX)
    {
        this.dropX = dropX;
        Begin(startX, endX, cargo);
    }

    void Begin(float startX, float endX, GameObject cargo)
    {
        this.endX = endX;
        direction = Mathf.Sign(endX - startX);
        transform.position = new Vector3(startX, ScreenTop() - belowTop, 0f);

        // The sprite faces right
        if (TryGetComponent(out SpriteRenderer sprite))
            sprite.flipX = direction < 0f;

        // Held as a hidden child, so it goes with the bird if it's never dropped
        this.cargo = cargo;
        if (cargo != null)
        {
            cargo.SetActive(false);
            cargo.transform.SetParent(transform, false);
        }
    }

    // LateUpdate runs after the camera moves, so the bird holds still on screen
    void LateUpdate()
    {
        Vector3 position = transform.position;
        position.x += direction * speed * Time.deltaTime;
        position.y = ScreenTop() - belowTop;
        transform.position = position;

        if (cargo != null && (DropX() - position.x) * direction <= 0f)
            Drop(position);

        if ((endX - position.x) * direction <= 0f)
            Destroy(gameObject);
    }

    float DropX()
    {
        if (target != null)
            return target.position.x;
        return dropX;
    }

    void Drop(Vector3 position)
    {
        cargo.transform.SetParent(transform.parent);
        cargo.transform.position = position;
        cargo.SetActive(true);
        cargo = null;
    }

    static float ScreenTop()
    {
        Camera view = Camera.main;
        return view.transform.position.y + view.orthographicSize;
    }
}
