using UnityEngine;

public class Creature : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite frontSprite;  // facing toward camera — shown while idle
    public Sprite sideSprite;   // facing sideways (optional)
    public Sprite backSprite;   // facing away (optional)

    [Header("Settings")]
    public float detectionRadius      = 2.5f;
    public float fleeSpeed            = 2.5f;
    public float oscillationSpeed     = 8f;   // how fast the wobble cycles
    public float oscillationAmplitude = 15f;  // max tilt in degrees

    SpriteRenderer   sr;
    CircleCollider2D trigger;
    bool             fleeing = false;
    Vector2          fleeDir;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Start runs after the generator has set all public fields
    void Start()
    {
        RandomizeIdleFacing();
        UpdateSortingOrder();

        trigger           = gameObject.AddComponent<CircleCollider2D>();
        trigger.radius    = detectionRadius;
        trigger.isTrigger = true;
    }

    void Update()
    {
        if (!fleeing) return;

        float s = Mathf.Sin(Time.time * oscillationSpeed);

        // Sprite tilt
        transform.rotation = Quaternion.Euler(0f, 0f, s * oscillationAmplitude);

        // Direction wobble — slightly offset phase so tilt and path feel independent
        float dirRad = Mathf.Sin(Time.time * oscillationSpeed + 0.5f) * 0.3f;
        float dc  = Mathf.Cos(dirRad);
        float ds  = Mathf.Sin(dirRad);
        Vector2 dir = new Vector2(
            fleeDir.x * dc - fleeDir.y * ds,
            fleeDir.x * ds + fleeDir.y * dc);

        transform.Translate(dir * fleeSpeed * Time.deltaTime, Space.World);
        UpdateSortingOrder();

        Camera  cam    = Camera.main;
        Vector3 camPos = cam.transform.position;
        float   camH   = cam.orthographicSize   + 1f;  // +1 world-unit margin
        float   camW   = camH * cam.aspect;
        Vector3 pos    = transform.position;
        if (pos.x < camPos.x - camW || pos.x > camPos.x + camW ||
            pos.y < camPos.y - camH || pos.y > camPos.y + camH)
            gameObject.SetActive(false);
    }

    void RandomizeIdleFacing()
    {
        // Build a pool of available sprites and pick one at random
        var options = new System.Collections.Generic.List<(Sprite sprite, bool flip)>();
        if (frontSprite != null) options.Add((frontSprite, false));
        if (sideSprite  != null) { options.Add((sideSprite, false)); options.Add((sideSprite, true)); }
        if (backSprite  != null) options.Add((backSprite,  false));

        if (options.Count == 0) return;
        var chosen = options[Random.Range(0, options.Count)];
        sr.sprite = chosen.sprite;
        sr.flipX  = chosen.flip;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (fleeing) return;
        if (other.GetComponent<Player>() == null) return;
        StartFlee((Vector2)other.transform.position);
    }

    void StartFlee(Vector2 playerPos)
    {
        fleeing         = true;
        trigger.enabled = false;

        Vector2 away = ((Vector2)transform.position - playerPos).normalized;

        // Scatter slightly so a group of ants doesn't all flee identically
        float jitter = Random.Range(-25f, 25f) * Mathf.Deg2Rad;
        fleeDir = new Vector2(
            away.x * Mathf.Cos(jitter) - away.y * Mathf.Sin(jitter),
            away.x * Mathf.Sin(jitter) + away.y * Mathf.Cos(jitter)).normalized;

        UpdateFacingSprite();
    }

    void UpdateFacingSprite()
    {
        if (!fleeing) { sr.sprite = frontSprite; sr.flipX = false; return; }

        if (Mathf.Abs(fleeDir.x) >= Mathf.Abs(fleeDir.y))
        {
            // Side — assumes side sprite faces right; flip for left movement
            sr.sprite = sideSprite != null ? sideSprite : frontSprite;
            sr.flipX  = fleeDir.x < 0;
        }
        else if (fleeDir.y > 0)
        {
            // Fleeing upward (away from camera)
            sr.sprite = backSprite  != null ? backSprite  :
                        sideSprite  != null ? sideSprite  : frontSprite;
            sr.flipX  = false;
        }
        else
        {
            // Fleeing downward (toward camera)
            sr.sprite = frontSprite;
            sr.flipX  = false;
        }
    }

    void UpdateSortingOrder()
    {
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 10);
    }



}
