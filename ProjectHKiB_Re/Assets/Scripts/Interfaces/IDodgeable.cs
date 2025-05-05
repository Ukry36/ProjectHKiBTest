public interface IDodgeable
{
    public StatContainer DodgeCooltime { get; set; }
    public StatContainer ContinuousDodgeLimit { get; set; }
    public StatContainer KeepDodgeMaxTime { get; set; }
    public StatContainer KeepDodgeMaxDistance { get; set; }
}