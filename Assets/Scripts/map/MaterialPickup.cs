using UnityEngine;

/// Gives the player building materials on contact, then disappears. Needs a Collider2D set to Is Trigger.
public class MaterialPickup : MonoBehaviour
{
    public int amount = 5;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out MaterialBudget materials))
            return;

        materials.Add(amount);
        if (AudioManager.Instance != null)
            AudioManager.Instance.Play(AudioManager.SoundType.Pickup);
        Destroy(gameObject);
    }
}
