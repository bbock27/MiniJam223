using UnityEngine;

/// Makes the player bounce higher while it lasts.
[CreateAssetMenu(menuName = "Powerups/High Jump")]
public class HighJump : Powerup
{
    [Min(0.1f)]
    public float heightMultiplier = 1.5f;

    public override void Apply(GameObject player)
    {
        if (player.TryGetComponent(out PlayerMovement movement))
            movement.BounceHeightMultiplier *= heightMultiplier;
    }

    public override void Remove(GameObject player)
    {
        if (player.TryGetComponent(out PlayerMovement movement))
            movement.BounceHeightMultiplier /= heightMultiplier;
    }
}
