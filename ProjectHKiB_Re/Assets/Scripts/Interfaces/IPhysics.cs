using UnityEngine;
public interface IPhysicsBase
{
    public Vector2Int Size { get; set; }
    public float Mass { get; set; }

    public float WalkAcceleration { get; set; }
    public float MaxWalkSpeed { get; set; }
    public float SprintCoeff { get; set; }
    public float FrictionWalkInfluence { get; set; }

    public float FrictionCoeff { get; set; }
    public float AirFriction { get; set; }
    public float BounceCoeff { get; set; }
    
    public float GridEndureSpeed { get; set; }
    public float GridEndureForce { get; set; }

    public float StepUpTolerance { get; set; }
    public float StepDownTolerance { get; set; }

    public LayerMask WallLayer { get; set; }
    public LayerMask FloorLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
}

public interface IPhysics : IPhysicsBase, IInitializable
{
    public Vector3 LastSetDir { get; set; }
    public bool IsKnockbackMove { get; set; }
    public bool IsWalking { get; set; }
    public Vector2 WalkingDir { get; set; }
    public bool IsSprinting { get; set; }
    public Vector3 ExForce { get; set; }
    public BodyComponent[] BodyComponents { get; set; }
    public float ZPosition { get; set; }
    public Vector2 HPosition { get; set; }
    public float ZVelocity { get; set; }
    public Vector2 HVelocity { get; set; }
    public GridState    Grid { get; set; }
    public PhysicsState Phys { get; set; }
    public MovementMode Mode { get; set; }
    public ZCollider2D Ground { get; set; }
    public int CanWalkFrameLeft { get; set; }
    public bool IsGroundedPrev { get; set; }
    public bool IsOnSlope { get; set; }
    public AudioDataSO FootStepAudio { get; set; }
    public float InvM { get; set; }
    public Vector3 PrevEntityPos { get; set; }
    public ZCollider2D ZCol { get; set; }
    public int ID {get; set;}
    public void KnockBack(Vector3 dir, float strength);
    public void EndKnockbackEarly();
    public void KnockBackEndCallback();

    public Vector3 Position {get => new(HPosition.x, HPosition.y, ZPosition); }
    public Vector3 Velocity { get => new(HVelocity.x, HVelocity.y, ZVelocity); }

    public void SetZLevel(float z);
    public void SetBodyPartSnapOffset(Vector2 nextWorldPos);
    public void DecayBodyPartOffset(float renderDecaySpeed, float snapDecaySpeed);
    public void SnapBodyPart();

    public void LogicalTeleport(Vector3 position);
    public void RealTeleport(Vector3 position);
}