using UnityEngine;
using UnityEngine.InputSystem;

/// Building with the mouse: the platform spot under the pointer is replaced by selectionPrefab,
/// and clicking it spends one material to build a real platform there.
public class PlatformBuilder : MonoBehaviour
{
    public LevelGenerator level;
    public MaterialBudget materials;
    public GameObject selectionPrefab;

    LayerMask spotLayer;
    GameObject selection;  // one copy of selectionPrefab, moved to whichever spot is hovered
    GameObject hovered;    // the spot it is standing in for

    void Start()
    {
        spotLayer = LayerMask.GetMask("PlatformSpot");

        if (level == null || materials == null || selectionPrefab == null || spotLayer == 0)
        {
            Debug.LogError("PlatformBuilder needs a level, a material budget, a selection prefab and a PlatformSpot layer.", this);
            enabled = false;
            return;
        }

        selection = Instantiate(selectionPrefab, transform);
        // It's only for show: with its collider on, the pointer would find it instead of the spot underneath
        foreach (Collider2D collider in selection.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        selection.SetActive(false);
    }

    void Update()
    {
        Hover(SpotUnderPointer());

        if (hovered != null && Pointer.current.press.wasPressedThisFrame && materials.TrySpend())
        {
            GameObject spot = hovered;
            Hover(null);
            level.BuildPlatform(spot);
        }
    }

    // Nothing is selectable without materials, so the selection only shows where a click would build
    GameObject SpotUnderPointer()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || materials.Count <= 0)
            return null;

        Vector2 point = Camera.main.ScreenToWorldPoint(pointer.position.ReadValue());
        Collider2D hit = Physics2D.OverlapPoint(point, spotLayer);
        return hit != null ? hit.gameObject : null;
    }

    void Hover(GameObject spot)
    {
        // ReferenceEquals because a hovered spot that got culled compares equal to null,
        // which would leave the selection showing where it used to be
        if (ReferenceEquals(spot, hovered))
            return;

        if (hovered != null)
            SetVisible(hovered, true);

        hovered = spot;
        if (spot != null)
        {
            SetVisible(spot, false);
            selection.transform.position = spot.transform.position;
        }
        selection.SetActive(spot != null);
    }

    static void SetVisible(GameObject target, bool visible)
    {
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            renderer.enabled = visible;
    }
}
