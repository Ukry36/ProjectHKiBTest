public interface IDodgeable
{
    public CustomVariable<float> DodgeCooltime { get; set; }
    public CustomVariable<float> ContinuousDodgeLimit { get; set; }
    public CustomVariable<float> KeepDodgeMaxTime { get; set; }
    public CustomVariable<float> KeepDodgeMaxDistance { get; set; }
}