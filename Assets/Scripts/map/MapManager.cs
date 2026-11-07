using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class MapManager : MonoBehaviour
{
    public Transform target;
    [FormerlySerializedAs("platformPrefab")]
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

    [Header("Materials")]
    public int startingMaterials = 10;
    public int pickupMaterials = 5;      // gained from each pickup
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

    readonly List<GameObject> spots = new List<GameObject>();  // lowest first, spots leave from anywhere
    readonly List<GameObject> pickups = new List<GameObject>();  // same, once collected
    readonly Queue<GameObject> platforms = new Queue<GameObject>();
    readonly Queue<GameObject> hazards = new Queue<GameObject>();
    readonly Queue<GameObject> skies = new Queue<GameObject>();

    float nextPlatformX;
    float nextPlatformY;
    float nextSkyY;
    float highestY;
    float gapLimit;
    LayerMask spotLayer;
    PlayerMovement player;
    Vector2 startPlatform;
    Vector2 startClearance;
    Collider2D playerHitbox;
    float nextPickupY;
    float startHeight;
    int materials;

    float PlatformSpan => Mathf.Max(0f, playWidth - edgeMargin * 2f);

    public int Materials => materials;
    public int Score => Mathf.FloorToInt(highestY - startHeight);

    void Start()
    {
        spotLayer = LayerMask.GetMask("PlatformSpot");

        if (target == null || spotPrefab == null || placedPlatformPrefab == null || spotLayer == 0)
        {
            Debug.LogError("MapManager needs a target, a spot prefab, a placed platform prefab and a PlatformSpot layer.", this);
            enabled = false;
            return;
        }

        gapLimit = target.TryGetComponent(out player)
            ? player.BounceHeight * 0.9f
            : hardMaxGap;

        nextPlatformX = Random.Range(0f, PlatformSpan);
        nextPlatformY = startY;
        highestY = target.position.y;
        startHeight = target.position.y;

        playerHitbox = target.GetComponentInChildren<Collider2D>();
        materials = startingMaterials;
        nextPickupY = startHeight + Random.Range(minPickupSpacing, maxPickupSpacing);

        nextSkyY = -skyHeight;

        startPlatform = new Vector2(target.position.x, target.position.y - startPlatformDrop);
        platforms.Enqueue(SpawnAt(placedPlatformPrefab, startPlatform.x, startPlatform.y));

        startClearance = (placedPlatformPrefab.GetComponent<BoxCollider2D>().size
            + spotPrefab.GetComponent<BoxCollider2D>().size) * 0.5f;
    }

    void Update()
    {
        WrapTarget();

        highestY = Mathf.Max(highestY, target.position.y);

        float top = highestY + spawnAhead;
        while (nextPlatformY < top)
            SpawnPlatforms();
        while (skyPrefab != null && nextSkyY - skyHeight * 0.5f < top)
            SpawnSky();

        BuildClickedSpot();
        CollectPickups();

        float bottom = highestY - despawnBehind;
        Cull(spots, bottom);
        Cull(pickups, bottom);
        Cull(platforms, bottom, 0f);
        Cull(hazards, bottom, 0f);
        Cull(skies, bottom, skyHeight * 0.5f);

        if (player != null && skies.Count > 0 && target.position.y < skies.Peek().transform.position.y)
            player.Die();
    }

    void WrapTarget()
    {
        float offset = target.position.x - transform.position.x;
        if (Mathf.Abs(offset) > playWidth * 0.5f)
            target.position += Vector3.left * Mathf.Sign(offset) * playWidth;
    }

    void SpawnPlatforms()
    {
        float span = PlatformSpan;
        float left = transform.position.x - span * 0.5f;

        float x = nextPlatformX;
        nextPlatformX = Random.Range(0f, span);

        float difficulty = Mathf.Clamp01(nextPlatformY / difficultyHeight);
        float maxGap = Mathf.Min(Mathf.Lerp(easyMaxGap, hardMaxGap, difficulty), gapLimit);
        float gap = Random.Range(maxGap * 0.5f, maxGap);

        SpawnSpot(left + x, nextPlatformY);

        if (Random.value < extraPlatformChance)
        {
            SpawnSpot(left + Mathf.Repeat(x + span * 0.5f, span), nextPlatformY);
        }

        if (pickupPrefab != null && nextPlatformY >= nextPickupY)
        {
            pickups.Add(SpawnAt(pickupPrefab, left + x, nextPlatformY + gap * 0.5f));
            nextPickupY = nextPlatformY + Random.Range(minPickupSpacing, maxPickupSpacing);
        }
        else if (hazardPrefab != null && gap >= hazardMinGap
            && Random.value < Mathf.Lerp(easyHazardChance, hardHazardChance, difficulty))
        {
            TrySpawnHazard(x, left, span, nextPlatformY + gap * 0.5f);
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
            spots.Add(SpawnAt(spotPrefab, x, y));
    }

    bool OverlapsStartPlatform(float x, float y)
    {
        return Mathf.Abs(x - startPlatform.x) < startClearance.x
            && Mathf.Abs(y - startPlatform.y) < startClearance.y;
    }

    void BuildClickedSpot()
    {
        Pointer pointer = Pointer.current;
        if (materials <= 0 || pointer == null || !pointer.press.wasPressedThisFrame)
            return;

        Vector2 click = Camera.main.ScreenToWorldPoint(pointer.position.ReadValue());
        Collider2D hit = Physics2D.OverlapPoint(click, spotLayer);
        if (hit == null)
            return;

        GameObject spot = hit.gameObject;
        Vector3 position = spot.transform.position;
        platforms.Enqueue(SpawnAt(placedPlatformPrefab, position.x, position.y));
        spots.Remove(spot);
        Destroy(spot);
        materials--;
    }


    void CollectPickups()
    {
        if (playerHitbox == null)
            return;

        Bounds hitbox = playerHitbox.bounds;
        for (int i = pickups.Count - 1; i >= 0; i--)
        {
            if (!pickups[i].GetComponent<SpriteRenderer>().bounds.Intersects(hitbox))
                continue;

            materials += pickupMaterials;
            Destroy(pickups[i]);
            pickups.RemoveAt(i);
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play(AudioManager.SoundType.Pickup);
        }
    }

    void Cull(List<GameObject> objects, float bottom)
    {
        while (objects.Count > 0 && objects[0].transform.position.y < bottom)
        {
            Destroy(objects[0]);
            objects.RemoveAt(0);
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
