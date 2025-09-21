using UnityEngine;

public interface ITargetableBase
{
    public LayerMask[] TargetLayers { get; set; }
}