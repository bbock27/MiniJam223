using System.Collections.Generic;
using UnityEngine;

/// Endless upward level. A cursor climbs by a random gap, placing platforms until it
/// is above the player's highest point, and anything far enough below is destroyed.
public class MapManager : MonoBehaviour
{
    public Transform target;
    public GameObject platformPrefab;
    public GameObject skyPrefab;
    public GameObject hazardPrefab;

    [Header("Play area")]
    public float playWidth = 8f;     // centred on this object, 8 matches the Sky sprite
    public float edgeMargin = 1f;    // keeps platform centres this far from the sides
    public float skyHeight = 16f;    // world height of the Sky sprite

    [Header("Platforms")]
    public float startY = 2f;        // height of the first generated platform
    public float easyMaxGap = 1.5f;  // largest gap at the start
    public float hardMaxGap = 2.7f;  // largest gap once difficultyHeight is reached
    public float difficultyHeight = 300f;

    [Range(0f, 1f)]
    public float extraPlatformChance = 0.25f;  // chance of a bonus platform beside the main one

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

    readonly Queue<GameObject> platforms = new Queue<GameObject>();
    readonly Queue<GameObject> hazards = new Queue<GameObject>();
    readonly Queue<GameObject> skies = new Queue<GameObject>();

    float nextPlatformX;
    float nextPlatformY;
    float nextSkyY;
    float highestY;
    float gapLimit;

    float PlatformSpan => Mathf.Max(0f, playWidth - edgeMargin * 2f);

    void Start()
    {
        if (target == null || platformPrefab == null)
        {
            Debug.LogError("MapManager needs a target and a platform prefab.", this);
            enabled = false;
            return;
        }

        // Never generate a gap the player can't clear, whatever the settings above say
        gapLimit = target.TryGetComponent(out PlayerMovement player)
            ? player.BounceHeight * 0.9f
            : hardMaxGap;

        nextPlatformX = Random.Range(0f, PlatformSpan);
        nextPlatformY = startY;
        highestY = target.position.y;
    }

    void Update()
    {
        highestY = Mathf.Max(highestY, target.position.y);

        float top = highestY + spawnAhead;
        while (nextPlatformY < top)
            SpawnPlatforms();
        while (skyPrefab != null && nextSkyY - skyHeight * 0.5f < top)
            SpawnSky();

        float bottom = highestY - despawnBehind;
        Cull(platforms, bottom, 0f);
        Cull(hazards, bottom, 0f);
        Cull(skies, bottom, skyHeight * 0.5f);
    }

    void SpawnPlatforms()
    {
        float span = PlatformSpan;
        float left = transform.position.x - span * 0.5f;

        // The next platform is chosen now so hazards can keep clear of it
        float x = nextPlatformX;
        nextPlatformX = Random.Range(0f, span);

        float difficulty = Mathf.Clamp01(nextPlatformY / difficultyHeight);
        float maxGap = Mathf.Min(Mathf.Lerp(easyMaxGap, hardMaxGap, difficulty), gapLimit);
        float gap = Random.Range(maxGap * 0.5f, maxGap);

        platforms.Enqueue(SpawnAt(platformPrefab, left + x, nextPlatformY));

        // Half the play area away, so the two platforms never overlap
        if (Random.value < extraPlatformChance)
        {
            platforms.Enqueue(SpawnAt(platformPrefab, left + Mathf.Repeat(x + span * 0.5f, span), nextPlatformY));
        }
        else if (hazardPrefab != null && gap >= hazardMinGap
            && Random.value < Mathf.Lerp(easyHazardChance, hardHazardChance, difficulty))
        {
            TrySpawnHazard(x, left, span, nextPlatformY + gap * 0.5f);
        }

        nextPlatformY += gap;
    }

    /// Sits halfway up the gap and stays clear of the platforms above and below it, so
    /// the route between them stays open. Gives up if there's no room this row.
    void TrySpawnHazard(float x, float left, float span, float y)
    {
        for (int attempt = 0; attempt < 4; attempt++)
        {
            float hazardX = Random.Range(0f, span);
            if (Mathf.Abs(hazardX - x) >= hazardClearance && Mathf.Abs(hazardX - nextPlatformX) >= hazardClearance)
            {
                hazards.Enqueue(SpawnAt(hazardPrefab, left + hazardX, y));
                return;
            }
        }
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

    void Cull(Queue<GameObject> objects, float bottom, float halfHeight)
    {
        while (objects.Count > 0 && objects.Peek().transform.position.y + halfHeight < bottom)
            Destroy(objects.Dequeue());
    }
}
