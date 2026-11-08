using System.Collections.Generic;
using UnityEngine;

/// Generates the level ahead of the player as they climb and clears it away behind them:
/// platform spots, hazards, material pickups and the sky.
public class LevelGenerator : MonoBehaviour
{
    public Transform target;  // the player, which needs a HeightTracker
    public GameObject spotPrefab;
    public GameObject placedPlatformPrefab;
    public GameObject skyPrefab;
    public GameObject hazardPrefab;
    public GameObject pickupPrefab;

    [Header("Play area")]
    public float playWidth = 8f;
    public float edgeMargin = 1f;    // keeps platform centres this far from the sides
    public float skyHeight = 16f;    // world height of the Sky sprite

    [Header("Platforms")]
    public float startY = 2f;        // height of the first generated platform
    public float startPlatformDrop = 2f;  // a real platform sits this far below the player at the start
    public float easyMaxGap = 1.5f;  // largest gap at the start
    public float hardMaxGap = 2.7f;  // largest gap once difficultyHeight is reached
    public float difficultyHeight = 300f;

    [Range(0f, 1f)]
    public float extraPlatformChance = 0.25f;  // chance of a bonus platform beside the main one

    [Header("Pickups")]
    public float minPickupSpacing = 6f;  // height between pickups: 6 to 10 lets careful
    public float maxPickupSpacing = 10f; // building outlast the budget, wasteful building can't

    [Header("Hazards")]
    [Range(0f, 1f)]
    public float easyHazardChance = 0.05f;  // chance per platform row at the start
    [Range(0f, 1f)]
    public float hardHazardChance = 0.35f;  // chance per platform row at difficultyHeight
    public float hazardMinGap = 1.5f;       // only gaps this tall have room for a hazard
    public float hazardClearance = 1.5f;    // minimum sideways distance from nearby platforms

    [Header("Streaming")]
    public float spawnAhead = 14f;   // generate this far above the highest point reached
    public float despawnBehind = 14f;  // destroy this far below the highest point reached

    // Spawning goes upwards, so the front of each queue is its lowest object and culling only checks there.
    // Spots and pickups can be destroyed early (built on, collected); culling drops those when it reaches them.
    readonly Queue<GameObject> spots = new Queue<GameObject>();
    readonly Queue<GameObject> pickups = new Queue<GameObject>();
    readonly Queue<GameObject> platforms = new Queue<GameObject>();
    readonly Queue<GameObject> hazards = new Queue<GameObject>();
    readonly Queue<GameObject> skies = new Queue<GameObject>();

    HeightTracker height;
    float nextPlatformX;
    float nextPlatformY;
    float nextPickupY;
    float nextSkyY;
    float gapLimit;
    Vector2 startPlatform;
    Vector2 startClearance;

    float PlatformSpan
    {
        get { return Mathf.Max(0f, playWidth - edgeMargin * 2f); }
    }

    /// Falling below this ends the run: the middle of the lowest sky still in the level.
    public float KillHeight
    {
        get
        {
            if (skies.Count == 0)
                return float.NegativeInfinity;
            return skies.Peek().transform.position.y;
        }
    }

    void Start()
    {
        if (target == null || !target.TryGetComponent(out height) || spotPrefab == null || placedPlatformPrefab == null)
        {
            Debug.LogError("LevelGenerator needs a target with a HeightTracker, a spot prefab and a placed platform prefab.", this);
            enabled = false;
            return;
        }

        gapLimit = target.TryGetComponent(out PlayerMovement movement)
            ? movement.BounceHeight * 0.9f
            : hardMaxGap;

        nextPlatformX = Random.Range(0f, PlatformSpan);
        nextPlatformY = startY;
        nextPickupY = target.position.y + Random.Range(minPickupSpacing, maxPickupSpacing);
        nextSkyY = -skyHeight;  // one tile below the start, so the kill line sits under the first platforms

        startPlatform = new Vector2(target.position.x, target.position.y - startPlatformDrop);
        platforms.Enqueue(SpawnAt(placedPlatformPrefab, startPlatform.x, startPlatform.y));

        startClearance = (placedPlatformPrefab.GetComponent<BoxCollider2D>().size
            + spotPrefab.GetComponent<BoxCollider2D>().size) * 0.5f;
    }

