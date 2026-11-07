using UnityEngine;

/// Sends birds across the top of the screen at random times, more often as the climb gets harder.
/// Most drop a hammer on the player; some drop a random powerup somewhere random instead.
public class BirdSpawner : MonoBehaviour
{
    public FlyingBird birdPrefab;
    public GameObject hammerPrefab;
    public PowerupPickup powerupPrefab;
    public Powerup[] powerups;     // powerup birds bring one of these; while it's empty, every bird brings a hammer
    public LevelGenerator level;   // for the play area and the difficulty
    public HeightTracker player;   // hammers drop on them

    public float easyDelay = 15f;  // average seconds between birds at the start
    public float hardDelay = 4f;   // average seconds between birds at full difficulty
    [Range(0f, 1f)]
    public float powerupChance = 0.25f;  // chance a bird brings a powerup instead of a hammer
    public float startOutside = 1f;  // birds start and finish this far beyond the sides of the play area

    float nextBirdTime;

    void Start()
    {
        if (birdPrefab == null || hammerPrefab == null || level == null || player == null)
        {
            Debug.LogError("BirdSpawner needs a bird prefab, a hammer prefab, a level and a player.", this);
            enabled = false;
            return;
        }

        ScheduleNextBird();
    }

    void Update()
    {
        if (Time.time < nextBirdTime)
            return;

        SendBird();
        ScheduleNextBird();
    }

    void ScheduleNextBird()
    {
        float difficulty = level.DifficultyAt(player.Highest);
        float averageDelay = Mathf.Lerp(easyDelay, hardDelay, difficulty);
        nextBirdTime = Time.time + averageDelay * Random.Range(0.5f, 1.5f);
    }

    void SendBird()
    {
        float side = Random.value < 0.5f ? -1f : 1f;
        float reach = level.playWidth * 0.5f + startOutside;
        float centre = level.transform.position.x;
        float startX = centre + side * reach;
        float endX = centre - side * reach;

        FlyingBird bird = Instantiate(birdPrefab, transform);

        if (CanBringPowerup() && Random.value < powerupChance)
        {
            PowerupPickup pickup = Instantiate(powerupPrefab);
            pickup.powerup = powerups[Random.Range(0, powerups.Length)];

            // Somewhere a platform could be, so the player can get under it
            float dropRange = level.playWidth * 0.5f - level.edgeMargin;
            bird.Launch(startX, endX, pickup.gameObject, centre + Random.Range(-dropRange, dropRange));
        }
        else
        {
            bird.Launch(startX, endX, Instantiate(hammerPrefab), player.transform);
        }
    }

    bool CanBringPowerup()
    {
        return powerupPrefab != null && powerups != null && powerups.Length > 0;
    }
}
