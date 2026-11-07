using UnityEngine;

/// Kills the player on contact. Compares collider bounds directly because the player
/// has no Rigidbody2D, so Unity's trigger events would never fire.
public class Hazard : MonoBehaviour
{
    Collider2D hitbox;
    Collider2D playerHitbox;
    PlayerMovement player;

    void Start()
    {
        hitbox = GetComponent<Collider2D>();
        player = FindAnyObjectByType<PlayerMovement>();
        if (player != null)
            playerHitbox = player.GetComponentInChildren<Collider2D>();

        if (hitbox == null || playerHitbox == null)
        {
            Debug.LogError("Hazard needs a Collider2D on itself and on the player.", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (hitbox.bounds.Intersects(playerHitbox.bounds))
            player.Die();
    }
}
