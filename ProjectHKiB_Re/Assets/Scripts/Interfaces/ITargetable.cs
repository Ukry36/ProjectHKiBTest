using UnityEngine;

public interface ITargetable
{
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
}