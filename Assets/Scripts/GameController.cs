using UnityEngine;
using System.Runtime.InteropServices;

public class GameController : MonoBehaviour
{
    public static GameController instance;

    [Header("Scroll Settings")]
    public int totalScrolls = 5;

    int scrollsCollected = 0;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void SceneLoaded();
    [DllImport("__Internal")] static extern void ScrollCollected(int count, int total);
    [DllImport("__Internal")] static extern void AllScrollsCollected();
#endif

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SceneLoaded();
        ScrollCollected(0, totalScrolls);
#endif
    }

    public void OnScrollCollected()
    {
        scrollsCollected++;
        Debug.Log($"Scrolls collected: {scrollsCollected} / {totalScrolls}");

#if UNITY_WEBGL && !UNITY_EDITOR
        ScrollCollected(scrollsCollected, totalScrolls);
#endif

        if (scrollsCollected >= totalScrolls)
            OnAllScrollsCollected();
    }

    void OnAllScrollsCollected()
    {
        Debug.Log("All scrolls collected!");
        PauseGame();

#if UNITY_WEBGL && !UNITY_EDITOR
        AllScrollsCollected();
#endif
    }

    public void PauseGame()  { Time.timeScale = 0; }
    public void ResumeGame() { Time.timeScale = 1; }
}
