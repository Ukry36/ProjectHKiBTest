using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    private List<PhysicsObjectTest> AllPhysicsEntitys = new();

    public const int GRAVITY_FORCE_ID    = 0;
    public const int GROUND_FORCE_ID     = 1;
    public const int WALL_REACT_FORCE_ID = 2;
    public const int IMPULSE_FORCE_ID = 3;
    
    private Collider2D[] overlapBuffer = new Collider2D[32];
    public float gravity = -9.8f;
    public float gridSize = 1f; 
    public float settleBlendThreshold = 3f;
    public float settleStrength = 8f;
    public float stopThreshold = 0.01f;  
    public const int MAX_PHYS_STEP = 20;

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

            // 4. Blend walk intent to current horizontal obj.velocity
            if (obj.IsWalking)
                obj.tempVelocity = BlendWalkIntent(obj, obj.tempVelocity, obj.WalkingDir);

            // 4.5. Prepare for physics update
            float speed = obj.tempVelocity.magnitude;

            float budgetBlend = Mathf.Clamp01(speed / settleBlendThreshold);
            obj.moveBudget += speed * Time.fixedDeltaTime;
            obj.moveBudget *= budgetBlend;
        }

        for (int i = 0; i < MAX_PHYS_STEP; i++)
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
                if (obj.moveBudget < 0) continue;

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
            AllPhysicsEntitys[i].resolvedThisStep = false;
            AllPhysicsEntitys[i].IsWalking = false;
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
                obj.contactFilter.minDepth - 0.01f, obj.contactFilter.maxDepth);

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

        obj.contactFilter.useDepth  = true;
        obj.contactFilter.minDepth  = obj.ZPosition + obj.verticalCollisionOffset;
        obj.contactFilter.maxDepth  = obj.ZPosition + obj.Height + obj.verticalCollisionOffset;
        obj.contactFilter.layerMask = obj.WallLayer;
    }

    private Vector2 BlendWalkIntent(PhysicsObjectTest obj, Vector2 currentVel, Vector2 walkDir)
    {
        float speed = obj.WalkSpeed * (obj.IsSprinting ? obj.SprintCoeff : 1f) * (1 - Mathf.Clamp01(obj.frictionCoeff * obj.frictionWalkInfluence));
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
    private void UpdateHorizontalPhysics(PhysicsObjectTest obj, Vector2 dir)
    {
        int safetyLoop = MAX_PHYS_STEP;
        obj.moveBudget = obj.tempVelocity.magnitude;

        while (obj.moveBudget > stopThreshold && safetyLoop-- > 0)
        {
            Transform mpTr = obj.MovePoint.transform;
            float stepDist = Mathf.Min(obj.moveBudget, gridSize);
            RaycastHit2D hit = CastWithFilter(obj, (Vector2)mpTr.position, dir, stepDist);

            if (!hit)
            {
                Vector2 targetPos = SnapToGrid((Vector2)mpTr.position + dir * stepDist);
                mpTr.position = new Vector3(targetPos.x, targetPos.y, obj.ZPosition);
                obj.moveBudget -= stepDist;
            }
            else
            {
                IMovable hitMovable = hit.collider.GetComponentInParent<IMovable>();
                if (hitMovable != null)
                    TransferForce(obj, hitMovable, obj.tempVelocity, hit.normal);

                Vector2 reflect = Vector2.Reflect(obj.tempVelocity, hit.normal) * obj.bounceCoeff;
                obj.tempVelocity = reflect;
                dir = obj.tempVelocity.normalized;
                obj.moveBudget = obj.tempVelocity.magnitude;

                obj.ExForce.SetForce(WALL_REACT_FORCE_ID, new Vector3(-obj.tempVelocity.x, -obj.tempVelocity.y, 0f) * obj.Mass);

                dir = GetAvailableDirHorizontal(obj, (Vector2)mpTr.position, dir);
                if (dir == Vector2.zero) break;
            }

            if (obj.moveBudget <= stopThreshold) break;
        }

        obj.Velocity = new Vector3(obj.tempVelocity.x, obj.tempVelocity.y, obj.Velocity.z);
    }
    private void StepHorizontalPhysics1(PhysicsObjectTest obj, Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        Transform mpTr = obj.MovePoint.transform;
        float stepDist = Mathf.Min(obj.moveBudget, gridSize);
        RaycastHit2D hit = CastWithFilter(obj, (Vector2)mpTr.position, dir, stepDist);
        if (!hit)
        {
            Vector2 targetPos = SnapToGrid((Vector2)mpTr.position + dir * stepDist);
            mpTr.position = new Vector3(targetPos.x, targetPos.y, obj.ZPosition);
            obj.moveBudget -= stepDist;
        }
        else
        {
            if (hit.collider.TryGetComponent(out IMovable hitMovable))
                TransferForce(obj, hitMovable, obj.tempVelocity, hit.normal);
            Vector2 reflect;
            if (obj.IsWalkingDominant) reflect = GetAvailableDirHorizontal(obj, (Vector2)mpTr.position, dir) * obj.tempVelocity.magnitude;
            else                       reflect = Vector2.Reflect(obj.tempVelocity, hit.normal) * obj.bounceCoeff;
            
            obj.tempVelocity = reflect;
            obj.moveBudget = obj.tempVelocity.magnitude;
            obj.ExForce.SetForce(WALL_REACT_FORCE_ID, new Vector3(-obj.tempVelocity.x, -obj.tempVelocity.y, 0f) * obj.Mass);
        }
        ////////////////////////////////////////////////

        obj.Velocity = new Vector3(obj.tempVelocity.x, obj.tempVelocity.y, obj.Velocity.z);
    }

    private void StepHorizontalPhysics(PhysicsObjectTest obj, Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        Transform    mpTr = obj.MovePoint.transform;
        RaycastHit2D hit  = CastWithFilter(obj, (Vector2)mpTr.position, dir, gridSize);

        if (!hit)
        {
            Vector2 prevPos = mpTr.position;
            Vector2 targetPos = SnapToGrid((Vector2)mpTr.position + dir * gridSize);
            mpTr.position = new Vector3(targetPos.x, targetPos.y, obj.ZPosition);
            obj.moveBudget -= gridSize * Vector2.Distance(targetPos, prevPos);
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
            Vector2 slideDir = GetAvailableDirHorizontal(obj, origin, dir);
            obj.tempVelocity = slideDir * obj.tempVelocity.magnitude;
        }
        else
        {
            obj.tempVelocity = Vector2.Reflect(obj.tempVelocity, normal) * obj.bounceCoeff;
            obj.ExForce.AddForce(IMPULSE_FORCE_ID, new Vector3(-obj.tempVelocity.x, -obj.tempVelocity.y, 0f) * obj.Mass);
        }

        obj.moveBudget = obj.tempVelocity.magnitude * Time.fixedDeltaTime;
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
                Vector2 slideDir = GetAvailableDirHorizontal(obj, origin, dir);
                obj.tempVelocity = slideDir * obj.tempVelocity.magnitude;
                obj.moveBudget   = obj.tempVelocity.magnitude * Time.fixedDeltaTime;
            }
            else              // obj == phys, other == walk → obj Impulse
            {
                obj.tempVelocity = Vector2.Reflect(obj.tempVelocity, normal) * obj.bounceCoeff;
                obj.moveBudget   = obj.tempVelocity.magnitude * Time.fixedDeltaTime;
            }

            if (otherWalking) // other == walk → other slide (use its LastSetDir)
            {
                Vector2 otherDir      = otherObj.LastSetDir;
                Vector2 otherSlideDir = GetAvailableDirHorizontal(otherObj, otherObj.MovePoint.transform.position, otherDir);
                otherObj.tempVelocity = otherSlideDir * otherObj.tempVelocity.magnitude;
                otherObj.moveBudget   = otherObj.tempVelocity.magnitude * Time.fixedDeltaTime;
            }
            else              // obj == walk, other == phys → other Impulse
            {
                Vector2 impulseOnOther = obj.Mass * obj.tempVelocity.magnitude * obj.tempVelocity.normalized / otherObj.Mass;
                otherObj.tempVelocity += impulseOnOther * (1f + otherObj.bounceCoeff);
                otherObj.moveBudget    = otherObj.tempVelocity.magnitude * Time.fixedDeltaTime;
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

            obj.moveBudget      = obj.tempVelocity.magnitude      * Time.fixedDeltaTime;
            otherObj.moveBudget = otherObj.tempVelocity.magnitude * Time.fixedDeltaTime;
        }

        if (!objWalking)
            obj.ExForce.AddForce(IMPULSE_FORCE_ID, new Vector3(-obj.tempVelocity.x, -obj.tempVelocity.y, 0f) * obj.Mass);
        if (!otherWalking)
            otherObj.ExForce.AddForce(IMPULSE_FORCE_ID, new Vector3(-otherObj.tempVelocity.x, -otherObj.tempVelocity.y, 0f) * otherObj.Mass);

        otherObj.Velocity = new Vector3(otherObj.tempVelocity.x, otherObj.tempVelocity.y, otherObj.Velocity.z);
    }
    private Vector3 GetAvailableDirHorizontal(PhysicsObjectTest obj, Vector2 origin, Vector2 dir)
    {
        if (dir == Vector2.zero) return Vector2.zero;
 
        // check if entity can simply go through
        if (!OverlapCheckHorizontal(obj, origin + dir.normalized * gridSize, 0.2f))
        {
            //obj.LastSetDir = dir;
            return dir;
        }

        // else, there is something
        Vector2 moveDir = dir;
        Vector2 xTilt   = new(Mathf.Sign(dir.x), 0f);
        Vector2 yTilt   = new(0f, Mathf.Sign(dir.y));
 
        bool xWalled = dir.x != 0f && OverlapCheckHorizontal(obj, origin + xTilt, 0.4f);
        bool yWalled = dir.y != 0f && OverlapCheckHorizontal(obj, origin + yTilt, 0.4f);
 
        if (xWalled) moveDir.x = 0f;
        if (yWalled) moveDir.y = 0f;
 
        // if there is no wall at x, y dir, it might mean there is only diagonal wall
        // if so, randomly choose where to go
        if (!xWalled && !yWalled)
        {
            if (OverlapCheckHorizontal(obj, origin + xTilt + yTilt, 0.4f))
                moveDir = Random.value > 0.5f ? xTilt : yTilt;
        }
 
        // seek 45d slides 
        if (moveDir == Vector2.zero)
        {
            Vector2 slideUp   = Quaternion.Euler(0, 0,  45) * dir;
            Vector2 slideDown = Quaternion.Euler(0, 0, -45) * dir;
 
            bool canUp   = !OverlapCheckHorizontal(obj, origin + slideUp   * 0.5f, 0.2f);
            bool canDown = !OverlapCheckHorizontal(obj, origin + slideDown  * 0.5f, 0.2f);
 
            if (canUp && canDown)
            {
                moveDir = Random.value > 0.5f ? slideUp : slideDown;
                return moveDir;
            }
            if (canUp)   return slideUp;
            if (canDown) return slideDown;
 
            return Vector2.zero; // completely stops
        }
 
        return moveDir;
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
    private void TransferForce(PhysicsObjectTest obj, IMovable target, Vector2 vel, Vector2 normal)
    {
        target.ExForce.AddForce(IMPULSE_FORCE_ID, (Vector3)vel * obj.Mass);
    }
    private bool OverlapCheckHorizontal(PhysicsObjectTest obj, Vector2 point, float radius)
    {
        int cnt = Physics2D.OverlapCircleNonAlloc(point, radius, overlapBuffer, obj.WallLayer, obj.contactFilter.minDepth, obj.contactFilter.maxDepth);
        for (int i = 0; i < cnt; i++)
        {
            Transform t = overlapBuffer[i].transform;
            if (t == obj.entityTransform) continue;
            if (t == obj.MovePoint.transform) continue;
            return true;
        }
        return false;
    }
    private RaycastHit2D CastWithFilter(PhysicsObjectTest obj, Vector2 origin, Vector2 dir, float dist)
    {
        RaycastHit2D[] hits = new RaycastHit2D[8];
        int cnt = Physics2D.Raycast(origin, dir, obj.contactFilter, hits, dist);
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