using UnityEditor.Animations;

public interface IChainEventable
{
    public GameStateEvent ChainEvent { get; set; }
}