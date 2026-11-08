using TMPro;
using UnityEngine;

/// Shows the last run's score and the high score, on two lines.
[RequireComponent(typeof(TMP_Text))]
public class ScoreSummary : MonoBehaviour
{
    void Start()
    {
        GetComponent<TMP_Text>().text = $"Score: {Scores.Last}\nHigh Score: {Scores.Best}";
    }
}
