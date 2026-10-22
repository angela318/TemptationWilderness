using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public class ObstacleEntry
{
    public Sprite sprite;
    public int    count              = 3;
    public bool   isLandmark         = false;
    [Range(0.1f, 2f)] public float colliderWidthRatio  = 0.8f;
    [Range(0.1f, 1f)] public float colliderHeightRatio = 0.4f;
    public bool   useCapsuleCollider  = false;
    public float  capsuleAngle        = 0f;   // rotation degrees for capsule collider (e.g. 45 for diagonal log)
    public bool   alwaysBehindPlayer  = false; // use decoration sort range — player always renders on top
    public bool   hidesScroll         = false;
    public float  scrollXOffset       = 0f;
    public float  scrollYOffset       = 0.3f;
}

[System.Serializable]
public class CreatureEntry
{
    public Sprite frontSprite;
    public Sprite sideSprite;
    public Sprite backSprite;
    public int    count           = 4;
    public float  detectionRadius      = 2.5f;
    public float  fleeSpeed            = 2.5f;
    public float  oscillationSpeed     = 8f;
    public float  oscillationAmplitude = 15f;
}

public class WildernessGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;

    [Header("Ground Tiles")]
    public Sprite[] sandSprites;

    [Header("Scroll")]
    public Sprite scrollSprite;

    [Header("Obstacles")]
    public ObstacleEntry[] obstacles;
    public int   obstacleEdgeMargin = 2;
    public float minClearance       = 1.5f;  // world-unit gap between any two obstacle edges

    [Header("Landmarks")]
    public float landmarkSpacing = 12f;      // min world-unit distance between landmark centres

    [Header("Close Grouping (ordinary obstacles only)")]
    [Range(0f, 1f)] public float clusterChance = 0.25f;
    public float clusterRadius = 2.5f;

    [Header("Uneven Distribution")]
    public int   emptyZoneCount    = 4;
    public float emptyZoneRadius   = 7f;
    [Range(0f, 1f)] public float emptyZoneStrength = 0.85f;

    [Header("Creatures")]
    public CreatureEntry[] creatures;

    [Header("Decoration Sprites")]
    public Sprite[] decorationSprites;

    [Header("Map Settings")]
    public int mapWidth  = 40;
    public int mapHeight = 40;

    [Header("Spawn Density")]
    [Range(0f, 1f)] public float decorationDensity = 0.08f;

    [Header("Tile Weights")]
    public float[] sandWeights = { 40f, 10f, 10f, 10f, 10f, 10f, 10f, 10f, 10f, 10f, 10f };

    [Header("Scroll Protection")]
    public float scrollProtectRadius = 1.5f;
    public float minScrollSpacing    = 8f;   // min world-units between any two scrolls

    // ── runtime state ──────────────────────────────────────────────────────
    Tile[]    sandTiles;
    Transform obstaclesParent;
    Transform decorationsParent;
    Transform scrollsParent;
    Transform creaturesParent;

    HashSet<Vector2Int>              occupied        = new HashSet<Vector2Int>();
    HashSet<Vector2Int>              decorOccupied   = new HashSet<Vector2Int>();
    Dictionary<Sprite, List<(Vector2, float, float)>> hidePoints
                                                    = new Dictionary<Sprite, List<(Vector2, float, float)>>();
    Dictionary<int, HashSet<Sprite>> segmentSprites = new Dictionary<int, HashSet<Sprite>>();
    List<(Vector2 pos, float radius)> placedFootprints = new List<(Vector2, float)>();
    List<Vector2> landmarkPositions = new List<Vector2>();
    List<Vector2> ordinaryPositions = new List<Vector2>();
    List<Vector2> scrollPositions   = new List<Vector2>();
    List<Vector2> emptyZoneCenters  = new List<Vector2>();

    const int SAFE_RADIUS = 3;
    const int EDGE_MARGIN = 4;

    void Start()
    {
        BuildTiles();
        Generate();
    }

    public void Generate()
    {
        groundTilemap.ClearAllTiles();
        occupied.Clear();
        decorOccupied.Clear();
        hidePoints.Clear();
        segmentSprites.Clear();
        placedFootprints.Clear();
        landmarkPositions.Clear();
        ordinaryPositions.Clear();
        scrollPositions.Clear();
        emptyZoneCenters.Clear();

        ClearChildren();
        obstaclesParent   = new GameObject("Obstacles").transform;
        decorationsParent = new GameObject("Decorations").transform;
        scrollsParent     = new GameObject("Scrolls").transform;
        creaturesParent   = new GameObject("Creatures").transform;

        FillGround();
        GenerateEmptyZones();
        SpawnObstacles();
        SpawnScrolls();
        SpawnCreatures();
        ScatterObjects(decorationSprites, decorationDensity, decorationsParent,
                       hasCollider: false, jitter: 0.4f, randomFlip: true);
    }

    // ── empty zones ────────────────────────────────────────────────────────

    void GenerateEmptyZones()
    {
        int halfW = mapWidth  / 2 - obstacleEdgeMargin - 2;
        int halfH = mapHeight / 2 - obstacleEdgeMargin - 2;
        for (int i = 0; i < emptyZoneCount; i++)
            emptyZoneCenters.Add(new Vector2(
                Random.Range(-halfW, halfW),
                Random.Range(-halfH, halfH)));
    }

    bool InEmptyZone(Vector2 worldPos)
    {
        foreach (Vector2 c in emptyZoneCenters)
        {
            float dist = Vector2.Distance(worldPos, c);
            if (dist < emptyZoneRadius)
            {
                float t = 1f - dist / emptyZoneRadius;
                if (Random.value < t * emptyZoneStrength)
                    return true;
            }
        }
        return false;
    }

    // ── obstacle spawning ──────────────────────────────────────────────────

    void SpawnObstacles()
    {
        if (obstacles == null) return;

        var landmarks = new List<ObstacleEntry>();
        var hiders    = new List<ObstacleEntry>();
        var ordinary  = new List<ObstacleEntry>();

        foreach (ObstacleEntry e in obstacles)
        {
            if (e.sprite == null || e.count <= 0) continue;
            if      (e.isLandmark)  landmarks.Add(e);
            else if (e.hidesScroll) hiders.Add(e);
            else                    ordinary.Add(e);
        }

        // Pass 1 — landmarks: wide separation, no empty-zone rejection, second instance flipped
        foreach (ObstacleEntry entry in landmarks)
            for (int i = 0; i < entry.count; i++)
            {
                Vector2Int cell;
                if (FindCell(entry, isLandmark: true, out cell))
                    PlaceObstacle(entry, cell, instanceIndex: i);
            }

        // Pass 2 — scroll-hiding: normal clearance, protected, no clustering
        foreach (ObstacleEntry entry in hiders)
            for (int i = 0; i < entry.count; i++)
            {
                Vector2Int cell;
                if (FindCell(entry, isLandmark: false, out cell))
                    PlaceObstacle(entry, cell, instanceIndex: i);
            }

        // Pass 3 — ordinary: normal clearance with optional close clustering
        foreach (ObstacleEntry entry in ordinary)
            for (int i = 0; i < entry.count; i++)
            {
                bool tryCluster = ordinaryPositions.Count > 0 && Random.value < clusterChance;
                Vector2Int cell;
                if (tryCluster && FindClusterCell(entry, out cell))
                {
                    PlaceObstacle(entry, cell, instanceIndex: i);
                    continue;
                }
                if (FindCell(entry, isLandmark: false, out cell))
                    PlaceObstacle(entry, cell, instanceIndex: i);
            }
    }

    bool FindCell(ObstacleEntry entry, bool isLandmark, out Vector2Int result)
    {
        int halfW    = mapWidth  / 2;
        int halfH    = mapHeight / 2;
        float radius = GetObstacleRadius(entry);

        for (int a = 0; a < entry.count * 15; a++)
        {
            int x = Random.Range(-halfW + obstacleEdgeMargin, halfW - obstacleEdgeMargin);
            int y = Random.Range(-halfH + obstacleEdgeMargin, halfH - obstacleEdgeMargin);
            Vector2Int cell    = new Vector2Int(x, y);
            Vector2    worldPos = new Vector2(cell.x + 0.5f, cell.y + 0.5f);

            if (occupied.Contains(cell))                 continue;
            if (InSafeZone(cell))                        continue;
            if (InEdgeZone(cell))                        continue;
            if (SameTypeInSegment(cell, entry.sprite))   continue;
            if (!isLandmark && InEmptyZone(worldPos))    continue;
            if (FootprintOverlaps(worldPos, radius, minClearance)) continue;
            if (isLandmark && LandmarkTooClose(worldPos)) continue;

            result = cell;
            return true;
        }
        result = Vector2Int.zero;
        return false;
    }

    bool FindClusterCell(ObstacleEntry entry, out Vector2Int result)
    {
        float  radius = GetObstacleRadius(entry);
        Vector2 anchor = ordinaryPositions[Random.Range(0, ordinaryPositions.Count)];

        for (int a = 0; a < 20; a++)
        {
            float angle   = Random.Range(0f, Mathf.PI * 2f);
            float dist    = Random.Range(radius * 2f + 0.3f, clusterRadius);
            Vector2    wPos = anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            Vector2Int cell = new Vector2Int(Mathf.FloorToInt(wPos.x), Mathf.FloorToInt(wPos.y));

            if (occupied.Contains(cell)) continue;
            if (InSafeZone(cell))        continue;
            if (InEdgeZone(cell))        continue;
            if (FootprintOverlaps(wPos, radius, 0f)) continue;

            result = cell;
            return true;
        }
        result = Vector2Int.zero;
        return false;
    }

    void PlaceObstacle(ObstacleEntry entry, Vector2Int cell, int instanceIndex)
    {
        Vector2 worldPos = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
        float   radius   = GetObstacleRadius(entry);

        occupied.Add(cell);
        placedFootprints.Add((worldPos, radius));
        RegisterSegment(cell, entry.sprite);

        if (!entry.isLandmark && !entry.hidesScroll)
            ordinaryPositions.Add(worldPos);

        if (entry.isLandmark)
            landmarkPositions.Add(worldPos);

        if (entry.hidesScroll)
        {
            if (!hidePoints.ContainsKey(entry.sprite))
                hidePoints[entry.sprite] = new List<(Vector2, float, float)>();
            bool flipped = entry.isLandmark && instanceIndex == 1;
            float xOff = flipped ? -entry.scrollXOffset : entry.scrollXOffset;
            hidePoints[entry.sprite].Add((worldPos, xOff, entry.scrollYOffset));
        }

        SpriteRenderer sr = SpawnObject(entry.sprite, cell, obstaclesParent, hasCollider: true,
            colliderWidthRatio:  entry.colliderWidthRatio,
            colliderHeightRatio: entry.colliderHeightRatio,
            useCapsule:          entry.useCapsuleCollider,
            capsuleAngle:        entry.capsuleAngle,
            behindPlayer:        entry.alwaysBehindPlayer);

        if (entry.isLandmark && instanceIndex == 1) sr.flipX = true;
    }

    float GetObstacleRadius(ObstacleEntry entry)
    {
        if (entry.sprite == null) return 0.5f;
        float w = entry.sprite.bounds.size.x * entry.colliderWidthRatio  * 0.5f;
        float h = entry.sprite.bounds.size.y * entry.colliderHeightRatio * 0.5f;
        return Mathf.Max(w, h, 0.3f);
    }

    bool FootprintOverlaps(Vector2 center, float radius, float clearance)
    {
        foreach (var (pos, r) in placedFootprints)
            if (Vector2.Distance(center, pos) < r + radius + clearance)
                return true;
        return false;
    }

    bool LandmarkTooClose(Vector2 worldPos)
    {
        foreach (Vector2 lp in landmarkPositions)
            if (Vector2.Distance(worldPos, lp) < landmarkSpacing)
                return true;
        return false;
    }

    // ── ground ────────────────────────────────────────────────────────────

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

    // ── decorations ───────────────────────────────────────────────────────

    void ScatterObjects(Sprite[] sprites, float density, Transform parent, bool hasCollider,
                        float jitter = 0f, bool randomFlip = false)
    {
        if (sprites == null || sprites.Length == 0) return;

        int count    = Mathf.RoundToInt(mapWidth * mapHeight * density);
        int halfW    = mapWidth  / 2;
        int halfH    = mapHeight / 2;
        int attempts = count * 10;

        for (int i = 0; i < count; i++)
        {
            Vector2Int cell  = Vector2Int.zero;
            Vector2    wPos  = Vector2.zero;
            bool found = false;

            for (int a = 0; a < attempts; a++)
            {
                int x = Random.Range(-halfW, halfW);
                int y = Random.Range(-halfH, halfH);
                cell = new Vector2Int(x, y);

                Vector2 jitteredPos = new Vector2(
                    cell.x + 0.5f + (jitter > 0f ? Random.Range(-jitter, jitter) : 0f),
                    cell.y + 0.5f + (jitter > 0f ? Random.Range(-jitter, jitter) : 0f));

                if (decorOccupied.Contains(cell)) continue;
                if (InSafeZone(cell))             continue;
                if (NearScroll(jitteredPos))      continue;

                wPos  = jitteredPos;
                found = true;
                break;
            }

            if (!found) continue;

            decorOccupied.Add(cell);

            Sprite sprite = sprites[Random.Range(0, sprites.Length)];
            Vector2 offset = wPos - new Vector2(cell.x + 0.5f, cell.y + 0.5f);
            SpriteRenderer sr = SpawnObject(sprite, cell, parent, hasCollider, offset);
            if (randomFlip) sr.flipX = Random.value > 0.5f;
        }
    }

    bool NearScroll(Vector2 worldPos)
    {
        foreach (Vector2 sp in scrollPositions)
            if (Vector2.Distance(worldPos, sp) < scrollProtectRadius)
                return true;
        return false;
    }

    float MinDistToScrolls(Vector2 worldPos)
    {
        if (scrollPositions.Count == 0) return float.MaxValue;
        float min = float.MaxValue;
        foreach (Vector2 sp in scrollPositions)
            min = Mathf.Min(min, Vector2.Distance(worldPos, sp));
        return min;
    }

    // ── spawn object ──────────────────────────────────────────────────────

    SpriteRenderer SpawnObject(Sprite sprite, Vector2Int cell, Transform parent, bool hasCollider,
        Vector2 posOffset = default, float colliderWidthRatio = 0.8f,
        float colliderHeightRatio = 0.4f, bool useCapsule = false, float capsuleAngle = 0f,
        bool behindPlayer = false)
    {
        GameObject go = new GameObject(sprite.name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(cell.x + 0.5f + posOffset.x, cell.y + 0.5f + posOffset.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // behindPlayer or decorations: omit +250 so they always sort below player/obstacles
        bool useHighRange = hasCollider && !behindPlayer;
        sr.sortingOrder = useHighRange
            ? 250 + Mathf.RoundToInt(-go.transform.position.y * 10)
            :       Mathf.RoundToInt(-go.transform.position.y * 10);

        if (hasCollider)
        {
            float colW = sprite.bounds.size.x * colliderWidthRatio;
            float colH = sprite.bounds.size.y * colliderHeightRatio;

            if (useCapsule)
            {
                // Put capsule on a child so it can be rotated independently of the sprite.
                // Anchor the child at the sprite's visual centre (bounds.center is relative to pivot).
                GameObject colChild = new GameObject("Collider");
                colChild.transform.SetParent(go.transform);
                colChild.transform.localPosition = new Vector3(sprite.bounds.center.x,
                                                               sprite.bounds.center.y, 0f);
                colChild.transform.localRotation = Quaternion.Euler(0f, 0f, capsuleAngle);

                CapsuleCollider2D cap = colChild.AddComponent<CapsuleCollider2D>();
                cap.size      = new Vector2(colW, colH);
                cap.offset    = Vector2.zero;
                cap.direction = CapsuleDirection2D.Horizontal;
            }
            else
            {
                BoxCollider2D col = go.AddComponent<BoxCollider2D>();
                col.size   = new Vector2(colW, colH);
                col.offset = new Vector2(0f, colH * 0.5f);
            }
        }

        return sr;
    }

    // ── scrolls ───────────────────────────────────────────────────────────

    void SpawnScrolls()
    {
        if (scrollSprite == null) return;

        int count = GameController.instance != null ? GameController.instance.totalScrolls : 5;
        int halfW = mapWidth  / 2;
        int halfH = mapHeight / 2;

        var available = new Dictionary<Sprite, List<(Vector2, float, float)>>();
        foreach (var kv in hidePoints)
            available[kv.Key] = new List<(Vector2, float, float)>(kv.Value);
        List<Sprite> typeOrder = new List<Sprite>(available.Keys);
        int typeIndex = 0;

        for (int i = 0; i < count; i++)
        {
            Vector2 worldPos = Vector2.zero;
            bool found = false;

            int tries = typeOrder.Count;
            while (tries > 0 && !found)
            {
                if (typeOrder.Count == 0) break;
                typeIndex = typeIndex % typeOrder.Count;
                Sprite type = typeOrder[typeIndex];
                var pool = available[type];

                // Pick the candidate furthest from all already-placed scrolls.
                // Track best by value — avoids stale index after invalid entries are removed.
                (Vector2, float, float)? best     = null;
                float                   bestDist  = -1f;
                for (int j = pool.Count - 1; j >= 0; j--)
                {
                    var item = pool[j];
                    (Vector2 obs, float xOff, float yOff) = item;
                    Vector2    candidate  = new Vector2(obs.x + xOff, obs.y + yOff);
                    Vector2Int approxCell = new Vector2Int(Mathf.FloorToInt(candidate.x), Mathf.FloorToInt(candidate.y));

                    if (InSafeZone(approxCell) || InEdgeZone(approxCell))
                    {
                        pool.RemoveAt(j); // permanently invalid, discard
                        continue;
                    }

                    float dist = MinDistToScrolls(candidate);
                    if (dist >= minScrollSpacing && dist > bestDist)
                    {
                        bestDist = dist;
                        best     = item;
                    }
                }

                if (best.HasValue)
                {
                    pool.Remove(best.Value);
                    (Vector2 obs, float xOff, float yOff) = best.Value;
                    worldPos = new Vector2(obs.x + xOff, obs.y + yOff);
                    found    = true;
                }

                if (pool.Count == 0) typeOrder.RemoveAt(typeIndex);
                else typeIndex++;
                tries--;
            }

            if (!found)
            {
                for (int a = 0; a < count * 20; a++)
                {
                    int x = Random.Range(-halfW + EDGE_MARGIN, halfW - EDGE_MARGIN);
                    int y = Random.Range(-halfH + EDGE_MARGIN, halfH - EDGE_MARGIN);
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!occupied.Contains(cell) && !InSafeZone(cell))
                    {
                        worldPos = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                        occupied.Add(cell);
                        found = true;
                        break;
                    }
                }
            }

            if (!found) continue;

            scrollPositions.Add(worldPos);

            GameObject go = new GameObject("Scroll");
            go.transform.SetParent(scrollsParent);
            go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = scrollSprite;
            sr.sortingOrder = 1;

            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform);
            float h = scrollSprite.bounds.size.y;
            glow.transform.localPosition = new Vector3(0f, -h * 0.2f, 0f);
            glow.transform.localScale    = Vector3.one * 1.4f;
            SpriteRenderer glowSr = glow.AddComponent<SpriteRenderer>();
            glowSr.sprite       = scrollSprite;
            glowSr.color        = new Color(1f, 0.85f, 0f, 0.3f);
            glowSr.sortingOrder = 0;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.4f;

            go.AddComponent<Scroll>();
        }
    }

    // ── creatures ─────────────────────────────────────────────────────────

    void SpawnCreatures()
    {
        if (creatures == null) return;
        int halfW = mapWidth  / 2;
        int halfH = mapHeight / 2;

        foreach (CreatureEntry entry in creatures)
        {
            if (entry.frontSprite == null || entry.count <= 0) continue;

            for (int i = 0; i < entry.count; i++)
            {
                bool placed = false;
                for (int a = 0; a < 30; a++)
                {
                    float      x    = Random.Range(-halfW + obstacleEdgeMargin, halfW - obstacleEdgeMargin)
                                      + Random.Range(-0.4f, 0.4f);
                    float      y    = Random.Range(-halfH + obstacleEdgeMargin, halfH - obstacleEdgeMargin)
                                      + Random.Range(-0.4f, 0.4f);
                    Vector2Int cell = new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y));

                    if (InSafeZone(cell)) continue;

                    GameObject go = new GameObject(entry.frontSprite.name);
                    go.transform.SetParent(creaturesParent);
                    go.transform.position = new Vector3(x, y, 0f);

                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite       = entry.frontSprite;
                    sr.sortingOrder = Mathf.RoundToInt(-y * 10);

                    Creature c = go.AddComponent<Creature>();
                    c.frontSprite     = entry.frontSprite;
                    c.sideSprite      = entry.sideSprite;
                    c.backSprite      = entry.backSprite;
                    c.detectionRadius      = entry.detectionRadius;
                    c.fleeSpeed            = entry.fleeSpeed;
                    c.oscillationSpeed     = entry.oscillationSpeed;
                    c.oscillationAmplitude = entry.oscillationAmplitude;

                    placed = true;
                    break;
                }
                if (!placed) Debug.LogWarning($"Could not place creature: {entry.frontSprite.name}");
            }
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────

    int SegmentKey(Vector2Int cell)
    {
        int segX = Mathf.Clamp((cell.x + mapWidth  / 2) / (mapWidth  / 4), 0, 3);
        int segY = Mathf.Clamp((cell.y + mapHeight / 2) / (mapHeight / 4), 0, 3);
        return segX * 4 + segY;
    }

    bool SameTypeInSegment(Vector2Int cell, Sprite sprite)
    {
        int key = SegmentKey(cell);
        return segmentSprites.TryGetValue(key, out HashSet<Sprite> set) && set.Contains(sprite);
    }

    void RegisterSegment(Vector2Int cell, Sprite sprite)
    {
        int key = SegmentKey(cell);
        if (!segmentSprites.ContainsKey(key)) segmentSprites[key] = new HashSet<Sprite>();
        segmentSprites[key].Add(sprite);
    }

    bool InSafeZone(Vector2Int cell)
    {
        return Mathf.Abs(cell.x) <= SAFE_RADIUS && Mathf.Abs(cell.y) <= SAFE_RADIUS;
    }

    bool InEdgeZone(Vector2Int cell)
    {
        int halfW = mapWidth  / 2;
        int halfH = mapHeight / 2;
        return cell.x < -halfW + EDGE_MARGIN || cell.x >= halfW - EDGE_MARGIN ||
               cell.y < -halfH + EDGE_MARGIN || cell.y >= halfH - EDGE_MARGIN;
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
        old = GameObject.Find("Creatures");   if (old) Destroy(old);
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
