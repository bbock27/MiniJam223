using UnityEngine;
using UnityEngine.SceneManagement;

/// Ends the run when the player falls out of the bottom of the level or something kills them:
/// records the score and goes to the end menu.
public class PlayerDeath : MonoBehaviour
{
    public LevelGenerator level;
    public string endScene = "EndMenu";

    bool dead;

    void Start()
    {
        if (level == null)
            Debug.LogError("PlayerDeath has no level, so falling won't end the run.", this);
    }

    void Update()
    {
        if (level != null && transform.position.y < level.KillHeight)
            Die();
    }

    public void Die()
    {
        // End the run once, even if several things kill the player in the same frame
        if (dead)
            return;

        dead = true;
        if (TryGetComponent(out HeightTracker height))
            Scores.Submit(height.Score);
        SceneManager.LoadScene(endScene);
    }
}
