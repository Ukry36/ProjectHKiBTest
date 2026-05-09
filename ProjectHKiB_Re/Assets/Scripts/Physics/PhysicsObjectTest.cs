using UnityEngine;

public class PhysicsObjectTest : InterfaceModule, IMovable
{
    public Transform       entityTransform;

    public PhysicsManager physManager;
    
    [Header("physics")]
    
    public float Height   = 2f; 
    public float verticalCollisionOffset  = 0.0f;
    public float frictionCoeff   = 0.85f;
    public float bounceCoeff     = 0.3f;  
    public float airFriction     = 0.98f;   
    public float stopAccelerateThreshold;
    public float frictionWalkInfluence = 1;
 
    public LayerMask floorLayer;
 
    [field: NaughtyAttributes.ReadOnly][field: SerializeField]public Vector3 Velocity { get; set; }
    public float ZPosition 
    { 
        get => transform.position.z; 
        set { SetBodyPartZLevel(value);}
    }
    public float ZVelocity { get; set; }
    public bool IsGrounded { get; set; }
    public bool IsGroundedPrev { get; set; }

    [field: NaughtyAttributes.ReadOnly][field: SerializeField] public Vector3 ExForce { get; set; }
    public float Mass {get;set;}
    [field: SerializeField] public MovePoint MovePoint { get; set; }
    public Vector3 LastSetDir { get; set; }
    public bool IsSprinting { get; set; }
    public bool IsWalking { get; set; }
    public bool IsWalkingDominant
    {
        get
        {
            float spd = IsSprinting ? WalkSpeed * SprintCoeff : WalkSpeed;
            //Debug.Log(IsWalking && IsGrounded && ExForce.magnitude < spd * Mass);
            return IsWalking && IsGrounded && ExForce.magnitude < spd * Mass;
        }
    }

    public Vector2 WalkingDir { get; set; }
    public float SprintCoeff { get; set; }
    [field: SerializeField]public float WalkSpeed { get; set; }
    public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public AudioDataSO FootStepAudio { get; set; }
    public bool IsKnockbackMove { get; set; }
    [field: SerializeField] public BodyComponent[] BodyComponents { get; set; }

    public Vector2 TempVelocity { get; set; }
 
    public float MoveBudget { get; set; }
 
    public Vector3 PrevEntityPos { get; set; }
    public bool CollisionResolved { get; set; }
    public bool DelayFollowMove { get; set; }

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
        MovePoint.Initialize(this);
        ExForce = new();
        PrevEntityPos = entityTransform.position;
        physManager.AddPhysicsObject(this);
    }

    public void SetBodyPartZLevel(float z)
    {
        float d = z - transform.position.z;
        for(int i = 0; i < BodyComponents.Length; i++) BodyComponents[i].SetZ(z, d);
        MovePoint.transform.position += Vector3.forward * d;
        transform.position += Vector3.forward * d;
    }

    public Vector3 _prevRenderPos;
    public Vector3 _nextRenderPos;
 
}