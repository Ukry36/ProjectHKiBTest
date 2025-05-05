using System.Collections;
using UnityEngine;
using DG.Tweening;

public class AudioPlayer : MonoBehaviour
{
    private AudioSource _audioSource;
    [SerializeField] private AudioDataSO _audioData;
    public bool IsFading { get; set; }
    private float resetCoolTime;

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (TryGetComponent(out AudioSource audioSource))
            _audioSource = audioSource;
        else
            Debug.LogError("ERROR: AudioPlayer is incompleted!!");

        if (_audioData)
            InitializeWhenReused(_audioData);
    }

    //Will probably be played only in pooling sequence
    public void InitializeWhenReused(AudioDataSO audioData)
    {
        _audioData = audioData;
        IsFading = false;
        _audioSource.volume = 0;
    }

    public void Play(float volume, float fadeTimeSec)
    {
        if (volume <= 0) return;

        if (!_audioData)
        {
            Debug.LogError("ERROR: AudioPlayer not initialized!!!");
            return;
        }

        if (fadeTimeSec > 0)
            Fade(volume, fadeTimeSec);
        else
            _audioSource.volume = volume;

        if (_audioData.type.playOneShot)
        {
            _audioSource.PlayOneShot(_audioData.audioClips[Random.Range(0, _audioData.audioClips.Length)], volume);
        }
        else
        {
            _audioSource.loop = _audioData.type.loop;
            _audioSource.clip = _audioData.audioClips[Random.Range(0, _audioData.audioClips.Length)];
            if (fadeTimeSec <= 0) _audioSource.volume = volume;
            _audioSource.Play();
        }
    }

    public void Fade(float volume, float timeSec)
    {
        if (IsFading)
        {
            Debug.LogWarning("WARNING: Audio is already fading!!");
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(FadeCoroutine(timeSec, volume));
        }
    }

    public IEnumerator FadeCoroutine(float timeSec, float volume)
    {
        IsFading = true;
        Tween audio = _audioSource.DOFade(volume, timeSec);
        yield return audio.WaitForCompletion();
        IsFading = false;
        if (_audioSource.volume <= 0.01f)
        {
            //make cooldown manager!!!
        }
    }
}
