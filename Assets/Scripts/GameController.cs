using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;

    [Header("Scroll Settings")]
    public int totalScrolls = 5;

    int scrollsCollected = 0;

    void Awake()
    {
        instance = this;
    }

    public void OnScrollCollected()
    {
        scrollsCollected++;
        Debug.Log($"Scrolls collected: {scrollsCollected} / {totalScrolls}");

        if (scrollsCollected >= totalScrolls)
        {
            OnAllScrollsCollected();
        }
    }

    void OnAllScrollsCollected()
    {
        Debug.Log("All scrolls collected — Satan enters!");
        // Satan phase trigger goes here
    }

    public void PauseGame()  { Time.timeScale = 0; }
    public void ResumeGame() { Time.timeScale = 1; }
}
