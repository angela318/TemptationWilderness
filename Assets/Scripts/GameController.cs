
// Unity Medium CummunicateTest.cs file , Generally placed on Script Under the folder 
// Bound to a file named ConnectObject Of Panel On the object of the game 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
   public int gameLevel = 1;
   public void PauseGame() { // The method must be public 
        Time.timeScale = 0;
   }
   public void ResumeGame() { // The method must be public 
        Time.timeScale = 1;
   }

}