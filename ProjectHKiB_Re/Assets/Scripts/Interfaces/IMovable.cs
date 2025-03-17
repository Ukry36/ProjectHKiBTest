using UnityEngine;

public interface IMovable
{
    public CustomVariable<float> Speed { get; set; }
    public CustomVariable<float> SprintCoeff { get; set; }
    public LayerMask WallLayer { get; set; }
}