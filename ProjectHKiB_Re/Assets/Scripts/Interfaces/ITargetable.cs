using UnityEngine;

public interface ITargetable : ITargetableBase
{
    public Transform CurrentTarget { get; set; }
    public void Initialize();
}