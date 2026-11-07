using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// Lists the player's active powerups, one per line, with the seconds each has left.
[RequireComponent(typeof(TMP_Text))]
public class PowerupIndicator : MonoBehaviour
{
    public PlayerPowerups powerups;

    readonly StringBuilder lines = new StringBuilder();
    TMP_Text text;

    void Start()
    {
        text = GetComponent<TMP_Text>();

        if (powerups == null)
        {
            Debug.LogError("PowerupIndicator needs the player's PlayerPowerups.", this);
            enabled = false;
        }
    }

    void Update()
    {
        lines.Clear();
        IReadOnlyList<Powerup> active = powerups.Active;
        for (int i = 0; i < active.Count; i++)
        {
            if (i > 0)
                lines.Append('\n');
            lines.Append(active[i].DisplayName).Append(": ").Append(Mathf.RoundToInt(powerups.TimeLeft(active[i]))).Append('s');
        }
        text.SetText(lines);
    }
}
