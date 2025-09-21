using System;
using System.Collections.Generic;
using UnityEngine;

public interface IMovableBase
{
    public float Speed { get; set; }
    public float SprintCoeff { get; set; }
    public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public float Mass { get; set; }
}