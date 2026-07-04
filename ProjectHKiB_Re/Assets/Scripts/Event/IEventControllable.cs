public interface IEventControllable
{
    public StateController Entity { get; }
    public IAnimatable Animatable { get; }
    public IDirAnimatable DirAnimatable { get; }
}