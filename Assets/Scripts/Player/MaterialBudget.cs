using UnityEngine;

/// The player's building materials: each platform built costs one, and pickups add more.
public class MaterialBudget : MonoBehaviour
{
    public int startingMaterials = 10;

    public int Count { get; private set; }

    void Awake()
    {
        Count = startingMaterials;
    }

    public bool TrySpend()
    {
        if (Count <= 0)
            return false;

        Count--;
        return true;
    }

    public void Add(int amount)
    {
        Count += amount;
    }
}
