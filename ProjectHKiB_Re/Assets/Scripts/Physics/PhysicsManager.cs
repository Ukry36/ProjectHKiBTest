using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    private List<PhysicsObjectTest> AllPhysicsEntitys = new();

    public const int GRAVITY_FORCE_ID    = 0;
    public const int GROUND_FORCE_ID     = 1;
    public const int WALL_REACT_FORCE_ID = 2;
    public const int IMPULSE_FORCE_ID = 3;
    public const float EPSILON = 0.00001f;
    
    private Collider2D[] overlapBuffer = new Collider2D[32];
    public float gravity = -9.8f;
    public float gridSize = 1f; 
    public float settleBlendThreshold = 0.5f;
    public float settleStrength = 8f;
    public float settleQuitDist = 0.1f;
    public float stopThreshold = 0.01f;  
    public int MaxPhysicsStep = 10;

    public void AddPhysicsObject(PhysicsObjectTest obj)
    {
        AllPhysicsEntitys.Add(obj);
    }
    
    private void FixedUpdate()
    {
        for (int i = 0; i < AllPhysicsEntitys.Count; i++)
        {
            PhysicsObjectTest obj = AllPhysicsEntitys[i];
            // 1. Update vertical Physics 
            UpdateVerticalPhysics(obj);

            // 2. Preprocess horizontal force
            Vector2 horizonForce = (Vector2)obj.ExForce;
            obj.TempVelocity     = (Vector2)obj.Velocity;
            obj.TempVelocity     = ApplyFriction(obj, obj.TempVelocity);
            obj.TempVelocity    += horizonForce / obj.Mass * Time.fixedDeltaTime;

            // 3. walkMove
            obj.RealWalkingVector = GetModifiedVelocityHorizontal(obj, obj.MovePoint.transform.position, obj.WalkingVector);
            obj.TempVelocity += obj.RealWalkingVector;

            // 4. Prepare for physics update
            float speed = obj.TempVelocity.magnitude;
            //float budgetBlend = Mathf.Clamp01(speed / settleBlendThreshold);
            obj.MoveBudget += speed * Time.fixedDeltaTime;
            //obj.MoveBudget *= budgetBlend;
        }

        for (int i = 0; i < MaxPhysicsStep; i++) // 5. Update horizontal physics
        {
            for (int j = 0; j < AllPhysicsEntitys.Count; j++)
            {
                PhysicsObjectTest obj = AllPhysicsEntitys[j];

                // Stop if all the budgets are consumed or the velocity is too slow
                if (obj.MoveBudget <= 0 && !obj.DelayFollowMove || obj.TempVelocity.magnitude <= stopThreshold) continue;
                
                obj.DelayFollowMove = false;

                // Update horizontal Physics 
                if (!obj.CollisionResolved) //this is for detecting "collision resolve from another entity" this step
                {
                    float distToMovePoint = Vector3.Distance(obj.transform.position, obj.MovePoint.transform.position);
                    if ((obj.IsWalkingDominant && distToMovePoint < EPSILON) || !obj.IsWalkingDominant)
                    {
                        StepHorizontalPhysics(obj, obj.TempVelocity.normalized);
                    }
                    if (!obj.DelayFollowMove) FollowMovepoint(obj); //if collision occourd, delay the FollowMovepoint to next step
                }
                obj.CollisionResolved = false;
            }
        }

        for (int i = 0; i < AllPhysicsEntitys.Count; i++) // 6. Reset things
        {
            PhysicsObjectTest obj = AllPhysicsEntitys[i];
            SnapMovePointToGrid(obj);
            if (!obj.IsWalkingDominant) SettleToGrid(obj);
            obj.PrevEntityPos = obj.transform.position;
            obj.LastSetDir = obj.IsWalking ? obj.WalkingDir : obj.Velocity.normalized;
            obj.Velocity = new Vector3(obj.TempVelocity.x, obj.TempVelocity.y, obj.Velocity.z);
            obj.ExForce = Vector3.zero;
            if (obj.MoveBudget < 0) obj.MoveBudget = 0;
            obj.CollisionResolved = false;
            obj.DelayFollowMove = false;
            RecordRenderPosition(obj);
        }
    }

    private void UpdateVerticalPhysics(PhysicsObjectTest obj)
    {
        obj.ExForce += Vector3.forward * gravity;
        int cnt = ZPhysics2D.OverlapCircleNonAlloc(
                obj.MovePoint.transform.position, 0.4f,
                overlapBuffer, obj.floorLayer,
                obj.zCollider.ZMin - EPSILON, obj.zCollider.ZMax + EPSILON);
        obj.ZVelocity += obj.ExForce.z / obj.Mass * Time.fixedDeltaTime;
        obj.IsGrounded = cnt > 0;

        if (obj.IsGrounded && obj.ExForce.z <= EPSILON)
        {
            obj.ZPosition = overlapBuffer[0].ZGetTop();
            if (!obj.IsGroundedPrev)
            {
                obj.ZVelocity = -obj.ZVelocity * obj.bounceCoeff;
                if (Mathf.Abs(obj.ZVelocity) < stopThreshold) obj.ZVelocity = 0f;
                obj.ZVelocity = Mathf.Clamp(obj.ZVelocity,
                    -obj.stopAccelerateThreshold, obj.stopAccelerateThreshold);
            }
            else
            {
                obj.ExForce += Vector3.back * obj.ExForce.z;
                obj.ZVelocity = 0f;
            }
        }
        else obj.ZPosition += obj.ZVelocity * Time.fixedDeltaTime;
        
        obj.IsGroundedPrev = obj.IsGrounded;
        Vector3 ep = obj.transform.position;
        obj.transform.position = new Vector3(ep.x, ep.y, obj.ZPosition);
    }

    private void FollowMovepoint(PhysicsObjectTest obj)
    {
        Vector3 prevPos    = obj.transform.position;
        float   followDist = obj.TempVelocity.magnitude * Time.fixedDeltaTime;
        Vector3 target = obj.IsWalkingDominant
            ? new Vector3(obj.MovePoint.transform.position.x,
                          obj.MovePoint.transform.position.y,
                          obj.ZPosition)
            : new Vector3(prevPos.x + obj.TempVelocity.x * Time.fixedDeltaTime,
                          prevPos.y + obj.TempVelocity.y * Time.fixedDeltaTime,
                          obj.ZPosition);                                        

        obj.transform.position = Vector3.MoveTowards(obj.transform.position, target, followDist);
        obj.MoveBudget -= Vector3.Distance(prevPos, obj.transform.position);
    }
    
    private void SettleToGrid(PhysicsObjectTest obj)
    {
        float speed = obj.TempVelocity.magnitude;
        if (speed >= settleBlendThreshold) return;

        float t           = 1f - Mathf.Clamp01(speed / settleBlendThreshold);
        float blendAmount = t * settleStrength * Time.fixedDeltaTime;

        Vector2 entityPos  = obj.transform.position;
        Vector2 snappedPos = (Vector2)obj.MovePoint.transform.position - ((Vector2)obj.MovePoint.transform.position - entityPos).normalized * settleQuitDist;
        Vector2 delta      = snappedPos - entityPos;

        if (delta.magnitude < EPSILON)
        {
            obj.transform.position  = new Vector3(snappedPos.x, snappedPos.y, obj.ZPosition);
            obj.TempVelocity              = Vector2.zero;
            return;
        }

        Vector2 newEntityPos         = Vector2.Lerp(entityPos, snappedPos, blendAmount);
        obj.transform.position = new Vector3(newEntityPos.x, newEntityPos.y, obj.ZPosition);
        Vector2 settleCorrection = delta.normalized * (delta.magnitude * t * settleStrength);
        obj.TempVelocity        += settleCorrection * Time.fixedDeltaTime;
    }

    private void StepHorizontalPhysics(PhysicsObjectTest obj, Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        Transform    mpTr = obj.MovePoint.transform;
        RaycastHit2D hit  = CastWithFilterHorizontal(obj, (Vector2)mpTr.position, dir, gridSize);

        if (!hit)
        {
            float advance = obj.MoveBudget < gridSize ? obj.MoveBudget : gridSize;
            if (obj.IsWalkingDominant) advance = gridSize;
            Vector2 targetPos = SnapToGrid((Vector2)obj.transform.position + dir * advance);
            mpTr.position = new Vector3(targetPos.x, targetPos.y, obj.ZPosition);
        }
        else
        {
            bool hitIsMovable = hit.collider.TryGetComponent(out IMovable hitMovable);
            PhysicsObjectTest otherObj = hitIsMovable ? hitMovable as PhysicsObjectTest : null;

            if (hitIsMovable && otherObj != null)
            {
                ResolveEntityCollision(obj, otherObj, hit.normal, (Vector2)mpTr.position);
                otherObj.MoveBudget   = otherObj.TempVelocity.magnitude * Time.fixedDeltaTime;
            }
            else ResolveStaticCollision(obj, hit.normal, (Vector2)mpTr.position);
            obj.MoveBudget = obj.TempVelocity.magnitude * Time.fixedDeltaTime;
            obj.DelayFollowMove = true;
        }
        if (!obj.IsWalkingDominant)
            obj.Velocity = new Vector3(obj.TempVelocity.x, obj.TempVelocity.y, obj.Velocity.z);
    }
    private void ResolveStaticCollision(PhysicsObjectTest obj, Vector2 normal, Vector2 origin)
    {
        if (obj.IsWalkingDominant)
        {
            obj.TempVelocity = GetModifiedVelocityHorizontal(obj, origin, obj.TempVelocity);
        }
        else
        {
            obj.TempVelocity -= (1f + obj.bounceCoeff) * Vector2.Dot(obj.TempVelocity, normal) * normal;
            obj.ExForce += new Vector3(-obj.TempVelocity.x, -obj.TempVelocity.y, 0f) * obj.Mass;
        }
    }
    private void ResolveEntityCollision(PhysicsObjectTest obj, PhysicsObjectTest otherObj, Vector2 normal, Vector2 origin)
    {
        bool objWalking   = obj.IsWalkingDominant;
        bool otherWalking = otherObj.IsWalkingDominant;

        if (objWalking || otherWalking) 
        {
            if (objWalking)   // obj == walk → obj slide
            {
                obj.TempVelocity = GetModifiedVelocityHorizontal(obj, origin, obj.TempVelocity);
            }
            else              // obj == phys, other == walk → obj Impulse
            {
                obj.TempVelocity -= (1f + obj.bounceCoeff) * Vector2.Dot(obj.TempVelocity, normal) * normal;
            }

            if (otherWalking) // other == walk → other slide (use its LastSetDir)
            {
                otherObj.TempVelocity = GetModifiedVelocityHorizontal(otherObj, otherObj.MovePoint.transform.position, otherObj.TempVelocity);
            }
            else              // obj == walk, other == phys → other Impulse
            {
                Vector2 impulseOnOther = obj.Mass * obj.TempVelocity.magnitude * obj.TempVelocity.normalized / otherObj.Mass;
                otherObj.TempVelocity += impulseOnOther * (1f + otherObj.bounceCoeff);
            }
        }
        else // all two are phys
        {
            Vector2 vA   = obj.TempVelocity;
            Vector2 vB   = otherObj.TempVelocity;
            float   mA   = obj.Mass;
            float   mB   = otherObj.Mass;

            float vRel = Vector2.Dot(vA - vB, normal);
            if (vRel <= 0f) return; // already moving away (prevents multiple collision calc)

            float e = (obj.bounceCoeff + otherObj.bounceCoeff) * 0.5f;
            float j = -(1f + e) * vRel / (1f / mA + 1f / mB);

            Vector2 impulse = j * normal;

            obj.TempVelocity      += impulse / mA;
            otherObj.TempVelocity -= impulse / mB;
        }

        if (!objWalking)
            obj.ExForce += new Vector3(-obj.TempVelocity.x, -obj.TempVelocity.y, 0f) * obj.Mass;
        if (!otherWalking)
            otherObj.ExForce += new Vector3(-otherObj.TempVelocity.x, -otherObj.TempVelocity.y, 0f) * otherObj.Mass;

        otherObj.Velocity = new Vector3(otherObj.TempVelocity.x, otherObj.TempVelocity.y, otherObj.Velocity.z);
        otherObj.CollisionResolved = true;
        otherObj.DelayFollowMove = true;
    }
    private Vector3 GetModifiedVelocityHorizontal(PhysicsObjectTest obj, Vector2 origin, Vector2 vector)
    {
        Vector2 dir = vector.normalized;
        if (dir == Vector2.zero) return Vector2.zero;
 
        // check if entity can simply go through
        if (!OverlapCheckHorizontal(obj, origin + dir * gridSize, 0.2f))
        {
            return vector;
        }

        // else, there is something
        Vector2 result = vector;
        Vector2 xTilt   = new(Mathf.Sign(dir.x), 0f);
        Vector2 yTilt   = new(0f, Mathf.Sign(dir.y));
 
        bool xWalled = dir.x != 0f && OverlapCheckHorizontal(obj, origin + xTilt, 0.4f);
        bool yWalled = dir.y != 0f && OverlapCheckHorizontal(obj, origin + yTilt, 0.4f);
 
        if (xWalled) result.x = 0f;
        if (yWalled) result.y = 0f;
 
        // if there is no wall at x, y dir, it might mean there is only diagonal wall
        // if so, randomly choose where to go
        if (!xWalled && !yWalled)
        {
            if (OverlapCheckHorizontal(obj, origin + xTilt + yTilt, 0.4f))
                result *= Random.value > 0.5f ? xTilt : yTilt;
        }
 
        // seek 45d slides 
        if (result == Vector2.zero && dir.x != 0f && dir.y != 0f)
        {
            Vector2 slideUp   = Quaternion.Euler(0, 0,  45) * dir;
            Vector2 slideDown = Quaternion.Euler(0, 0, -45) * dir;
 
            bool canUp   = !OverlapCheckHorizontal(obj, origin + slideUp   * 0.5f, 0.2f);
            bool canDown = !OverlapCheckHorizontal(obj, origin + slideDown  * 0.5f, 0.2f);
 
            if (canUp && canDown)
            {
                result = Random.value > 0.5f ? slideUp : slideDown;
                return result;
            }
            if (canUp)   return slideUp;
            if (canDown) return slideDown;
 
            return Vector2.zero; // completely stops
        }
        return result;
    }
    private Vector2 ApplyFriction(PhysicsObjectTest obj, Vector2 vel)
    {
        if (obj.IsGrounded)
        {
            vel *= obj.frictionCoeff;
            if (vel.magnitude < stopThreshold)
                vel = Vector2.zero;
        }
        else
        {
            vel *= obj.airFriction;
        }
        return vel;
    }

    private bool OverlapCheckHorizontal(PhysicsObjectTest obj, Vector2 point, float radius)
    {
        int cnt = ZPhysics2D.OverlapCircleNonAlloc(point, radius, overlapBuffer,obj.WallLayer, obj.zCollider.ZMin, obj.zCollider.ZMax);
        for (int i = 0; i < cnt; i++)
        {
            Transform t = overlapBuffer[i].transform;
            if (t == obj.transform) continue;
            if (t == obj.MovePoint.transform) continue;
            return true;
        }
        return false;
    }
    private RaycastHit2D CastWithFilterHorizontal(PhysicsObjectTest obj, Vector2 origin, Vector2 dir, float dist)
    {
        RaycastHit2D[] hits = new RaycastHit2D[32];
        int cnt = ZPhysics2D.RaycastNonAlloc(origin, dir, dist, hits, overlapBuffer, obj.WallLayer, obj.zCollider.ZMin, obj.zCollider.ZMax);
        for (int i = 0; i < cnt; i++)
        {
            if (hits[i].transform == obj.transform) continue;
            if (hits[i].transform == obj.MovePoint.transform) continue;
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
    private void SnapMovePointToGrid(PhysicsObjectTest obj)
    {
        Transform mpTr   = obj.MovePoint.transform;
        Vector2   snapped = SnapToGrid(mpTr.position);
        mpTr.position = new Vector3(snapped.x, snapped.y, obj.ZPosition);
    }

    public void RecordRenderPosition(PhysicsObjectTest obj)
    {
        obj._prevRenderPos = obj._nextRenderPos;
        obj._nextRenderPos = obj.transform.position;
    }

    private void Update()
    {
        float alpha = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
        foreach (var obj in AllPhysicsEntitys)
        {
            Vector3 interpolated = Vector3.Lerp(obj._prevRenderPos, obj._nextRenderPos, alpha);
            obj.transform.position = interpolated;
        }
    }
    
}