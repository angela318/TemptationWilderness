using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class Scroll : MonoBehaviour
{
    [Header("Bob Settings")]
    public float bobSpeed  = 1.8f;
    public float bobHeight = 0.08f;

    SpriteRenderer   sr;
    CircleCollider2D col;
    bool             collected = false;
    Vector3          basePosition;

    void Awake()
    {
        sr  = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
    }

    void Start()
    {
        basePosition = transform.position;
    }

    void Update()
    {
        if (collected) return;
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(basePosition.x, basePosition.y + bob, basePosition.z);

        int order = 250 + Mathf.RoundToInt(-transform.position.y * 10);
        sr.sortingOrder = order;
        foreach (Transform child in transform)
        {
            SpriteRenderer childSr = child.GetComponent<SpriteRenderer>();
            if (childSr != null) childSr.sortingOrder = order - 1;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        Player player = other.GetComponent<Player>();
        if (player == null) return;

        collected = true;
        col.enabled = false;
        player.StartPickup(this);
    }

    public void HideGroundScroll()
    {
        sr.enabled = false;
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);
    }

    public void Collect()
    {
        gameObject.SetActive(false);
        GameController.instance?.OnScrollCollected();
    }
}
