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
    public float Speed { get; set; }
    public float SprintCoeff { get; set; }
    public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public AudioDataSO FootStepAudio { get; set; }
    public float Mass { get; set; }
    public MovePoint MovePoint { get => MovementController.MovePoint; }
    public FootstepController FootstepController { get; set; }
    public MovementController MovementController { get; set; }
    public Vector3 LastSetDir { get => MovementController.LastSetDir; set => MovementController.LastSetDir = value; }
    public bool IsKnockbackMove { get => MovementController.IsKnockbackMove; set => MovementController.IsKnockbackMove = value; }
    public bool IsSprinting { get => MovementController.IsSprinting; set => MovementController.IsSprinting = value; }
    public ExternalForce ExForce { get => MovementController.ExForce; set => MovementController.ExForce = value; }
    public void KnockBack(Vector3 dir, float strength) => MovementController.KnockBack(dir, strength, this);
    public void EndKnockbackEarly() => MovementController.EndKnockbackEarly(this);
}