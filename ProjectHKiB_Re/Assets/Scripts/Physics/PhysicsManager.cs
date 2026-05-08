using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    private List<PhysicsObjectTest> AllPhysicsEntitys = new();

    public const int GRAVITY_FORCE_ID    = 0;
    public const int GROUND_FORCE_ID     = 1;
    public const int WALL_REACT_FORCE_ID = 2;
    public const int IMPULSE_FORCE_ID = 3;
    public const float EPSILON = 0.0001f;
    
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
        obj.ExForce.SetForce(GRAVITY_FORCE_ID, Vector3.forward * gravity);
    }
    
    private void FixedUpdate()
    {
        for (int i = 0; i < AllPhysicsEntitys.Count; i++)
        {
            PhysicsObjectTest obj = AllPhysicsEntitys[i];
            // 1. Update vertical Physics 
            UpdateVerticalPhysics(obj, obj.ExForce.GetTotalForce().z);

            // 2. Preprocess horizontal force
            obj.Velocity = ApplyFriction(obj, obj.Velocity);
            Vector2 horizonForce = (Vector2)obj.ExForce.GetTotalForce();
            obj.tempVelocity   = (Vector2)obj.Velocity;
            obj.tempVelocity += horizonForce / obj.Mass * Time.fixedDeltaTime;

            // 3. Blend walk intent to current horizontal obj.velocity
            if (obj.IsWalking)
                obj.tempVelocity = BlendWalkIntent(obj, obj.tempVelocity, obj.WalkingDir);

            // 4. Prepare for physics update
            float speed = obj.tempVelocity.magnitude;
            float budgetBlend = Mathf.Clamp01(speed / settleBlendThreshold);
            obj.moveBudget += speed * Time.fixedDeltaTime;
            obj.moveBudget *= budgetBlend;
        }

        for (int i = 0; i < MaxPhysicsStep; i++) // 5. Update horizontal physics
        {
            for (int j = 0; j < AllPhysicsEntitys.Count; j++)
            {
                PhysicsObjectTest obj = AllPhysicsEntitys[j];

                // Stop if all the budgets are consumed or the velocity is too slow
                if (obj.moveBudget <= 0 && !obj.delayFollowMove || obj.tempVelocity.magnitude <= stopThreshold) continue;
                
                obj.delayFollowMove = false;

                // Update horizontal Physics 
                if (!obj.collisionResolved) //this is for detecting "collision resolve from another entity" this step
                {
                    float distToMovePoint = Vector2.Distance(
                        (Vector2)obj.entityTransform.position,
                        (Vector2)obj.MovePoint.transform.position);
                    if (obj.IsWalkingDominant && distToMovePoint < EPSILON && obj.IsWalking || !obj.IsWalkingDominant)
                    {
                        StepHorizontalPhysics(obj, obj.tempVelocity.normalized);
                    }
                    if (!obj.delayFollowMove) FollowMovepoint(obj);          //if collision occourd, delay the FollowMovepoint to next step
                }
                obj.collisionResolved = false;
            }
        }

        for (int i = 0; i < AllPhysicsEntitys.Count; i++)
        {
            // 6. Reset things
            PhysicsObjectTest obj = AllPhysicsEntitys[i];
            SnapMovePointToGrid(obj);
            SettleToGrid(obj);
            obj.prevEntityPos = obj.entityTransform.position;
            obj.LastSetDir = obj.IsWalking ? obj.WalkingDir : obj.Velocity.normalized;
            obj.ExForce.SetForce(IMPULSE_FORCE_ID, Vector3.zero);
            obj.Velocity = new Vector3(obj.tempVelocity.x, obj.tempVelocity.y, obj.Velocity.z);
            if (obj.moveBudget < 0) obj.moveBudget = 0;
            obj.collisionResolved = false;
            obj.delayFollowMove = false;
        }
    }

    private void UpdateVerticalPhysics(PhysicsObjectTest obj, float zForce)
    {
        if (obj.IsGrounded && zForce <= 0f)
        {
            obj.ExForce.SetForce(GROUND_FORCE_ID, Vector3.back * zForce);
            obj.ZVelocity = 0f;
        }
        else
        {
            obj.ZVelocity += zForce / obj.Mass * Time.fixedDeltaTime;
            obj.ZPosition += obj.ZVelocity * Time.fixedDeltaTime;

            int cnt = ZPhysics2D.OverlapCircleNonAlloc(
                obj.MovePoint.transform.position, 0.4f,
                overlapBuffer, obj.floorLayer,
                obj.contactFilterVectical.minDepth - 0.01f, obj.contactFilterVectical.maxDepth);

            obj.IsGrounded = cnt > 0;

            if (obj.IsGrounded)
            {
                obj.ZPosition = overlapBuffer[0].ZGetTop();
                obj.ZVelocity = -obj.ZVelocity * obj.bounceCoeff;
                if (Mathf.Abs(obj.ZVelocity) < stopThreshold) obj.ZVelocity = 0f;
                obj.ZVelocity = Mathf.Clamp(obj.ZVelocity,
                    -obj.stopAccelerateThreshold, obj.stopAccelerateThreshold);
            }
        }

        Vector3 ep = obj.entityTransform.position;
        obj.entityTransform.position = new Vector3(ep.x, ep.y, obj.ZPosition);

        obj.contactFilterVectical.useDepth  = true;
        obj.contactFilterVectical.SetDepth(obj.ZPosition + obj.verticalCollisionOffset + EPSILON, obj.ZPosition + obj.Height + obj.verticalCollisionOffset - EPSILON);
        obj.contactFilterVectical.SetLayerMask(obj.floorLayer);
        obj.contactFilterHorizontal.useDepth  = true;
        obj.contactFilterHorizontal.SetDepth(obj.ZPosition + obj.verticalCollisionOffset + EPSILON, obj.ZPosition + obj.Height + obj.verticalCollisionOffset - EPSILON);
        obj.contactFilterHorizontal.SetLayerMask(obj.WallLayer);
    }

    private Vector2 BlendWalkIntent(PhysicsObjectTest obj, Vector2 currentVel, Vector2 walkDir)
    {
        float speed = obj.WalkSpeed * (obj.IsSprinting ? obj.SprintCoeff : 1f);
        if (obj.IsGrounded) speed *= 1 - Mathf.Clamp01(obj.frictionCoeff * obj.frictionWalkInfluence);
        else                speed *= (1 - Mathf.Clamp01(obj.airFriction * obj.frictionWalkInfluence)) * 0.1f;
        return currentVel + walkDir.normalized * speed;
    }

    private void FollowMovepoint(PhysicsObjectTest obj)
    {
        Vector3 prevPos    = obj.entityTransform.position;
        float   followDist = obj.tempVelocity.magnitude * Time.fixedDeltaTime;
        Vector3 target = obj.IsWalkingDominant
            ? new Vector3(obj.MovePoint.transform.position.x,
                          obj.MovePoint.transform.position.y,
                          obj.ZPosition)
            : new Vector3(prevPos.x + obj.tempVelocity.x * Time.fixedDeltaTime,
                          prevPos.y + obj.tempVelocity.y * Time.fixedDeltaTime,
                          obj.ZPosition);                                        

        obj.entityTransform.position = Vector3.MoveTowards(obj.entityTransform.position, target, followDist);
        obj.moveBudget -= Vector3.Distance(prevPos, obj.entityTransform.position);
    }
    
    private void SettleToGrid(PhysicsObjectTest obj)
    {
        float speed = obj.tempVelocity.magnitude;
        if (speed >= settleBlendThreshold) return;

        float t           = 1f - Mathf.Pow(Mathf.Clamp01(speed / settleBlendThreshold), 4);
        float blendAmount = t * settleStrength * Time.fixedDeltaTime;

        Vector2 entityPos  = obj.entityTransform.position;
        Vector2 snappedPos = (Vector2)obj.MovePoint.transform.position - ((Vector2)obj.MovePoint.transform.position - entityPos).normalized * settleQuitDist;
        Vector2 delta      = snappedPos - entityPos;

        if (delta.magnitude < EPSILON)
        {
            obj.entityTransform.position  = new Vector3(snappedPos.x, snappedPos.y, obj.ZPosition);
            obj.tempVelocity              = Vector2.zero;
            return;
        }

        Vector2 newEntityPos         = Vector2.Lerp(entityPos, snappedPos, blendAmount);
        obj.entityTransform.position = new Vector3(newEntityPos.x, newEntityPos.y, obj.ZPosition);
        Vector2 settleCorrection = delta.normalized * (delta.magnitude * t * settleStrength);
        obj.tempVelocity        += settleCorrection * Time.fixedDeltaTime;
    }

    private void StepHorizontalPhysics(PhysicsObjectTest obj, Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        Transform    mpTr = obj.MovePoint.transform;
        RaycastHit2D hit  = CastWithFilterHorizontal(obj, (Vector2)obj.transform.position, dir, gridSize);

        if (!hit)
        {
            float advance = obj.moveBudget < gridSize ? obj.moveBudget : gridSize;
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
                otherObj.moveBudget   = otherObj.tempVelocity.magnitude * Time.fixedDeltaTime;
            }
            else ResolveStaticCollision(obj, hit.normal, (Vector2)mpTr.position);
            obj.moveBudget = obj.tempVelocity.magnitude * Time.fixedDeltaTime;
            obj.delayFollowMove = true;
        }

        obj.Velocity = new Vector3(obj.tempVelocity.x, obj.tempVelocity.y, obj.Velocity.z);
    }
    private void ResolveStaticCollision(PhysicsObjectTest obj, Vector2 normal, Vector2 origin)
    {
        if (obj.IsWalkingDominant)
        {
            obj.tempVelocity = GetModifiedVelocityHorizontal(obj, origin, obj.tempVelocity);
        }
        else
        {
            obj.tempVelocity -= (1f + obj.bounceCoeff) * Vector2.Dot(obj.tempVelocity, normal) * normal;
            obj.ExForce.AddForce(IMPULSE_FORCE_ID, new Vector3(-obj.tempVelocity.x, -obj.tempVelocity.y, 0f) * obj.Mass);
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
                obj.tempVelocity = GetModifiedVelocityHorizontal(obj, origin, obj.tempVelocity);
            }
            else              // obj == phys, other == walk → obj Impulse
            {
                obj.tempVelocity -= (1f + obj.bounceCoeff) * Vector2.Dot(obj.tempVelocity, normal) * normal;
            }

            if (otherWalking) // other == walk → other slide (use its LastSetDir)
            {
                otherObj.tempVelocity = GetModifiedVelocityHorizontal(otherObj, otherObj.MovePoint.transform.position, otherObj.tempVelocity);
            }
            else              // obj == walk, other == phys → other Impulse
            {
                Vector2 impulseOnOther = obj.Mass * obj.tempVelocity.magnitude * obj.tempVelocity.normalized / otherObj.Mass;
                otherObj.tempVelocity += impulseOnOther * (1f + otherObj.bounceCoeff);
            }
        }
        else // all two are phys
        {
            Vector2 vA   = obj.tempVelocity;
            Vector2 vB   = otherObj.tempVelocity;
            float   mA   = obj.Mass;
            float   mB   = otherObj.Mass;

            float vRel = Vector2.Dot(vA - vB, normal);
            if (vRel <= 0f) return; // already moving away (prevents multiple collision calc)

            float e = (obj.bounceCoeff + otherObj.bounceCoeff) * 0.5f;
            float j = -(1f + e) * vRel / (1f / mA + 1f / mB);

            Vector2 impulse = j * normal;

            obj.tempVelocity      += impulse / mA;
            otherObj.tempVelocity -= impulse / mB;
        }

        if (!objWalking)
            obj.ExForce.AddForce(IMPULSE_FORCE_ID, new Vector3(-obj.tempVelocity.x, -obj.tempVelocity.y, 0f) * obj.Mass);
        if (!otherWalking)
            otherObj.ExForce.AddForce(IMPULSE_FORCE_ID, new Vector3(-otherObj.tempVelocity.x, -otherObj.tempVelocity.y, 0f) * otherObj.Mass);

        otherObj.Velocity = new Vector3(otherObj.tempVelocity.x, otherObj.tempVelocity.y, otherObj.Velocity.z);
        otherObj.collisionResolved = true;
        otherObj.delayFollowMove = true;
    }
    private Vector3 GetModifiedVelocityHorizontal(PhysicsObjectTest obj, Vector2 origin, Vector2 vector)
    {
        Vector2 dir = vector.normalized;
        if (dir == Vector2.zero) return Vector2.zero;
 
        // check if entity can simply go through
        if (!OverlapCheckHorizontal(obj, origin + dir * gridSize, 0.2f))
        {
            //obj.LastSetDir = dir;
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
            Vector2 slideUp   = Quaternion.Euler(0, 0,  45) * vector;
            Vector2 slideDown = Quaternion.Euler(0, 0, -45) * vector;
 
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
        int cnt = ZPhysics2D.OverlapCircleNonAlloc(point, radius, overlapBuffer,obj.WallLayer, obj.contactFilterHorizontal.minDepth, obj.contactFilterHorizontal.maxDepth);
        for (int i = 0; i < cnt; i++)
        {
            Transform t = overlapBuffer[i].transform;
            if (t == obj.entityTransform) continue;
            if (t == obj.MovePoint.transform) continue;
            return true;
        }
        return false;
    }
    private RaycastHit2D CastWithFilterHorizontal(PhysicsObjectTest obj, Vector2 origin, Vector2 dir, float dist)
    {
        RaycastHit2D[] hits = new RaycastHit2D[8];
        int cnt = ZPhysics2D.RaycastNonAlloc(origin, dir, dist, hits, overlapBuffer, obj.WallLayer, obj.contactFilterHorizontal.minDepth, obj.contactFilterHorizontal.maxDepth);
        for (int i = 0; i < cnt; i++)
        {
            if (hits[i].transform == obj.entityTransform) continue;
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
    
}