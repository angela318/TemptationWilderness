using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject[] obstacleReference;
    // stone(0), ship(1), box(2), wreck(3), island(4), downJellyFish(5), upJellyFish(6), shipChainDown(7), shipChainUp(8)

    [SerializeField]
    private GameObject fishPrefab;    // drag Fish prefab here in the Inspector

    private const int DOWNJELLYFISH_REF = 5;
    private const int UPJELLYFISH_REF = 6;

    private const float SAME_LANE_MIN = 8f;
    private const float CROSS_LANE_MIN = 12f;
    private const float JELLY_CLEAR = 15f;
    private const float END_SCENE = 1000f;

    void Start()
    {
        float currentX = 20f;
        string lastLane = "bottom";
        int skipJellyCount = 0;
        int lastIndex = 0;  // stone is first

        Spawn(0, currentX);  // first obstacle: stone

        while (currentX < END_SCENE)
        {
            // Jellyfish: standalone segment, nothing else spawns this iteration
            if (currentX > 200 && skipJellyCount == 0 && Random.Range(0, 4) == 0)
            {
                float jellyX = currentX + JELLY_CLEAR;
                int jellyRef = (lastLane == "bottom") ? DOWNJELLYFISH_REF : UPJELLYFISH_REF;
                Spawn(jellyRef, jellyX);
                currentX = jellyX + JELLY_CLEAR;
                skipJellyCount = Random.Range(3, 6);
                continue;
            }

            int nextIndex = GetRandomIndex(lastLane, currentX, lastIndex);
            string nextLane = (nextIndex == 1 || nextIndex == 4 || nextIndex == 7) ? "top" : "bottom";
            // nextIndex 8 (shipChainUp) is a bottom obstacle

            float minDist = (lastLane == nextLane) ? SAME_LANE_MIN : CROSS_LANE_MIN;
            float nextX = currentX + minDist + Random.Range(0f, MaxExtra(currentX));

            Spawn(nextIndex, nextX);
            currentX = nextX;
            lastLane = nextLane;
            lastIndex = nextIndex;

            if (skipJellyCount > 0) skipJellyCount--;
        }

        SpawnFish();
    }

    // 70% chance to alternate lanes so top/bottom obstacles naturally interleave.
    // Island is held back until X > 400 since it is larger and harder than ship.
    // lastIndex is excluded from the pool so the same obstacle never repeats back-to-back.
    int GetRandomIndex(string lastLane, float currentX, int lastIndex)
    {
        bool goTop = (lastLane == "bottom") ? Random.Range(0f, 1f) < 0.75f
                                            : Random.Range(0f, 1f) < 0.3f;

        int[] topPool    = (currentX < 150f) ? new int[] { 1 }
                         : (currentX < 250f) ? new int[] { 1, 4 }
                         :                     new int[] { 1, 4, 7 };  // shipChainDown joins late
        int[] bottomPool = (currentX < 300f) ? new int[] { 0, 2, 3 }
                         :                     new int[] { 0, 2, 3, 8 }; // shipChainUp joins late

        int[] pool = goTop ? topPool : bottomPool;

        // Remove lastIndex from pool to prevent consecutive identical obstacles
        int[] filtered = System.Array.FindAll(pool, i => i != lastIndex);
        if (filtered.Length > 0) pool = filtered;

        return pool[Random.Range(0, pool.Length)];
    }

    float MaxExtra(float x)
    {
        if (x < 300) return 5f;
        if (x < 500) return 4f;
        return 3f;
    }

    void Spawn(int index, float x)
    {
        GameObject obj = Instantiate(obstacleReference[index]);
        obj.transform.position = new Vector2(x, obj.transform.position.y);
    }

    void SpawnFish()
    {
        if (fishPrefab == null)
        {
            Debug.LogWarning("ObstacleSpawner: Fish Prefab not assigned — fish will not spawn.");
            return;
        }

        float x = 130f;
        while (x < END_SCENE - 20f)
        {
            float y = Random.Range(5.5f, 8.5f);
            GameObject fish = Instantiate(fishPrefab);
            fish.transform.position = new Vector2(x, y);
            x += Random.Range(50f, 100f);
        }
    }

    void Update() { }
}
