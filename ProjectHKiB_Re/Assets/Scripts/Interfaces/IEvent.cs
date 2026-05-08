using System.Collections.Generic;
using UnityEngine;

public interface IEvent : IInitializable
{
    public Collider2D[] CurrentTargets { get; set; }
    public int TargetCount { get; set; } 
    public void RegisterTarget(Collider2D[] targets, int cnt);
    public void TriggerEvent();
    public void EndEvent(Collider2D target);
}