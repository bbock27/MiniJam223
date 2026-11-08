using System.Collections.Generic;
using UnityEngine;

/// Applies the powerups the player collects and removes each one when its time runs out.
public class PlayerPowerups : MonoBehaviour
{
    readonly Dictionary<Powerup, float> endTimes = new Dictionary<Powerup, float>();
    readonly List<Powerup> active = new List<Powerup>();  // in the order they were collected
    readonly List<Powerup> expired = new List<Powerup>();

    /// The timed powerups affecting the player right now, oldest first.
    public IReadOnlyList<Powerup> Active
    {
        get { return active; }
    }

    /// Seconds until this powerup wears off, or 0 if it isn't active.
    public float TimeLeft(Powerup powerup)
    {
        if (!endTimes.TryGetValue(powerup, out float endTime))
            return 0f;
        return Mathf.Max(0f, endTime - Time.time);
    }

    public void Add(Powerup powerup)
    {
        if (powerup.duration <= 0f)
        {
            powerup.Apply(gameObject);
            return;
        }

        // Collecting one that's already active only restarts its timer
        if (!endTimes.ContainsKey(powerup))
        {
            powerup.Apply(gameObject);
            active.Add(powerup);
        }
        endTimes[powerup] = Time.time + powerup.duration;
    }

    void Update()
    {
        foreach (KeyValuePair<Powerup, float> active in endTimes)
        {
            if (Time.time >= active.Value)
                expired.Add(active.Key);
        }

        foreach (Powerup powerup in expired)
        {
            endTimes.Remove(powerup);
            active.Remove(powerup);
            powerup.Remove(gameObject);
        }
        expired.Clear();
    }
}
