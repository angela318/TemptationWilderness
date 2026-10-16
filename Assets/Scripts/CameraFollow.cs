using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform whale;
    private Vector3 tempPos;

    [SerializeField]
    private float maxX;

    // Start is called before the first frame update
    void Start()
    {
        whale = GameObject.FindWithTag("Whale").transform;
        Debug.Log("Whale" + whale.position.x);
        Debug.Log("Camera" + transform.position.x);
    }

    // Update is called once per frame
    void LateUpdate()
    {

        if (!whale)
            return;

        tempPos = transform.position;
        tempPos.x = whale.position.x + 6;

        if (tempPos.x > maxX)
            tempPos.x = maxX;

        transform.position = tempPos;

    }



} // class