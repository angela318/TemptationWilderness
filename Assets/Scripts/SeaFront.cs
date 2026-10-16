using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaFront : MonoBehaviour
{
    private Transform whale;
    private Vector3 tempPos;

    // Start is called before the first frame update
    void Start()
    {
        whale = GameObject.FindWithTag("Whale").transform;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
        spriteRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.3f);
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        tempPos = transform.position;
        tempPos.x = whale.position.x + 6;

        transform.position = tempPos;
    }
}
