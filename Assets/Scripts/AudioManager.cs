using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private AudioClip bonusPop;
    [SerializeField] private AudioClip gameOver;
    [SerializeField] private AudioClip jellyfishZap;
    [SerializeField] private AudioClip whaleBlow;
    [SerializeField] private AudioClip airWarning;

    private AudioSource audioSource;
    private AudioSource airWarningSource;

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        audioSource = GetComponent<AudioSource>();
        airWarningSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayBonusPop()     => audioSource.PlayOneShot(bonusPop);
    public void PlayGameOver()     => audioSource.PlayOneShot(gameOver);
    public void PlayJellyfishZap() => audioSource.PlayOneShot(jellyfishZap);
    public void PlayWhaleBlow()    => audioSource.PlayOneShot(whaleBlow);

    public void PlayAirWarning()
    {
        airWarningSource.Stop();
        airWarningSource.clip = airWarning;
        airWarningSource.Play();
    }

    public void StopAirWarning() => airWarningSource.Stop();

    public void StopAll()
    {
        AudioSource[] all = FindObjectsOfType<AudioSource>();
        foreach (var src in all) src.Stop();
    }

    public void AudioToggle(string value)
    {
        AudioListener.volume = value == "true" ? 1f : 0f;
    }
}
