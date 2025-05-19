using System;
using System.Collections.Generic;
using UnityEngine;

public interface IMovable
{
    public struct ExternalForce
    {
        public readonly Vector3 GetForce
        {
            get
            {
                Vector3 result = new();
                if (SetForce?.Count > 0)
                {
                    foreach (var key in SetForce.Keys)
                        result += SetForce[key];
                }
                return result;
            }
        }
        public Dictionary<int, Vector3> SetForce { get; set; }

        public ExternalForce(bool use)
        {
            SetForce = new();
        }
    }
    public StatContainer Speed { get; set; }
    public StatContainer SprintCoeff { get; set; }
    public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public bool IsSprinting { get; set; }
    public AudioDataSO FootStepAudio { get; set; }
    public MovePoint MovePoint { get; set; }
    public FootstepController FootstepController { get; set; }
    public ExternalForce ExForce { get; set; }
    public bool IsKnockbackMove { get; set; }

    public void KnockBack(Vector3 dir, float strength);
    public void EndKnockbackEarly();
}