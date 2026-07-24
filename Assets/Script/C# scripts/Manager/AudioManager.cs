using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip background;
    public AudioClip eating;
    public AudioClip gameOver;
    public AudioClip ghost_mode;
    public AudioClip dashing;
    public AudioClip updateSkill;

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        musicSource.clip = background;
        musicSource.Play();
    }

    public void playSFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }
}
