using UnityEngine;
using UnityEditor.Animations;
using System;

public interface IDirAnimatable : IAnimatable
{
    public EnumManager.AnimDir AnimationDirectionInternal { get; set; }
    public EnumManager.AnimDir AnimationDirection
    {
        get => AnimationDirectionInternal;
        private set
        {
            if (AnimationDirectionInternal != value)
            {
                AnimationDirectionInternal = value;
                OnDirChanged?.Invoke(value);
            }
        }
    }
    public Vector2 LastSetAnimationDir8 { get; set; }
    public Vector2 LastSetAnimationDir4 { get; set; }
    public Quaternion LastSetAnimationQuaternion4
    {
        get
        {
            if (LastSetAnimationDir8 == Vector2.zero) return Quaternion.identity;
            return AnimationDirection switch
            {
                EnumManager.AnimDir.D => Quaternion.identity,
                EnumManager.AnimDir.L => Quaternion.Euler(0, 0, -90),
                EnumManager.AnimDir.R => Quaternion.Euler(0, 0, 90),
                EnumManager.AnimDir.U => Quaternion.Euler(0, 0, 180),
                _ => Quaternion.identity,
            };
        }
    }
    public float LastSetAnimationAngle4
    {
        get
        {
            if (LastSetAnimationDir8 == Vector2.zero) return 0;
            return AnimationDirection switch
            {
                EnumManager.AnimDir.D => 0,
                EnumManager.AnimDir.L => -90,
                EnumManager.AnimDir.R => 90,
                EnumManager.AnimDir.U => 180,
                _ => 0,
            };
        }
    }
    public Action<EnumManager.AnimDir> OnDirChanged { get; set; }
    public Vector2 GetAnimationRestrictedDirection(Vector3 input);
    public bool CheckIfLastSetDirectionSame(Vector2 input);
    public void SetAnimationDirection(Vector2 vectorDir);
    public void SetAnimationDirection(EnumManager.AnimDir animDir);
}