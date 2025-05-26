using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
public class Cooltime
{
    public float cooltime;
    [NaughtyAttributes.ReadOnly] public bool cooltimeEnded = false;

    public void StartCooltime()
    {
        GameManager.instance.cooltimeManager.StartCooltime(this.GetHashCode(), this);
    }

    public void CancelCooltime()
    {
        GameManager.instance.cooltimeManager.CancelCooltime(this.GetHashCode());
    }

    public Cooltime(Cooltime cooltime)
    {
        this.cooltime = cooltime.cooltime;
    }

    public Cooltime(float cooltime)
    {
        this.cooltime = cooltime;
    }
}

public class CooltimeManager : MonoBehaviour
{
    private Dictionary<int, Sequence> _cooltimes;
    public void StartCooltime(int ID, Cooltime cooltime)
    {
        cooltime.cooltimeEnded = false;
        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(cooltime.cooltime);
        sequence.OnComplete(() => cooltime.cooltimeEnded = true);
        _cooltimes[ID] = sequence;
    }

    public void CancelCooltime(int ID)
    {
        _cooltimes[ID]?.Complete();
        _cooltimes.Remove(ID);
    }
}
