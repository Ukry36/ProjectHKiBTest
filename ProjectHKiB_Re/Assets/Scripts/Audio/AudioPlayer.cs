using System.Collections;
using UnityEngine;
using DG.Tweening;

public class AudioPlayer : MonoBehaviour, IPoolable
{
    public delegate void GameObjectDisabled(int ID, int hash);
    public event GameObjectDisabled OnGameObjectDisabled;

    private AudioSource _audioSource;
    [SerializeField] private AudioDataSO _audioData;
    [HideInInspector] public bool isFading;
    private float resetCoolTime;
    public int PoolSize { get; set; }

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
        isFading = false;
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
        if (isFading)
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
        isFading = true;
        Tween audio = _audioSource.DOFade(volume, timeSec);
        yield return audio.WaitForCompletion();
        isFading = false;
        if (_audioSource.volume <= 0.01f)
        {
            //make cooldown manager!!!
        }
    }

    public void OnDisable()
    {
        OnGameObjectDisabled?.Invoke(_audioData.GetInstanceID(), this.gameObject.GetHashCode());
    }
}
