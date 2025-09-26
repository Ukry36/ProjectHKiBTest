using UnityEngine;

public interface ITargetable : ITargetableBase, IInitializable
{
    public Transform CurrentTarget { get; set; }
}