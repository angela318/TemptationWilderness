using UnityEngine;

public class ShipChain : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private bool  anchorDrops       = true;
    [SerializeField] private bool  freezeAtLastFrame = false;
    [SerializeField] private float frameRate             = 0.1f;  // seconds per frame
    [SerializeField] private float firstFrameHoldMultiplier =  0f;  // hold at frame 0 — use for chain_up
    [SerializeField] private float lastFrameHoldMultiplier  = 15f;  // hold at last frame — use for chain_down
    [SerializeField] private int   sortingOrder = 0;

    [SerializeField] private float chainOffsetX   =  1f;
    [SerializeField] private float chainStartY    = -1f;
    [SerializeField] private float minChainLength =  0.3f;
    [SerializeField] private float maxChainLength =  1.5f;
    [SerializeField] private float anchorWidth    =  0.5f;
    [SerializeField] private float anchorHeight   =  0.4f;

    private SpriteRenderer    sr;
    private AudioSource       chainAudio;
    private CapsuleCollider2D chainCol;

    private int   currentFrame = 0;
    private int   direction    = 1;
    private int   maxFrame;
    private float timer     = 0f;
    private float holdTimer = 0f;
    private bool  isMoving  = false;

    void Start()
    {
        sr         = GetComponent<SpriteRenderer>();
        chainAudio = GetComponent<AudioSource>();
        chainAudio.loop        = true;
        chainAudio.playOnAwake = false;
        sr.sortingOrder        = sortingOrder;

        maxFrame = frames.Length - 1;

        GameObject anchorObj = new GameObject("AnchorCollider");
        anchorObj.transform.SetParent(transform, false);
        anchorObj.tag = "Obstacle";
        chainCol = anchorObj.AddComponent<CapsuleCollider2D>();
        chainCol.direction = CapsuleDirection2D.Vertical;

        currentFrame = freezeAtLastFrame ? frames.Length - 1 : Random.Range(0, maxFrame + 1);
        if (frames.Length > 0) sr.sprite = frames[currentFrame];
        UpdateAnchorCollider();
    }

    void Update()
    {
        UpdateAnchorCollider();

        if (sr.isVisible && isMoving)
        {
            if (!chainAudio.isPlaying) chainAudio.Play();
        }
        else
        {
            if (chainAudio.isPlaying) chainAudio.Stop();
        }

        if (frames.Length < 2 || freezeAtLastFrame) return;

        if (holdTimer > 0f)
        {
            holdTimer -= Time.deltaTime;
            isMoving = false;
            return;
        }

        isMoving = true;
        timer += Time.deltaTime;
        while (timer >= frameRate)
        {
            timer -= frameRate;
            AdvanceFrame();
            if (holdTimer > 0f) break;
        }
    }

    void AdvanceFrame()
    {
        currentFrame += direction;

        if (currentFrame >= maxFrame)
        {
            currentFrame = maxFrame;
            direction    = -1;
            holdTimer    = lastFrameHoldMultiplier * frameRate;
        }
        else if (currentFrame <= 0)
        {
            currentFrame = 0;
            direction    = 1;
            holdTimer    = firstFrameHoldMultiplier * frameRate;
        }

        sr.sprite = frames[currentFrame];
        UpdateAnchorCollider();
    }

    void UpdateAnchorCollider()
    {
        if (chainCol == null || frames.Length == 0) return;

        float t           = maxFrame > 0 ? (float)currentFrame / maxFrame : 0f;
        float chainLength = Mathf.Lerp(minChainLength, maxChainLength, t);

        float anchorCenterY = anchorDrops
            ? chainStartY - chainLength - anchorHeight / 2f
            : chainStartY + chainLength + anchorHeight / 2f;

        chainCol.size   = new Vector2(anchorWidth, anchorHeight);
        chainCol.offset = new Vector2(chainOffsetX, anchorCenterY);
    }
}
