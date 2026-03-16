public interface IFootstepBase
{
    public AudioDataSO DefaultFootstepAudio { get; set; }
}

public interface IFootstep : IFootstepBase, IInitializable
{
    public void PlayFootstepAudio(EnumManager.AnimDir animDir);
}