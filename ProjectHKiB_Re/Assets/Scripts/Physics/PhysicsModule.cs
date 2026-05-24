using UnityEngine;

public class PhysicsModule : InterfaceModule, IPhysics
{
    public PhysicsManager physManager;
    
    public float FrictionCoeff { get; set; }         = 0.85f;
    public float BounceCoeff { get; set; }           = 0.3f;
    public float AirFriction { get; set; }           = 0.98f;
    public float FrictionWalkInfluence { get; set; } = 1;

    public MovementMode Mode { get; set; }
    public GridState    Grid { get; set; }
    public PhysicsState Phys { get; set; }
    public Vector2Int Size { get; set; }

    public float GridEndureSpeed { get; set; }   = 10;
    public float GridEndureForce { get; set; }   = 50;
    public float StepUpTolerance { get; set; }   = 0.5f;
    public float StepDownTolerance { get; set; } = 0.2f;
 
    public LayerMask FloorLayer { get; set; }
 
    public float ZPosition { get => transform.position.z; set => SetZLevel(value); }
    public Vector2 HPosition { get => transform.position; set => transform.position = new Vector3(value.x, value.y, transform.position.z) ; }
    public float ZVelocity { get; set; }
    public Vector2 HVelocity { get; set; }
    public ZCollider2D Ground { get; set; }
    public int CanWalkFrameLeft { get; set; }
    public bool IsGroundedPrev { get; set; }
    public bool IsOnSlope { get; set; }

    [field: NaughtyAttributes.ReadOnly][field: SerializeField] public Vector3 ExForce { get; set; }
    [field: SerializeField] public float Mass { get; set; }
    public float InvM { get; set; }
    public MovePoint MovePoint { get; set; }
    public Vector3 LastSetDir { get; set; }
    public bool IsSprinting { get; set; }
    public bool IsWalking { get; set; }

    public Vector2 WalkingDir { get; set; }
    public float SprintCoeff { get; set; }
    [field: SerializeField]public float MaxWalkSpeed { get; set; }
    [field: SerializeField]public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public AudioDataSO FootStepAudio { get; set; }
    public bool IsKnockbackMove { get; set; }
    [field: SerializeField] public BodyComponent[] BodyComponents { get; set; }
    public float WalkAcceleration { get; set; } = 300f;

 
    public Vector3 PrevEntityPos { get; set; }

    [field: SerializeField] public ZCollider2D ZCol { get; set; }
    public int ID { get; set; }

    [NaughtyAttributes.Button]
    public void Jump()
    {
        ExForce += 100 * jump * Vector3.forward;
    }
    public float jump;

    public void KnockBack(Vector3 dir, float strength) => ExForce += dir * strength;
    public void EndKnockbackEarly(){}
    public void KnockBackEndCallback(){}

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IPhysics>(this);
    }

    public void Initialize()
    {
        if (!ZCol && TryGetComponent(out ZCollider2D z))
        {
            ZCol = z;
        }
        else
        {
            return;
        }
        if (!physManager) physManager = FindObjectOfType<PhysicsManager>();
        if (MovePoint) MovePoint.Initialize(this);
        ExForce = new();
        Grid = new();
        Phys = new();
        PrevEntityPos = transform.position;
        if (Size.x <= 0 || Size.y <= 0) Size = Vector2Int.one;
        InvM = 1f / Mass;
        ZCol.frictionCoeff = FrictionCoeff;
        ZCol.bounceCoeff   = BounceCoeff;
        ID = GetInstanceID();
        GridEndureSpeed = GridEndureSpeed < 0 ? MaxWalkSpeed * 2 : GridEndureSpeed;


        physManager.AddPhysicsObject(this);
    }

    public void SetZLevel(float z)
    {
        float d = z - transform.position.z;
        for(int i = 0; i < BodyComponents.Length; i++) BodyComponents[i].SetZ(z);
        if (MovePoint)MovePoint.transform.position += Vector3.forward * d;
        transform.position += Vector3.forward * d;
    }

    public void SetBodyPartSnapOffset(Vector2 nextWorldPos)
    {
        for(int i = 0; i < BodyComponents.Length; i++) BodyComponents[i].SetSnapOffset(nextWorldPos);
    }
    public void DecayBodyPartOffset(float renderDecaySpeed, float snapDecaySpeed)
    {
        for(int i = 0; i < BodyComponents.Length; i++) BodyComponents[i].DecayOffsets(renderDecaySpeed, snapDecaySpeed);
    }
    public void SnapBodyPart()
    {
        for(int i = 0; i < BodyComponents.Length; i++) BodyComponents[i].Snap();
    }

    public void LogicalTeleport(Vector3 position) => physManager.LogicalTeleport(this, position);
    public void RealTeleport(Vector3 position) => physManager.RealTeleport(this, position);
}