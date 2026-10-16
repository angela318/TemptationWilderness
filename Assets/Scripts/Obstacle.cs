using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public int obstacleIndex;
    public string type; 

    public float midPoint; 
    private Transform obstacle;
    private SpriteRenderer myRenderer;
    bool shown = false; 
    public float[] staticMinDistance;
    float[][] minDistanceConstant = { 
        //          1.5         3.7,     1.6,     2,4,     3.55,      0.6,        0.6 // midWidth
        // current: stone,      ship,    box,     wreck,   island,     downjelly,  upjelly 
        new float[] { 3.0f,     2.4f,    3.1f,    3.9f,    2.8f    }, // previous: stone
        new float[] { -2.4f,    7.8f,    -2.3f,   -1.3f,   8.5f     }, // previous: ship
        new float[] { 3.1f,     2.3f,    3.2f,    4.2f,    2.7f    }, // previous: box
        new float[] { 4.1f,     1.3f,    4.2f,    4.8f,    9.2f     }, // previous: wreck
        new float[] { -2.4f,    8.2f,    8.9f,    11f,     8.5f     },  // previous: island
        new float[] { 3.7f,     7.4f,    0.0f   }, // previous: downjelly
        new float[] { 5f,       6f,      0.0f   }, // previous: upjelly
        };

    string[] types = { "bottom", "top",     "bottom",   "bottom",   "top"};
                    // stone,     ship,    box,         wreck,      island


    // Start is called before the first frame update
    void Start()
    {
        obstacle = GetComponent<Transform>();
        myRenderer = GetComponent<SpriteRenderer>();
        
    }

    public float minDistance(int previousIndex, int currentIndex){ 
        return minDistanceConstant[previousIndex][currentIndex];
    }

    public float positionX(){ 
        return obstacle.transform.position.x;
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate(){ 
    }

}
