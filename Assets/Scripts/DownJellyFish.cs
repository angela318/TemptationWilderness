using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownJellyFish : MonoBehaviour
{
    private SpriteRenderer myRenderer;
    // Start is called before the first frame update
    //private float startY = -2.5f; //2.8f
    //private float point1 = -1.2f; //4.1f
    private float diffThreshold = 0.5f;   
    private float startY = 6.2f;
    private float point1 = 5.5f;
    private float point2 = 3.9f;
    private float point3 = 2.7f;
    bool startAnimate1 = false; 
    bool startAnimate2 = false;
    bool startAnimate3 = false;
    
    bool done = false; 
    private bool animationStarted = false; 
    void Start()
    {
        myRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate(){ 
        if (myRenderer.isVisible){
            if(startAnimate1){ 
                StartAnimate1();
            } 
            else if(startAnimate2){ 
                 StartAnimate2();
            }
            else if(startAnimate3){ 
                 StartAnimate3();
                 done = true;
            }
            else if (!animationStarted && Time.timeScale > 0f){
                animationStarted = true;
                StartCoroutine(StartAnimation());
            }
        }
        else{
            if(done){
                done = false;
                animationStarted = false;
                transform.position = new Vector3 (transform.position.x, startY, transform.position.z);
            }
        }
    }

    IEnumerator StartAnimation(){
        AudioManager.instance?.PlayJellyfishZap();
        yield return new WaitForSeconds(1f);
        startAnimate1 = true;
        yield return new WaitForSeconds(0.1f);
        startAnimate2 = true;
        yield return new WaitForSeconds(0.05f);
        startAnimate3 = true;
    } 

    void StartAnimate1(){ 
        if(transform.position.y == point1){             
            return;
        }
        float newPositionY = Mathf.Lerp (transform.position.y, point1, Time.deltaTime* 3f);

        float difference = newPositionY - point1;

        if ( difference < diffThreshold) {
            startAnimate1 = false;
            //Debug.Log( difference + ":" +  diffThreshold + (difference < diffThreshold));
            //newPositionY = point1;
        }
        //Debug.Log("current" + transform.position.y); 
        //Debug.Log("new" + newPositionY);    
        transform.position = new Vector3 (transform.position.x, newPositionY, transform.position.z);
    }
     
    void StartAnimate2(){ 
        if(transform.position.y == point2){ 
            return;
        }
        float newPositionY = Mathf.Lerp (transform.position.y, point2, Time.deltaTime* 2.5f);

        float difference = newPositionY - point2;

        if ( difference < diffThreshold) {
            startAnimate2 = false;
            //Debug.Log( difference + ":" +  diffThreshold + (difference < diffThreshold));
            //newPositionY = point2;
        }
        //Debug.Log("current" + transform.position.y); 
        //Debug.Log("new" + newPositionY);    
        transform.position = new Vector3 (transform.position.x, newPositionY, transform.position.z);
    }

    void StartAnimate3(){ 
        
        float newPositionY = Mathf.Lerp (transform.position.y, point3, Time.deltaTime* 2.5f);
        transform.position = new Vector3 (transform.position.x, newPositionY, transform.position.z);
    }
}
