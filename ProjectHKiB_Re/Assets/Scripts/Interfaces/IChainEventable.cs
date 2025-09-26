using UnityEditor.Animations;

public interface IChainEventable : IInitializable
{
    public GameStateEvent ChainEvent { get; set; }
}