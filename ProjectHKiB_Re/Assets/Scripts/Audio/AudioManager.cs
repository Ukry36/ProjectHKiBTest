using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : PoolManager<AudioPlayer>
{
    [SerializeField] private AudioDataSO[] allDatas;
    [SerializeField] private AudioTypeSO[] allTypes;
    public AudioMixer audioMixer;

    private Queue<Tuple<AudioDataSO, float, Vector3>> _oneShotPlayQueue;
    private AudioPlayer _oneShotPlayer;

    private bool _oneShotPlayerDequeueInProgress;


    public override void Initialize()
    {
        _oneShotPlayQueue = new();
        base.Initialize();
    }

    public override void CreatePool()
    {
        base.CreatePool();

        var clone = Instantiate(prefab, this.transform);
        if (clone.TryGetComponent(out AudioPlayer oneShotAudioPlayer))
            _oneShotPlayer = oneShotAudioPlayer;
        else
            Debug.LogError("ERROR: Failed to create oneShotPlayer(audioPlayer prefab is invalid)!!!");

        for (int i = 0; i < allDatas.Length; i++)
        {
            if (allDatas[i].type.playOneShot) continue;
            for (int j = 0; j < allDatas[i].type.PoolSize; j++)
            {
                clone = Instantiate(prefab, this.transform);
                if (clone.TryGetComponent(out AudioPlayer audioPlayer))
                {
                    AddPool(allDatas[i].ID, audioPlayer);
                    audioPlayer.InitializeWhenReused(allDatas[i]);
                    audioPlayer.OnGameObjectDisabled += OnGameObjectDisabled;
                }
                else
                {
                    Debug.LogError("ERROR: Failed to create pool(audioPlayer prefab is invalid)!!!");
                }
            }
        }
    }

    public override void SetObjectOnReuse(AudioPlayer audioPlayer, Transform transform, Quaternion rotation, bool attatchToTransform)
    {
        audioPlayer.gameObject.SetActive(true);
        audioPlayer.transform.SetPositionAndRotation(transform.position, rotation);
        if (attatchToTransform) audioPlayer.transform.parent = transform;
    }

    public AudioPlayer PlayAudio(int ID, float volume, float fadeTimeSec, Transform transform, bool attatchToTransform)
    {
        AudioPlayer clone = ReuseObject(ID, transform, quaternion.identity, attatchToTransform);
        if (!clone)
        {
            Debug.LogError("ERROR: Failed to play audio(audioPlayer prefab is invalid)!!! ID: " + ID);
            return null;
        }
        clone.Play(volume, fadeTimeSec);
        return clone;
    }

    public void PlayAudioOneShot(AudioDataSO audioData, float volume, Vector3 pos)
    {
        _oneShotPlayQueue.Enqueue(Tuple.Create(audioData, volume, pos));
        if (!_oneShotPlayerDequeueInProgress)
        {
            _oneShotPlayerDequeueInProgress = true;
            StartCoroutine(OneShotPlayDequeueCoroutine());
        }
    }

    private void PlayAudioOneShotDequeue(AudioDataSO audioData, float volume, Vector3 pos)
    {
        _oneShotPlayer.InitializeWhenReused(audioData);
        _oneShotPlayer.transform.position = pos;
        _oneShotPlayer.Play(volume, 0);
    }

    private IEnumerator OneShotPlayDequeueCoroutine()
    {
        while (_oneShotPlayQueue.Count > 0)
        {
            Tuple<AudioDataSO, float, Vector3> tuple = _oneShotPlayQueue.Dequeue();
            PlayAudioOneShotDequeue(tuple.Item1, tuple.Item2, tuple.Item3);
            yield return null;
        }
        _oneShotPlayerDequeueInProgress = false;
    }

    public void ChangeVolume(AudioPlayer audioPlayer, float volume, float fadeTimeSec)
    => audioPlayer.Fade(volume, fadeTimeSec);

    public void StopPlaying(AudioPlayer audioPlayer, float fadeTimeSec)
    => ChangeVolume(audioPlayer, 0, fadeTimeSec);

    public override void ResetPool()
    {
        int[] keys = objectPool.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
            Destroy(objectPool[keys[i]].gameObject);
        base.ResetPool();
        Destroy(_oneShotPlayer.gameObject);
        _oneShotPlayer = null;
    }
}