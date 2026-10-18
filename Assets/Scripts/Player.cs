using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Player : MonoBehaviour
{
    [Header("Side Sprites")]
    public Sprite   sideStop;
    public Sprite[] sideWalk;   // side-walk1 to side-walk4
    public Sprite[] sidePick;   // side-pick1 to side-pick6

    // Down/Up sprites — leave empty until you shoot them
    [Header("Down Sprites (optional)")]
    public Sprite   downStop;
    public Sprite[] downWalk;
    public Sprite[] downPick;

    [Header("Up Sprites (optional)")]
    public Sprite   upStop;
    public Sprite[] upWalk;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Animation")]
    public float walkFrameRate = 0.12f; // seconds per frame

    [Header("Scroll In Hand")]
    public SpriteRenderer scrollInHand; // child object — assign in Inspector
    public Sprite          scrollSprite;
    public Vector2         scrollHandOffset = new Vector2(0.3f, 0.3f);

    // ── state ──────────────────────────────────────────────────────────────
    enum Direction { Down, Up, Side }

    Rigidbody2D  rb;
    SpriteRenderer sr;
    Direction    facing      = Direction.Down;
    bool         facingLeft  = false;
    bool         isPicking   = false;
    bool         canMove     = true;
    int          walkFrame   = 0;
    float        walkTimer   = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        rb.gravityScale  = 0;
        rb.freezeRotation = true;

        if (scrollInHand != null)
            scrollInHand.enabled = false;
    }

    void Update()
    {
        if (isPicking) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(h, v).normalized;

        if (canMove)
            rb.velocity = input * moveSpeed;
        else
            rb.velocity = Vector2.zero;

        UpdateFacing(input);
        UpdateAnimation(input);
    }

    void UpdateFacing(Vector2 input)
    {
        if (input.x != 0)
        {
            facing     = Direction.Side;
            facingLeft = input.x < 0;
        }
        else if (input.y < 0) facing = Direction.Down;
        else if (input.y > 0) facing = Direction.Up;
    }

    void UpdateAnimation(Vector2 input)
    {
        bool moving = input.sqrMagnitude > 0;

        if (!moving)
        {
            walkFrame = 0;
            sr.sprite = IdleSprite();
            sr.flipX  = facingLeft;
            return;
        }

        walkTimer += Time.deltaTime;
        if (walkTimer >= walkFrameRate)
        {
            walkTimer = 0f;
            Sprite[] frames = WalkFrames();
            if (frames != null && frames.Length > 0)
            {
                walkFrame = (walkFrame + 1) % frames.Length;
                sr.sprite = frames[walkFrame];
                sr.flipX  = (facing == Direction.Side) && facingLeft;
            }
        }
    }

    // ── Public: called by Scroll when player enters its trigger ────────────
    public void StartPickup(Scroll scroll)
    {
        if (isPicking) return;
        StartCoroutine(PickupRoutine(scroll));
    }

    IEnumerator PickupRoutine(Scroll scroll)
    {
        isPicking = true;
        rb.velocity = Vector2.zero;

        // Face toward the scroll — side if mostly horizontal, else down
        Vector2 diff = scroll.transform.position - transform.position;
        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
        {
            facing     = Direction.Side;
            facingLeft = diff.x < 0;
        }
        else
        {
            facing = Direction.Down;
        }

        Sprite[] frames = PickFrames();
        if (frames == null || frames.Length == 0)
        {
            FinishPickup(scroll);
            yield break;
        }

        sr.flipX = (facing == Direction.Side) && facingLeft;

        for (int i = 0; i < frames.Length; i++)
        {
            sr.sprite = frames[i];

            // At frame index 2 (pick3): hide ground scroll, show in hand
            if (i == 2)
            {
                scroll.HideGroundScroll();
                ShowScrollInHand();
            }

            yield return new WaitForSeconds(0.1f);
        }

        HideScrollInHand();
        FinishPickup(scroll);
    }

    void FinishPickup(Scroll scroll)
    {
        scroll.Collect();
        isPicking = false;
        sr.sprite = IdleSprite();
    }

    void ShowScrollInHand()
    {
        if (scrollInHand == null) return;
        scrollInHand.sprite  = scrollSprite;
        scrollInHand.enabled = true;
        float offsetX = facingLeft ? -scrollHandOffset.x : scrollHandOffset.x;
        scrollInHand.transform.localPosition = new Vector3(offsetX, scrollHandOffset.y, 0f);
    }

    void HideScrollInHand()
    {
        if (scrollInHand != null)
            scrollInHand.enabled = false;
    }

    // ── Sprite helpers ─────────────────────────────────────────────────────
    Sprite IdleSprite()
    {
        if (facing == Direction.Up   && upStop   != null) return upStop;
        if (facing == Direction.Down && downStop  != null) return downStop;
        return sideStop;
    }

    Sprite[] WalkFrames()
    {
        if (facing == Direction.Up   && upWalk   != null && upWalk.Length   > 0) return upWalk;
        if (facing == Direction.Down && downWalk  != null && downWalk.Length  > 0) return downWalk;
        return sideWalk;
    }

    Sprite[] PickFrames()
    {
        if (facing == Direction.Down && downPick != null && downPick.Length > 0) return downPick;
        return sidePick;
    }
}
