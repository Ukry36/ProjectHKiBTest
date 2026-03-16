using UnityEngine;
public interface ITargetableBase
{
    public LayerMask[] TargetLayers { get; set; }
}

public interface ITargetable : ITargetableBase, IInitializable
{
    public Transform CurrentTarget { get; set; }
}