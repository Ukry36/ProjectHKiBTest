using UnityEngine;

public class PhysicsObjectTest : InterfaceModule, IMovable
{
    public PhysicsManager2 physManager;
    
    [Header("physics")]
    public float frictionCoeff   = 0.85f;
    public float bounceCoeff     = 0.3f;  
    public float airFriction     = 0.98f;   
    public float stopAccelerateThreshold;
    public float frictionWalkInfluence = 1;

    public MovementMode Mode;
    public GridState    Grid   = new();
    public PhysicsState Phys   = new();

    public float ModeTransitionThreshold;
 
    public LayerMask floorLayer;
 
    public float ZPosition 
    { 
        get => transform.position.z; 
        set { SetZLevel(value);}
    }
    public float ZVelocity { get; set; }
    public bool IsGrounded { get; set; }
    public int CanWalkFrameLeft { get; set; }
    public bool IsGroundedPrev { get; set; }
    public bool IsOnSlope { get; set; }

    [field: NaughtyAttributes.ReadOnly][field: SerializeField] public Vector3 ExForce { get; set; }
    [field: SerializeField] public float Mass {get;set;}
    public MovePoint MovePoint { get; set; }
    public Vector3 LastSetDir { get; set; }
    public bool IsSprinting { get; set; }
    public bool IsWalking { get; set; }

    public Vector2 WalkingDir { get; set; }
    public float SprintCoeff { get; set; }
    [field: SerializeField]public float WalkSpeed { get; set; }
    [field: SerializeField]public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public AudioDataSO FootStepAudio { get; set; }
    public bool IsKnockbackMove { get; set; }
    [field: SerializeField] public BodyComponent[] BodyComponents { get; set; }
    public float WalkAcceleration = 30f;

 
    public Vector3 PrevEntityPos { get; set; }

    public ZCollider2D zCollider;

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
        interfaceRegistable.RegisterInterface<IMovable>(this);
    }

    public void Initialize()
    {
        if (!physManager) physManager = FindObjectOfType<PhysicsManager2>();
        if (MovePoint) MovePoint.Initialize(this);
        ExForce = new();
        PrevEntityPos = transform.position;
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
}