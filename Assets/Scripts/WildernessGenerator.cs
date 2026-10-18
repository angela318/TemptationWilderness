using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class WildernessGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;

    [Header("Ground Tiles")]
    public Sprite[] sandSprites;

    [Header("Scroll")]
    public Sprite scrollSprite;

    [Header("Obstacle Sprites")]
    public Sprite[] obstacleSprites;

    [Header("Decoration Sprites")]
    public Sprite[] decorationSprites;

    [Header("Map Settings")]
    public int mapWidth  = 40;
    public int mapHeight = 40;

    [Header("Spawn Density")]
    [Range(0f, 1f)] public float obstacleDensity   = 0.05f; // 5% of tiles
    [Range(0f, 1f)] public float decorationDensity = 0.08f; // 8% of tiles

    [Header("Tile Weights")]
    public float[] sandWeights = { 40f, 10f, 10f, 10f, 10f, 10f, 10f, 10f, 10f, 10f, 10f };

    // How much of the sprite height the collider covers (from bottom)
    [Header("Obstacle Collider")]
    [Range(0.1f, 1f)] public float colliderHeightRatio = 0.4f;

    Tile[]     sandTiles;
    Transform  obstaclesParent;
    Transform  decorationsParent;
    Transform  scrollsParent;

    // Tracks occupied grid positions to avoid overlapping
    HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    // Clear spawn zone radius around center (player start)
    const int SAFE_RADIUS = 3;

    void Start()
    {
        BuildTiles();
        Generate();
    }

    public void Generate()
    {
        groundTilemap.ClearAllTiles();
        occupied.Clear();

        ClearChildren();

        obstaclesParent   = new GameObject("Obstacles").transform;
        decorationsParent = new GameObject("Decorations").transform;
        scrollsParent     = new GameObject("Scrolls").transform;

        FillGround();
        ScatterObjects(obstacleSprites,   obstacleDensity,   obstaclesParent,   hasCollider: true);
        ScatterObjects(decorationSprites, decorationDensity, decorationsParent, hasCollider: false);
        SpawnScrolls();
    }

    void FillGround()
    {
        int halfW = mapWidth  / 2;
        int halfH = mapHeight / 2;

        for (int x = -halfW; x < halfW; x++)
            for (int y = -halfH; y < halfH; y++)
            {
                Tile tile = PickWeighted(sandTiles, sandWeights);
                groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
    }

    void ScatterObjects(Sprite[] sprites, float density, Transform parent, bool hasCollider)
    {
        if (sprites == null || sprites.Length == 0) return;

        int count = Mathf.RoundToInt(mapWidth * mapHeight * density);
        int halfW = mapWidth  / 2;
        int halfH = mapHeight / 2;
        int attempts = count * 10;

        for (int i = 0; i < count; i++)
        {
            Vector2Int cell = Vector2Int.zero;
            bool found = false;

            for (int a = 0; a < attempts; a++)
            {
                int x = Random.Range(-halfW, halfW);
                int y = Random.Range(-halfH, halfH);
                cell = new Vector2Int(x, y);

                if (!occupied.Contains(cell) && !InSafeZone(cell))
                {
                    found = true;
                    break;
                }
            }

            if (!found) continue;

            occupied.Add(cell);

            Sprite sprite = sprites[Random.Range(0, sprites.Length)];
            SpawnObject(sprite, cell, parent, hasCollider);
        }
    }

    void SpawnObject(Sprite sprite, Vector2Int cell, Transform parent, bool hasCollider)
    {
        GameObject go = new GameObject(sprite.name);
        go.transform.SetParent(parent);

        // Centre the object on the tile, shift up slightly so base sits on ground
        float tileSize = 1f;
        go.transform.position = new Vector3(cell.x + tileSize / 2f, cell.y + tileSize / 2f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1; // render above ground

        if (hasCollider)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();

            // Collider covers only the bottom portion of the sprite
            float spriteH = sprite.bounds.size.y;
            float spriteW = sprite.bounds.size.x;
            float colH    = spriteH * colliderHeightRatio;

            col.size   = new Vector2(spriteW * 0.8f, colH);
            col.offset = new Vector2(0f, -(spriteH / 2f - colH / 2f)); // anchor to bottom
        }
    }

    void SpawnScrolls()
    {
        if (scrollSprite == null) return;

        int count    = GameController.instance != null ? GameController.instance.totalScrolls : 5;
        int halfW    = mapWidth  / 2;
        int halfH    = mapHeight / 2;
        int attempts = count * 20;

        for (int i = 0; i < count; i++)
        {
            Vector2Int cell = Vector2Int.zero;
            bool found = false;

            for (int a = 0; a < attempts; a++)
            {
                int x = Random.Range(-halfW, halfW);
                int y = Random.Range(-halfH, halfH);
                cell = new Vector2Int(x, y);

                if (!occupied.Contains(cell) && !InSafeZone(cell))
                {
                    found = true;
                    break;
                }
            }

            if (!found) continue;

            occupied.Add(cell);

            GameObject go = new GameObject("Scroll");
            go.transform.SetParent(scrollsParent);
            go.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = scrollSprite;
            sr.sortingOrder = 1;

            // Golden glow — same sprite, scaled up, tinted golden, behind scroll
            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale    = Vector3.one * 1.6f;
            SpriteRenderer glowSr = glow.AddComponent<SpriteRenderer>();
            glowSr.sprite       = scrollSprite;
            glowSr.color        = new Color(1f, 0.85f, 0f, 0.25f);
            glowSr.sortingOrder = 0;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.4f;

            go.AddComponent<Scroll>();
        }
    }

    bool InSafeZone(Vector2Int cell)
    {
        return Mathf.Abs(cell.x) <= SAFE_RADIUS && Mathf.Abs(cell.y) <= SAFE_RADIUS;
    }

    void BuildTiles()
    {
        sandTiles = new Tile[sandSprites.Length];
        for (int i = 0; i < sandSprites.Length; i++)
        {
            Tile t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sandSprites[i];
            sandTiles[i] = t;
        }

        if (sandWeights == null || sandWeights.Length != sandSprites.Length)
        {
            float[] w = new float[sandSprites.Length];
            for (int i = 0; i < w.Length; i++)
                w[i] = (sandWeights != null && i < sandWeights.Length) ? sandWeights[i] : 10f;
            sandWeights = w;
        }
    }

    void ClearChildren()
    {
        GameObject old;
        old = GameObject.Find("Obstacles");   if (old) Destroy(old);
        old = GameObject.Find("Decorations"); if (old) Destroy(old);
        old = GameObject.Find("Scrolls");     if (old) Destroy(old);
    }

    Tile PickWeighted(Tile[] tiles, float[] weights)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++) total += weights[i];

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < tiles.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) return tiles[i];
        }

        return tiles[0];
    }
}
