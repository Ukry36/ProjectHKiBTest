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
 
    public Vector3 Velocity { get; set; }
    public float ZPosition 
    { 
        get => transform.position.z; 
        set { SetBodyPartZLevel(value);}
    }
    public float ZVelocity { get; set; }
    public bool IsGrounded;

    public ExternalForce ExForce { get; set; }
    public float Mass {get;set;}
    [field: SerializeField] public MovePoint MovePoint { get; set; }
    public Vector3 LastSetDir { get; set; }
    public bool IsSprinting { get; set; }
    [field: SerializeField]public bool IsWalking { get; set; }
    
    public bool IsWalkingDominant 
    { 
        get
        {
            if (IsWalking && IsGrounded)
            {
                float spd = IsSprinting ? WalkSpeed * SprintCoeff : WalkSpeed;
                return spd * 1f > ExForce.GetTotalForce().magnitude;
            }
            else return false;
        }
    }

    [NaughtyAttributes.Button]
    public void Impulse()
    {
        ExForce.AddForce(PhysicsManager.IMPULSE_FORCE_ID, Vector2.right * 5);
    }

    public Vector2 WalkingDir { get; set; }
    public float SprintCoeff { get; set; }
    [field: SerializeField]public float WalkSpeed { get; set; }
    public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public AudioDataSO FootStepAudio { get; set; }
    public bool IsKnockbackMove { get; set; }
    [field: SerializeField] public BodyComponent[] BodyComponents { get; set; }

    public Vector2 tempVelocity;
 
    public float moveBudget;
 
    public Vector3 prevEntityPos;
    public bool collisionResolved;
    public bool delayFollowMove;
 
    public ContactFilter2D contactFilterVectical = new();
    public ContactFilter2D contactFilterHorizontal = new();

    public void KnockBack(Vector3 dir, float strength) => ExForce.AddForce(PhysicsManager.IMPULSE_FORCE_ID, dir * strength);
    public void EndKnockbackEarly(){}
    public void KnockBackEndCallback(){}

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IMovable>(this);
    }

    public void Initialize()
    {
        MovePoint.Initialize();
        ExForce = new();
        prevEntityPos = entityTransform.position;
        physManager.AddPhysicsObject(this);
    }

    public void SetBodyPartZLevel(float z)
    {
        float d = z - transform.position.z;
        for(int i = 0; i < BodyComponents.Length; i++) BodyComponents[i].SetZ(z, d);
        MovePoint.transform.position += Vector3.forward * d;
        transform.position += Vector3.forward * d;
    }
 
}