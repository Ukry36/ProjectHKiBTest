using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
public class Cooltime
{
    public float Time { get; private set; }
    public bool IsCooltimeEnded { get; private set; }

    private TweenCallback _cooltimeEndCallback;

    public float ElapsedTime => GameManager.instance.cooltimeManager.GetElapsedTime(GetHashCode());
    public float RemainTime => Mathf.Max(0f, Time - ElapsedTime);

    public void StartCooltime(float cooltime, TweenCallback cooltimeEndCallback = null)
    {
        Time = cooltime;

        if (!IsCooltimeEnded)
            CancelCooltime();

        IsCooltimeEnded = false;
        _cooltimeEndCallback = cooltimeEndCallback;
        _cooltimeEndCallback += () => IsCooltimeEnded = true;

        GameManager.instance.cooltimeManager.StartCooltime(GetHashCode(), this, _cooltimeEndCallback);
    }

    /// <summary>
    /// 현재까지 진행된 시간은 유지하고, "총 쿨타임"만 새 값으로 다시 계산
    /// 예) 30초 중 10초 지난 상태에서 newTotalCooltime = 33 이면 남은 시간은 23초
    /// </summary>
    public void RecalculateTotalCooltime(float newTotalCooltime)
    {
        if (IsCooltimeEnded)
        {
            Time = newTotalCooltime;
            return;
        }

        float elapsed = ElapsedTime;
        float newRemain = Mathf.Max(0f, newTotalCooltime - elapsed);

        CancelCooltime();
        Time = newTotalCooltime;

        if (newRemain <= 0f)
        {
            IsCooltimeEnded = true;
            _cooltimeEndCallback?.Invoke();
            return;
        }

        IsCooltimeEnded = false;
        GameManager.instance.cooltimeManager.StartCooltime(GetHashCode(), this, _cooltimeEndCallback, newRemain);
    }

    public void ExtendCooltime(float cooltime, TweenCallback cooltimeEndCallback = null)
    {
        float elapsedTime = ElapsedTime;
        CancelCooltime();
        StartCooltime(cooltime + elapsedTime, cooltimeEndCallback);
    }

    public void CancelCooltime()
    {
        GameManager.instance.cooltimeManager.CancelCooltime(GetHashCode());
        IsCooltimeEnded = true;
    }

    public Cooltime(Cooltime cooltime)
    {
        Time = cooltime.Time;
        IsCooltimeEnded = true;
    }

    public Cooltime(float cooltime)
    {
        Time = cooltime;
        IsCooltimeEnded = true;
    }

    public Cooltime()
    {
        Time = 0;
        IsCooltimeEnded = true;
    }
}

public class CooltimeManager : MonoBehaviour
{
    private readonly Dictionary<int, Sequence> _cooltimes = new();

    public void StartCooltime(int ID, Cooltime cooltime, TweenCallback cooltimeEnded)
    {
        StartCooltime(ID, cooltime, cooltimeEnded, cooltime.Time);
    }

    public void StartCooltime(int ID, Cooltime cooltime, TweenCallback cooltimeEnded, float duration)
    {
        CancelCooltime(ID);

        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(duration);
        sequence.OnComplete(cooltimeEnded);
        _cooltimes[ID] = sequence;
    }

    public float GetElapsedTime(int ID)
        => _cooltimes.ContainsKey(ID) ? _cooltimes[ID].ElapsedDelay() : 0f;

    public void CancelCooltime(int ID)
    {
        if (!_cooltimes.ContainsKey(ID)) return;
        _cooltimes[ID]?.Kill();
        _cooltimes.Remove(ID);
    }
}