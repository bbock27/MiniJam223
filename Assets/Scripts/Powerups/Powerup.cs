using UnityEngine;


public abstract class Powerup : ScriptableObject
{
    public string displayName;   // shown in the HUD; the asset's name when left empty
    public float duration = 5f;  // seconds

    public string DisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(displayName))
                return name;
            return displayName;
        }
    }

    public abstract void Apply(GameObject player);

    public virtual void Remove(GameObject player)
    {
    }
}
