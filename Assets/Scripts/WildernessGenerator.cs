using UnityEngine;
using UnityEngine.Tilemaps;

public class WildernessGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;

    [Header("Ground Tiles")]
    public Sprite[] sandSprites;   // drag sand1–5 PNGs here in Inspector

    [Header("Map Settings")]
    public int mapWidth  = 40;
    public int mapHeight = 40;

    [Header("Tile Weights")]
    public float[] sandWeights = { 40f, 20f, 15f, 15f, 10f };

    // Tile assets created at runtime from the sprites
    Tile[] sandTiles;

    void Start()
    {
        BuildTiles();
        Generate();
    }

    // Converts Sprite array to Tile assets at runtime
    void BuildTiles()
    {
        sandTiles = new Tile[sandSprites.Length];
        for (int i = 0; i < sandSprites.Length; i++)
        {
            Tile t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sandSprites[i];
            sandTiles[i] = t;
        }
    }

    public void Generate()
    {
        groundTilemap.ClearAllTiles();
        FillGround();
    }

    void FillGround()
    {
        int halfW = mapWidth  / 2;
        int halfH = mapHeight / 2;

        for (int x = -halfW; x < halfW; x++)
        {
            for (int y = -halfH; y < halfH; y++)
            {
                Tile tile = PickWeighted(sandTiles, sandWeights);
                groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
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
