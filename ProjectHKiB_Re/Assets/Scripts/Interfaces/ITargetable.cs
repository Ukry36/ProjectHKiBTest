using UnityEngine;

public interface ITargetable
{
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get => TargetController.CurrentTarget; set => TargetController.CurrentTarget = value; }
    public TargetController TargetController { get; set; }
}