using UnityEngine;
using static Unity.VisualScripting.Member;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source")]
    [SerializeField] AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] AudioClip buttonClick;
    [SerializeField] AudioClip cubeCollected;
    [SerializeField] AudioClip gameWin;
    [SerializeField] AudioClip gameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void PlayButtonClick()
    {
        Play(buttonClick);
    }

    public void PlayCubeCollected()
    {
        Play(cubeCollected);
    }

    public void PlayGameWin()
    {
        Play(gameWin);
    }

    public void PlayGameOver()
    {
        Play(gameOver);
    }

    private void Play(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
