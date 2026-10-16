using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class Whale : MonoBehaviour
{
    private Rigidbody2D myBody;
    private SpriteRenderer mySpriteRenderer;
    private Animator myAnim;

    [DllImport("__Internal")]
    private static extern void GameOver (int score);

    [DllImport("__Internal")]
    private static extern void BreathingStatus (bool active);

    [DllImport("__Internal")]
    private static extern void FishCollected ();
    
    private string OBSTACLE_TAG = "Obstacle";
    private string SKY_COLLIDER = "SkyCollider";
    private string BREATHING = "breathing";

    public float speed = 4f;
    public int level = 1;
    public int fishScore = 0;
    private float accelleration = 0.0001f;
    private bool gameOverFired = false;
    
    // Start is called before the first frame update
    void Start()
    {
         myBody = GetComponent<Rigidbody2D>();
         mySpriteRenderer = GetComponent<SpriteRenderer>();
         myAnim = GetComponent<Animator>();
#if UNITY_WEBGL == true && UNITY_EDITOR == false
         Time.timeScale = 0;
#endif
    }

    void FixedUpdate(){ 
        speed = speed + accelleration;
        WhaleMovement(); 
    }
    // Update is called once per frame
    void Update()
    {          
        transform.position += new Vector3(speed * Time.deltaTime, 0, 0);
    }

    void WhaleMovement() {
        if((Input.GetMouseButton(0) == true) || (Input.touchCount > 0)){ 
            WhaleGoingUp();
        }
        else{ 
            WhaleGoingDown();
        }     
    }

    void WhaleGoingUp(){       
        // Add force to go against gravity
        Quaternion target = Quaternion.Euler(0, 0, 7);
        myBody.AddForce(new Vector2(0f, 0.0008f), ForceMode2D.Impulse);

        transform.rotation = Quaternion.Slerp(transform.rotation, target,  Time.deltaTime * 1.5f);
    }

    void WhaleGoingDown(){
        Quaternion target = Quaternion.Euler(0, 0, -4);  
        myBody.AddForce(new Vector2(0,0));     

        transform.rotation = Quaternion.Slerp(transform.rotation, target,  Time.deltaTime * 1.5f);
        if(myAnim.GetBool(BREATHING) == true){ 
        myAnim.SetBool(BREATHING, false);
#if UNITY_WEBGL == true && UNITY_EDITOR == false
            BreathingStatus (false);
#endif
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(OBSTACLE_TAG)) TriggerGameOver();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(OBSTACLE_TAG))
        {
            TriggerGameOver();
            return;
        }
        if (collision.gameObject.name == SKY_COLLIDER)
        {
            myAnim.SetBool(BREATHING, true);
            AudioManager.instance?.PlayWhaleBlow();
#if UNITY_WEBGL == true && UNITY_EDITOR == false
            BreathingStatus (true);
#endif
        }
    }

    public void GameLevel(int levelFromHtml) {
        Debug.Log("level: "+ level);
        level = levelFromHtml;
        if (level == 2){ 
            Debug.Log("speed: "+ speed);
            speed = 4.5f;
        }
        else if (level == 3){ 
            Debug.Log("speed: "+ speed);
        }
   }

   void TriggerGameOver()
   {
       if (gameOverFired) return;
       gameOverFired = true;
       AudioManager.instance?.StopAll();
       AudioManager.instance?.PlayGameOver();
       Debug.Log("GameOver");
#if UNITY_WEBGL == true && UNITY_EDITOR == false
       GameOver(100);
#endif
   }

   public void CollectFish()
   {
       fishScore++;
       Debug.Log("Fish collected: " + fishScore);
#if UNITY_WEBGL == true && UNITY_EDITOR == false
       FishCollected();
#endif
   }

   public void Restart(){
       transform.position =  new Vector2(2.8f, 7.3f);
       fishScore = 0;
       gameOverFired = false;
   }
}
