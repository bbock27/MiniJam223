using UnityEngine;

/// Kills the player on contact. Needs a Collider2D set to Is Trigger.
public class Hazard : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerDeath death))
            death.Die();
    }
}
