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

    public void StartCooltime(float cooltime, TweenCallback cooltimeEndCallback = null)
    {
        Time = cooltime;
        if (!IsCooltimeEnded)
            CancelCooltime();
        IsCooltimeEnded = false;
        cooltimeEndCallback += () => IsCooltimeEnded = true;
        GameManager.instance.cooltimeManager.StartCooltime(this.GetHashCode(), this, cooltimeEndCallback);
    }

    public void CancelCooltime()
    {
        GameManager.instance.cooltimeManager.CancelCooltime(this.GetHashCode());
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
    private Dictionary<int, Sequence> _cooltimes = new();
    public void StartCooltime(int ID, Cooltime cooltime, TweenCallback cooltimeEnded)
    {
        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(cooltime.Time);
        sequence.OnComplete(cooltimeEnded);
        _cooltimes[ID] = sequence;
    }

    public void CancelCooltime(int ID)
    {
        if (!_cooltimes.ContainsKey(ID)) { Debug.LogError("ERROR: Cooltime not found!!!"); return; }
        _cooltimes[ID]?.Kill();
        _cooltimes.Remove(ID);
    }
}
