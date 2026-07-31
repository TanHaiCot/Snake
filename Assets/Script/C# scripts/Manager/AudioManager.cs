using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip background;
    public AudioClip eating;
    public AudioClip aiEating;
    public AudioClip gameOver;
    public AudioClip ghost_mode;
    public AudioClip dashing;
    public AudioClip updateSkill;
    public AudioClip teleport;

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            transform.SetParent(null);
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

    public void EatingSound(bool collectedByPlayer)
    {
        AudioClip clip = collectedByPlayer
            ? eating
            : aiEating;

        sfxSource.pitch = Random.Range(0.75f, 1.25f);
        sfxSource.PlayOneShot(
            clip,
            collectedByPlayer ? 1f : 0.75f
        );
    }

}
