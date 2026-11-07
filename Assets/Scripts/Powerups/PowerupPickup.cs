using UnityEngine;

/// Gives its powerup to the player on contact, then disappears. Needs a Collider2D set to Is Trigger.
public class PowerupPickup : MonoBehaviour
{
    public Powerup powerup;  // BirdSpawner picks this when it sends a powerup bird

    void OnTriggerEnter2D(Collider2D other)
    {
        if (powerup == null || !other.TryGetComponent(out PlayerPowerups powerups))
            return;

        powerups.Add(powerup);
        if (AudioManager.Instance != null)
            AudioManager.Instance.Play(AudioManager.SoundType.Powerup);
        Destroy(gameObject);
    }
}
