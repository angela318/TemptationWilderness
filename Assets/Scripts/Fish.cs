using System.Collections;
using UnityEngine;

public class Fish : MonoBehaviour
{
    [SerializeField] public Sprite bonusSprite;  // assign bonus_point.png in the prefab Inspector

    private float startY;
    private float phase;
    private SpriteRenderer sr;
    private bool collected = false;

    private const float BOB_SPEED  = 2f;
    private const float BOB_HEIGHT = 0.2f;

    void Start()
    {
        startY = transform.position.y;
        phase  = Random.Range(0f, Mathf.PI * 2f);
        sr     = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (collected) return;
        float newY = startY + Mathf.Sin(Time.time * BOB_SPEED + phase) * BOB_HEIGHT;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collected || !collision.CompareTag("Whale")) return;

        collected = true;
        Whale whale = collision.GetComponent<Whale>();
        if (whale != null) whale.CollectFish();
        AudioManager.instance?.PlayBonusPop();

        GetComponent<CircleCollider2D>().enabled = false;
        StartCoroutine(CollectAnimation());
    }

    IEnumerator CollectAnimation()
    {
        // Spawn the +15 bonus bubble at the fish's current position
        GameObject bubble = null;
        SpriteRenderer bubbleSr = null;
        if (bonusSprite != null)
        {
            bubble = new GameObject("BonusPoint");
            bubbleSr = bubble.AddComponent<SpriteRenderer>();
            bubbleSr.sprite = bonusSprite;
            bubbleSr.sortingOrder = 2;
            bubble.transform.position = transform.position;
            bubble.transform.localScale = transform.localScale;
        }

        float elapsed       = 0f;
        float fishDuration   = 0.35f;  // fish pop lasts 0.35s
        float bubbleDuration = 0.8f;   // bubble floats up over 0.8s
        float totalDuration  = Mathf.Max(fishDuration, bubbleDuration);

        Vector3 fishStartScale = transform.localScale;
        Vector3 fishEndScale   = fishStartScale * 2f;
        Vector3 bubbleStartPos = transform.position;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            // Fish: scale up and fade out
            float fishT = Mathf.Clamp01(elapsed / fishDuration);
            transform.localScale = Vector3.Lerp(fishStartScale, fishEndScale, fishT);
            sr.color = new Color(1f, 1f, 1f, 1f - fishT);

            // Bubble: float upward and fade out
            if (bubble != null)
            {
                float bubbleT = Mathf.Clamp01(elapsed / bubbleDuration);
                bubble.transform.position = bubbleStartPos + Vector3.up * 1.5f * bubbleT;
                bubbleSr.color = new Color(1f, 1f, 1f, 1f - bubbleT);
            }

            yield return null;
        }

        if (bubble != null) Destroy(bubble);
        gameObject.SetActive(false);
    }
}
