using UnityEngine;

public class ChainEventModule : InterfaceModule, IChainEventable
{
    [field: SerializeField] public GameStateEvent ChainEvent { get; set; }

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IChainEventable>(this);
    }

    public void Initialize()
    {

    }
}