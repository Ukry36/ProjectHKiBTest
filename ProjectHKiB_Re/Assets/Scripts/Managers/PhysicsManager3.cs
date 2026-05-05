using UnityEngine;

public class PhysicsManager3 : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform       entityTransform;
 
    [Header("격자")]
    [SerializeField] private float           gridSize        = 1f; 
 
    [Header("Z축 (높이)")]
    [SerializeField] private float           gravity         = -9.8f;
    [SerializeField] private float           Height   = 2f; 
    [SerializeField] private float           verticalCollisionOffset  = 0.0f;
 
    [Header("물리 계수")]
    [SerializeField] private float           frictionCoeff   = 0.85f;
    [SerializeField] private float           bounceCoeff     = 0.3f;  
    [SerializeField] private float           airFriction     = 0.98f;   
    [SerializeField] private float           stopThreshold   = 0.05f;  
    [SerializeField] private float           stopAccelerateThreshold;
 
    [Header("레이어")]
    [SerializeField] private LayerMask       wallLayer;
    [SerializeField] private LayerMask       floorLayer;
 
    public Vector3 Velocity { get; private set; }
    public float ZPosition { get; private set; }
    public float ZVelocity { get; private set; }
    public bool IsGrounded;

    public ExternalForce ExForce { get; set; }
    public float Mass {get;set;}
    public MovePoint MovePoint { get; set; }
    public Vector3 LastSetDir { get; set; }
    public bool IsSprinting { get; set; }
    public bool IsWalking { get; set; }
    public Vector2 WalkingDir { get; set; }
    public float SprintCoeff { get; set; }
    public float WalkSpeed { get; set; }
 
    private const int GRAVITY_FORCE_ID    = 0;
    private const int GROUND_FORCE_ID     = 1;
    private const int WALL_REACT_FORCE_ID = 2;
    private const int PULSE_FORCE_ID = 3;
 
    private float moveBudget;
 
    private Vector3 prevEntityPos;
 
    private ContactFilter2D contactFilter = new();
    private Collider2D[]    overlapBuffer = new Collider2D[16];

    private void Awake()
    {
 
        prevEntityPos = entityTransform.position;
 
        ExForce.SetForce(GRAVITY_FORCE_ID,    Vector3.zero);
        ExForce.SetForce(GROUND_FORCE_ID,     Vector3.zero);
        ExForce.SetForce(WALL_REACT_FORCE_ID, Vector3.zero);
        ExForce.SetForce(PULSE_FORCE_ID,      Vector3.zero);
    }
 
    private void FixedUpdate()
    {
        // 1. Update vertical Physics 
        UpdateVerticalPhysics(ExForce.GetTotalForce().z);

        // 2. Calculate XY Total Force
        Vector2 horizonForce = (Vector2)ExForce.GetTotalForce();
        Vector2 horizonVelocity   = (Vector2)Velocity;

        horizonVelocity += horizonForce / Mass * Time.fixedDeltaTime;

        // 3. Blend walk intent to current horizontal velocity
        if (IsWalking)
            horizonVelocity = BlendWalkIntent(horizonVelocity, WalkingDir);

        // 4. Apply friction
        horizonVelocity = ApplyFriction(horizonVelocity);
        Velocity = new Vector3(horizonVelocity.x, horizonVelocity.y, Velocity.z);

        // 5. Stop if too slow
        if (horizonVelocity.magnitude <= stopThreshold)
        {
            SnapMovePointToGrid();
            return;
        }

        // 6. Update horizontal Physics 
        UpdateHorizontalPhysics(horizonVelocity.normalized, ref horizonVelocity, ref moveBudget);
        SnapMovePointToGrid();

        // 7. Entity follows MovePoint
        Vector3 targetPos = new(MovePoint.transform.position.x,
                                MovePoint.transform.position.y,
                                ZPosition);
        entityTransform.position = Vector3.MoveTowards(
            entityTransform.position, targetPos,
            horizonVelocity.magnitude * Time.fixedDeltaTime);

        prevEntityPos = entityTransform.position;
        
        // 8. Reset temporary force
        ExForce.SetForce(PULSE_FORCE_ID, Vector3.zero);
    }

    private void UpdateVerticalPhysics(float zForce)
    {
        if (IsGrounded && zForce <= 0f)
        {
            ExForce.SetForce(GROUND_FORCE_ID, Vector3.back * zForce);
            ZVelocity = 0f;
        }
        else
        {
            ZVelocity += gravity * Time.fixedDeltaTime;
            ZPosition += ZVelocity * Time.fixedDeltaTime;

            int cnt = Physics2D.OverlapCircleNonAlloc(
                MovePoint.transform.position, 0.4f,
                overlapBuffer, floorLayer,
                contactFilter.minDepth - 0.01f, contactFilter.maxDepth);

            IsGrounded = cnt > 0;

            if (IsGrounded)
            {
                ZPosition = overlapBuffer[0].transform.position.z + 1f;
                ZVelocity = -ZVelocity * bounceCoeff;
                if (Mathf.Abs(ZVelocity) < stopThreshold) ZVelocity = 0f;
                ZVelocity = Mathf.Clamp(ZVelocity,
                    -stopAccelerateThreshold, stopAccelerateThreshold);
            }
        }

        Vector3 ep = entityTransform.position;
        entityTransform.position = new Vector3(ep.x, ep.y, ZPosition);

        contactFilter.useDepth    = true;
        contactFilter.minDepth    = ZPosition + verticalCollisionOffset;
        contactFilter.maxDepth    = ZPosition + Height + verticalCollisionOffset;
        contactFilter.useTriggers = false;
    }

    private Vector2 BlendWalkIntent(Vector2 currentVel, Vector2 walkDir)
    {
        float speed = WalkSpeed * (IsSprinting ? SprintCoeff : 1f);
        Vector2 walkVel = walkDir.normalized * speed;

        float externalMag = currentVel.magnitude;
        float walkInfluence = Mathf.Clamp01(1f - externalMag / (speed * 3f));

        return Vector2.Lerp(currentVel, currentVel + walkVel, walkInfluence);
    }

    private void UpdateHorizontalPhysics(Vector2 dir, ref Vector2 vel, ref float budget)
    {
        Transform mpTr = MovePoint.transform;
        int safetyLoop = 20;
        budget = vel.magnitude;

        while (budget > stopThreshold && safetyLoop-- > 0)
        {
            float stepDist = Mathf.Min(budget, gridSize);
            RaycastHit2D hit = CastWithFilter((Vector2)mpTr.position, dir, stepDist);

            if (!hit)
            {
                Vector2 targetPos = SnapToGrid((Vector2)mpTr.position + dir * stepDist);
                mpTr.position = new Vector3(targetPos.x, targetPos.y, ZPosition);
                budget -= stepDist;
            }
            else
            {
                IMovable hitMovable = hit.collider.GetComponentInParent<IMovable>();
                if (hitMovable != null)
                    TransferForce(hitMovable, vel, hit.normal);

                Vector2 reflect = Vector2.Reflect(vel, hit.normal) * bounceCoeff;
                vel = reflect;
                dir = vel.normalized;
                budget = vel.magnitude;

                ExForce.SetForce(WALL_REACT_FORCE_ID, new Vector3(-vel.x, -vel.y, 0f) * Mass);

                dir = GetAvailableDirHorizontal((Vector2)mpTr.position, dir);
                if (dir == Vector2.zero) break;
            }

            if (budget <= stopThreshold) break;
        }

        Velocity = new Vector3(vel.x, vel.y, Velocity.z);
    }

    private Vector3 GetAvailableDirHorizontal(Vector2 origin, Vector2 dir)
    {
        if (dir == Vector2.zero) return Vector2.zero;
 
        // check if entity can simply go through
        if (!OverlapCheckHorizontal(origin + dir.normalized * gridSize, 0.2f))
        {
            LastSetDir = dir;
            return dir;
        }

        // else, there is something
        Vector2 moveDir = dir;
        Vector2 xTilt = (dir.x * Vector2.right).normalized;
        Vector2 yTilt = (dir.y * Vector2.up).normalized;
 
        bool xWalled = dir.x != 0f && OverlapCheckHorizontal(origin + xTilt, 0.4f);
        bool yWalled = dir.y != 0f && OverlapCheckHorizontal(origin + yTilt, 0.4f);
 
        if (xWalled) moveDir.x = 0f;
        if (yWalled) moveDir.y = 0f;
 
        // if there is no wall at x, y dir, it might mean there is only diagonal wall
        // if so, randomly choose where to go
        if (!xWalled && !yWalled)
        {
            if (OverlapCheckHorizontal(origin + xTilt + yTilt, 0.4f))
                moveDir = Random.value > 0.5f ? moveDir.x * Vector2.right : moveDir.y * Vector2.up;
        }
 
        // seek 45d slides 
        if (moveDir == Vector2.zero)
        {
            Vector2 slideUp   = Quaternion.Euler(0, 0,  45) * dir;
            Vector2 slideDown = Quaternion.Euler(0, 0, -45) * dir;
 
            bool canUp   = !OverlapCheckHorizontal(origin + slideUp   * 0.5f, 0.2f);
            bool canDown = !OverlapCheckHorizontal(origin + slideDown  * 0.5f, 0.2f);
 
            if (canUp && canDown)
            {
                moveDir = Random.value > 0.5f ? slideUp : slideDown;
                LastSetDir = moveDir;
                return moveDir;
            }
            if (canUp)   { LastSetDir = slideUp;   return slideUp;   }
            if (canDown) { LastSetDir = slideDown;  return slideDown; }
 
            LastSetDir = dir;
            return Vector2.zero; // completely stops
        }
 
        LastSetDir = moveDir;
        return moveDir;
    }

    private Vector2 ApplyFriction(Vector2 vel)
    {
        if (IsGrounded)
        {
            vel *= frictionCoeff;
            if (vel.magnitude < stopThreshold)
                vel = Vector2.zero;
        }
        else
        {
            vel *= airFriction;
        }
        return vel;
    }

    private void TransferForce(IMovable target, Vector2 vel, Vector2 normal)
    {
        target.ExForce.SetForce(PULSE_FORCE_ID, (Vector3)vel * Mass);
    }
 
    private bool OverlapCheckHorizontal(Vector2 point, float radius)
    {
        int cnt = Physics2D.OverlapCircleNonAlloc(point, radius, overlapBuffer, wallLayer, contactFilter.minDepth, contactFilter.maxDepth);
        for (int i = 0; i < cnt; i++)
        {
            Transform t = overlapBuffer[i].transform;
            if (t == entityTransform) continue;
            if (t == MovePoint.transform) continue;
            return true;
        }
        return false;
    }

    private RaycastHit2D CastWithFilter(Vector2 origin, Vector2 dir, float dist)
    {
        RaycastHit2D[] hits = new RaycastHit2D[8];
        int cnt = Physics2D.Raycast(origin, dir, contactFilter, hits, dist);
        for (int i = 0; i < cnt; i++)
        {
            if (hits[i].transform == entityTransform) continue;
            if (hits[i].transform == MovePoint.transform) continue;
            return hits[i];
        }
        return default;
    }
 
    private Vector2 SnapToGrid(Vector2 pos)
    {
        return new Vector2(
            Mathf.Round(pos.x / gridSize) * gridSize,
            Mathf.Round(pos.y / gridSize) * gridSize
        );
    }
 
    private void SnapMovePointToGrid()
    {
        Transform mpTr   = MovePoint.transform;
        Vector2   snapped = SnapToGrid(mpTr.position);
        mpTr.position = new Vector3(snapped.x, snapped.y, ZPosition);
    }
}