    void Update()
    {
        float top = height.Highest + spawnAhead;
        while (nextPlatformY < top)
            SpawnPlatformRow();
        while (skyPrefab != null && nextSkyY - skyHeight * 0.5f < top)
            SpawnSky();

        float bottom = height.Highest - despawnBehind;
        Cull(spots, bottom, 0f);
        Cull(pickups, bottom, 0f);
        Cull(platforms, bottom, 0f);
        Cull(hazards, bottom, 0f);
        Cull(skies, bottom, skyHeight * 0.5f);
    }

    /// How hard the level is at this height, from 0 at the bottom to 1 at difficultyHeight and above.
    public float DifficultyAt(float height)
    {
        return Mathf.Clamp01(height / difficultyHeight);
    }

    /// Replaces a platform spot with a real platform the player can land on.
    public void BuildPlatform(GameObject spot)
    {
        Vector3 position = spot.transform.position;
        platforms.Enqueue(SpawnAt(placedPlatformPrefab, position.x, position.y));
        Destroy(spot);
    }

    void SpawnPlatformRow()
    {
        float span = PlatformSpan;
        float left = transform.position.x - span * 0.5f;

        float x = nextPlatformX;
        nextPlatformX = Random.Range(0f, span);

        float difficulty = DifficultyAt(nextPlatformY);
        float maxGap = Mathf.Min(Mathf.Lerp(easyMaxGap, hardMaxGap, difficulty), gapLimit);
        float gap = Random.Range(maxGap * 0.5f, maxGap);
        float gapMiddle = nextPlatformY + gap * 0.5f;

        SpawnSpot(left + x, nextPlatformY);

        if (Random.value < extraPlatformChance)
            SpawnSpot(left + Mathf.Repeat(x + span * 0.5f, span), nextPlatformY);

        // Each gap holds a pickup or a hazard, never both
        if (pickupPrefab != null && nextPlatformY >= nextPickupY)
        {
            pickups.Enqueue(SpawnAt(pickupPrefab, left + x, gapMiddle));
            nextPickupY = nextPlatformY + Random.Range(minPickupSpacing, maxPickupSpacing);
        }
        else if (hazardPrefab != null && gap >= hazardMinGap
            && Random.value < Mathf.Lerp(easyHazardChance, hardHazardChance, difficulty))
        {
            TrySpawnHazard(x, left, span, gapMiddle);
        }

        nextPlatformY += gap;
    }

    void TrySpawnHazard(float x, float left, float span, float y)
    {
        for (int attempt = 0; attempt < 4; attempt++)
        {
            float hazardX = Random.Range(0f, span);
            if (Mathf.Abs(hazardX - x) >= hazardClearance && Mathf.Abs(hazardX - nextPlatformX) >= hazardClearance
                && !OverlapsStartPlatform(left + hazardX, y))
            {
                hazards.Enqueue(SpawnAt(hazardPrefab, left + hazardX, y));
                return;
            }
        }
    }

    void SpawnSpot(float x, float y)
    {
        if (!OverlapsStartPlatform(x, y))
            spots.Enqueue(SpawnAt(spotPrefab, x, y));
    }

    bool OverlapsStartPlatform(float x, float y)
    {
        return Mathf.Abs(x - startPlatform.x) < startClearance.x
            && Mathf.Abs(y - startPlatform.y) < startClearance.y;
    }

    void SpawnSky()
    {
        skies.Enqueue(SpawnAt(skyPrefab, transform.position.x, nextSkyY));
        nextSkyY += skyHeight;
    }

    GameObject SpawnAt(GameObject prefab, float x, float y)
    {
        return Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity, transform);
    }

    // halfHeight: how far the object reaches above its position, so it is only destroyed once fully below
    static void Cull(Queue<GameObject> objects, float bottom, float halfHeight)
    {
        while (objects.Count > 0)
        {
            GameObject lowest = objects.Peek();
            if (lowest != null && lowest.transform.position.y + halfHeight >= bottom)
                return;

            objects.Dequeue();
            if (lowest != null)
                Destroy(lowest);
        }
    }
}
