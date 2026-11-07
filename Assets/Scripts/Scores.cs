using UnityEngine;

/// The last run's score, and the best score ever reached, which is saved between plays.
public static class Scores
{
    const string BestKey = "HighScore";

    public static int Last { get; private set; }

    public static int Best
    {
        get { return PlayerPrefs.GetInt(BestKey, 0); }
    }

    /// Records a finished run's score, which also becomes the best if it beats it.
    public static void Submit(int score)
    {
        Last = score;
        if (score <= Best)
            return;

        PlayerPrefs.SetInt(BestKey, score);
        PlayerPrefs.Save();
    }
}
