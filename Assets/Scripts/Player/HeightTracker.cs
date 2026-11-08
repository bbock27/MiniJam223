using UnityEngine;

/// Tracks the highest point reached this run. The score is how far that is above the start.
public class HeightTracker : MonoBehaviour
{
    float startY;

    public float Highest { get; private set; }
    public int Score
    {
        get { return 100 * Mathf.FloorToInt(Highest - startY); }
    }

    void Awake()
    {
        startY = transform.position.y;
        Highest = startY;
    }

    void Update()
    {
        Highest = Mathf.Max(Highest, transform.position.y);
    }
}
