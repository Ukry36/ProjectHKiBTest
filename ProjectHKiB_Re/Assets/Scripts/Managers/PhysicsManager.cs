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
    public float settleBlendThreshold = 3f;
    public float settleStrength = 8f;
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

            // 2. Calculate XY Total Force
            Vector2 horizonForce = (Vector2)obj.ExForce.GetTotalForce();
            obj.tempVelocity   = (Vector2)obj.Velocity;
            
            obj.tempVelocity += horizonForce / obj.Mass * Time.fixedDeltaTime;

            // 3. Apply friction
            obj.tempVelocity = ApplyFriction(obj, obj.tempVelocity);
            Debug.Log(obj.tempVelocity);
            // 4. Blend walk intent to current horizontal obj.velocity
            if (obj.IsWalking)
                obj.tempVelocity = BlendWalkIntent(obj, obj.tempVelocity, obj.WalkingDir);

            // 4.5. Prepare for physics update
            float speed = obj.tempVelocity.magnitude;

            float budgetBlend = Mathf.Clamp01(speed / settleBlendThreshold);
            obj.moveBudget += speed * Time.fixedDeltaTime;
            obj.moveBudget *= budgetBlend;
            Debug.Log(obj.tempVelocity);
        }

        for (int i = 0; i < MaxPhysicsStep; i++)
        {
            for (int j = 0; j < AllPhysicsEntitys.Count; j++)
            {
                PhysicsObjectTest obj = AllPhysicsEntitys[j];
                // 5. Stop if too slow
                if (obj.tempVelocity.magnitude <= stopThreshold)
                {
                    obj.moveBudget = 0f;
                    SnapMovePointToGrid(obj);
                    continue;
                }

                // 6. Update horizontal Physics 
                StepHorizontalPhysics(obj, obj.tempVelocity.normalized);
                SnapMovePointToGrid(obj);
            }
        }

        for (int i = 0; i < AllPhysicsEntitys.Count; i++)
        {
            PhysicsObjectTest obj = AllPhysicsEntitys[i];

            // 7. Entity follows obj.MovePoint
            FollowMovepoint(obj);
            
            obj.prevEntityPos = obj.entityTransform.position;

            // 8. Reset cirtain things
            obj.LastSetDir = obj.IsWalkingDominant ? obj.WalkingDir : obj.Velocity.normalized;
            obj.ExForce.SetForce(IMPULSE_FORCE_ID, Vector3.zero);
            obj.Velocity = new Vector3(obj.tempVelocity.x, obj.tempVelocity.y, obj.Velocity.z);
            obj.resolvedThisStep = false;
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

            int cnt = Physics2D.OverlapCircleNonAlloc(
                obj.MovePoint.transform.position, 0.4f,
                overlapBuffer, obj.floorLayer,
                obj.contactFilterVectical.minDepth - 0.01f, obj.contactFilterVectical.maxDepth);

            obj.IsGrounded = cnt > 0;

            if (obj.IsGrounded)
            {
                obj.ZPosition = overlapBuffer[0].transform.position.z;
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
        Vector3 targetPos = new(
            obj.MovePoint.transform.position.x,
            obj.MovePoint.transform.position.y,
            obj.ZPosition);

        float speed = obj.tempVelocity.magnitude;

        float t = 1f - Mathf.Clamp01(speed / settleBlendThreshold);

        float physDist = speed * Time.fixedDeltaTime;

        float distToTarget = Vector2.Distance(obj.entityTransform.position, targetPos);
        float settleDist   = distToTarget * t * settleStrength * Time.fixedDeltaTime;

        float followDist = physDist + settleDist;

        obj.entityTransform.position = Vector3.MoveTowards(
            obj.entityTransform.position, targetPos, followDist);
    }

    private void StepHorizontalPhysics(PhysicsObjectTest obj, Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        Transform    mpTr = obj.MovePoint.transform;
        RaycastHit2D hit  = CastWithFilterHorizontal(obj, (Vector2)mpTr.position, dir, gridSize);

        if (!hit)
        {
            if (obj.moveBudget < 0) return;
            float step;
            if (obj.moveBudget < 1) step = obj.moveBudget;
            else                    step = gridSize; 
            Vector2 prevPos = mpTr.position;
            Vector2 targetPos = SnapToGrid((Vector2)mpTr.position + dir * step);
            mpTr.position = new Vector3(targetPos.x, targetPos.y, obj.ZPosition);
            obj.moveBudget -= Vector2.Distance(targetPos, prevPos);
        }
        else
        {
            bool hitIsMovable = hit.collider.TryGetComponent(out IMovable hitMovable);
            PhysicsObjectTest otherObj = hitIsMovable ? hitMovable as PhysicsObjectTest : null;

            if (hitIsMovable && otherObj != null)
                ResolveEntityCollision(obj, otherObj, hit.normal, (Vector2)mpTr.position, dir);
            else
                ResolveStaticCollision(obj, hit.normal, (Vector2)mpTr.position, dir);
        }

        obj.Velocity = new Vector3(obj.tempVelocity.x, obj.tempVelocity.y, obj.Velocity.z);
    }
    private void ResolveStaticCollision(PhysicsObjectTest obj, Vector2 normal, Vector2 origin, Vector2 dir)
    {
        if (obj.IsWalkingDominant)
        {
            obj.tempVelocity = GetModifiedVelocityHorizontal(obj, origin, obj.tempVelocity);
        }
        else
        {
            obj.tempVelocity = Vector2.Reflect(obj.tempVelocity, normal) * obj.bounceCoeff;
            obj.ExForce.AddForce(IMPULSE_FORCE_ID, new Vector3(-obj.tempVelocity.x, -obj.tempVelocity.y, 0f) * obj.Mass);
            //obj.moveBudget = obj.tempVelocity.magnitude * Time.fixedDeltaTime;
        }
    }
    private void ResolveEntityCollision(PhysicsObjectTest obj, PhysicsObjectTest otherObj, Vector2 normal, Vector2 origin, Vector2 dir)
    {
        if (otherObj.resolvedThisStep) return;
        obj.resolvedThisStep = true;

        bool objWalking   = obj.IsWalkingDominant;
        bool otherWalking = otherObj.IsWalkingDominant;

        if (objWalking || otherWalking) 
        {
            if (objWalking)   // obj == walk → obj slide
            {
                obj.tempVelocity = GetModifiedVelocityHorizontal(obj, origin, obj.tempVelocity);
                //obj.moveBudget   = obj.tempVelocity.magnitude * Time.fixedDeltaTime;
            }
            else              // obj == phys, other == walk → obj Impulse
            {
                obj.tempVelocity = Vector2.Reflect(obj.tempVelocity, normal) * obj.bounceCoeff;
                //obj.moveBudget   = obj.tempVelocity.magnitude * Time.fixedDeltaTime;
            }

            if (otherWalking) // other == walk → other slide (use its LastSetDir)
            {
                otherObj.tempVelocity = GetModifiedVelocityHorizontal(otherObj, otherObj.MovePoint.transform.position, otherObj.tempVelocity);
                //otherObj.moveBudget   = otherObj.tempVelocity.magnitude * Time.fixedDeltaTime;
            }
            else              // obj == walk, other == phys → other Impulse
            {
                Vector2 impulseOnOther = obj.Mass * obj.tempVelocity.magnitude * obj.tempVelocity.normalized / otherObj.Mass;
                otherObj.tempVelocity += impulseOnOther * (1f + otherObj.bounceCoeff);
                //otherObj.moveBudget    = otherObj.tempVelocity.magnitude * Time.fixedDeltaTime;
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

            //obj.moveBudget      = obj.tempVelocity.magnitude      * Time.fixedDeltaTime;
            //otherObj.moveBudget = otherObj.tempVelocity.magnitude * Time.fixedDeltaTime;
        }

        if (!objWalking)
            obj.ExForce.AddForce(IMPULSE_FORCE_ID, new Vector3(-obj.tempVelocity.x, -obj.tempVelocity.y, 0f) * obj.Mass);
        if (!otherWalking)
            otherObj.ExForce.AddForce(IMPULSE_FORCE_ID, new Vector3(-otherObj.tempVelocity.x, -otherObj.tempVelocity.y, 0f) * otherObj.Mass);

        otherObj.Velocity = new Vector3(otherObj.tempVelocity.x, otherObj.tempVelocity.y, otherObj.Velocity.z);
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
                result = Random.value > 0.5f ? xTilt : yTilt;
        }
 
        // seek 45d slides 
        if (result == Vector2.zero)
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
        int cnt = Physics2D.OverlapCircleNonAlloc(point, radius, overlapBuffer, obj.WallLayer, obj.contactFilterHorizontal.minDepth, obj.contactFilterHorizontal.maxDepth);
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
        int cnt = Physics2D.Raycast(origin, dir, obj.contactFilterHorizontal, hits, dist);
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