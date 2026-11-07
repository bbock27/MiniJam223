using UnityEngine;

/// Smoothly follows the target up and down, keeping the camera's own x and z.
public class BasicCameraFollow : MonoBehaviour
{
    public Transform followTarget;
    public float smoothTime = 0.2f;  // roughly how many seconds the camera takes to catch up

    Vector3 velocity;  // SmoothDamp keeps this between frames so the camera eases in and out

    // LateUpdate runs after the player has moved this frame, so the camera never lags a frame behind
    void LateUpdate()
    {
        if (followTarget == null)
            return;

        Vector3 target = new Vector3(transform.position.x, followTarget.position.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
    }
}
