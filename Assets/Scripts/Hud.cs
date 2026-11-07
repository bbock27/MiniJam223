using TMPro;
using UnityEngine;

/// Shows the run's score and remaining materials.
public class Hud : MonoBehaviour
{
    public MapManager map;
    public TMP_Text scoreText;
    public TMP_Text materialsText;

    void Update()
    {
        scoreText.SetText("Score: {0}", map.Score);
        materialsText.SetText("Materials: {0}", map.Materials);
    }
}
