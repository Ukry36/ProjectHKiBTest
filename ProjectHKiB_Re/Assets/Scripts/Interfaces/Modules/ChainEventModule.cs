using UnityEditor.Animations;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ChainEventModule : InterfaceModule, IChainEventable
{
    [field: SerializeField] public GameEvent ChainEvent { get; set; }

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IChainEventable>(this);
    }
}