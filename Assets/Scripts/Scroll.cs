using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class Scroll : MonoBehaviour
{
    SpriteRenderer sr;
    CircleCollider2D col;
    bool collected = false;

    void Awake()
    {
        sr  = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        Player player = other.GetComponent<Player>();
        if (player == null) return;

        collected = true;
        col.enabled = false; // prevent double-trigger
        player.StartPickup(this);
    }

    // Called by Player at pick frame 3 — hides the ground scroll
    public void HideGroundScroll()
    {
        sr.enabled = false;
    }

    // Called by Player after pickup animation finishes
    public void Collect()
    {
        gameObject.SetActive(false);
        GameController.instance?.OnScrollCollected();
    }
}
