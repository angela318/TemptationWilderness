using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WildernessGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;

    [Header("Ground Tiles")]
    public TileBase[] sandTiles;   // drag sand1–5 here in Inspector

    [Header("Map Settings")]
    public int mapWidth  = 40;
    public int mapHeight = 40;

    // Sand tile weights — higher = appears more often.
    // Index matches sandTiles array. Edit in Inspector.
    [Header("Tile Weights")]
    public float[] sandWeights = { 40f, 20f, 15f, 15f, 10f };

    void Start()
    {
        Generate();
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
                TileBase tile = PickWeighted(sandTiles, sandWeights);
                groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    // Returns a random tile using weighted probability.
    TileBase PickWeighted(TileBase[] tiles, float[] weights)
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
