using System.Collections.Generic;
using UnityEngine;

// ──────────────────────────────────────────────
//  Data Structure
// ──────────────────────────────────────────────

public enum MovementMode { Grid, Physics }

[System.Serializable]
public class GridState
{
    public Vector2Int CurrentCell;
    public Vector2Int TargetCell;
    public float      CellProgress;
    public Vector2Int LastMoveDir;
    public bool       IsSettling;
    public Vector2    PhysicsReturnOffset;
}

[System.Serializable]
public class PhysicsState
{
    public int KeepPhysics;
}

public class PhysicsManager2 : MonoBehaviour
{
    private List<PhysicsObjectTest> AllPhysicsEntitys = new();
    private readonly Dictionary<Vector2Int, List<PhysicsObjectTest>> cellOccupancy = new();

    public const float EPSILON = 0.00001f;

    public float gravity           = -9.8f;
    public float gridSize          = 1f;
    public float stopThreshold     = 0.05f;
    public bool enable;
    public float gridSettleSpeed   = 3f;
    public float bounceTolerance   = 0.1f;
    public int keepCanWalkFrames   = 4;
    public int keepPhysicsFrames   = 4;
    public float snapDecaySpeed    = 12f;
    public float renderDecaySpeed  = 12f;
    public bool interpolateRender  = true;
    public int physicsIterations   = 4;
    public float stabilization     = 1f;

    private readonly Collider2D[] overlapBuffer = new Collider2D[32];
    private readonly Vector2Int[] cellBuffer = new Vector2Int[32];
    private readonly PhysicsObjectTest[] objBuffer = new PhysicsObjectTest[32];
    private readonly RaycastHit2D[] staticCastBuffer = new RaycastHit2D[32];

#region PUBLIC API
    public void AddPhysicsObject(PhysicsObjectTest obj)
    {
        AllPhysicsEntitys.Add(obj);
        obj.Grid.CurrentCell  = WorldCenterToAnchorCell(obj.transform.position, obj.Size);
        obj.Grid.TargetCell   = obj.Grid.CurrentCell;
        obj.Grid.CellProgress = 1f;
    }
#endregion

#region MAIN PIPELINE
    private void FixedUpdate()
    {
        if (!enable) return;

        AllPhysicsEntitys = AllPhysicsEntitys.ShuffleList();

        for (int i = 0; i < AllPhysicsEntitys.Count; i++)
            UpdateVelocity(AllPhysicsEntitys[i]);
        
        for (int i = 0; i < AllPhysicsEntitys.Count; i++)
            UpdateVerticalPhysics(AllPhysicsEntitys[i]);

        for (int i = 0; i < AllPhysicsEntitys.Count; i++)
            UpdateMode(AllPhysicsEntitys[i]);

        RebuildCellOccupancy();
    
        for (int i = 0; i < AllPhysicsEntitys.Count; i++)
        {
            if      (AllPhysicsEntitys[i].Mode == MovementMode.Grid)    UpdateGridMovement(AllPhysicsEntitys[i]);
            else if (AllPhysicsEntitys[i].Mode == MovementMode.Physics) UpdatePhysicsMovementOnlyWall(AllPhysicsEntitys[i]); 
        }

        for (int i = 0; i < physicsIterations; i++)
        {
            for (int j = 0; j < AllPhysicsEntitys.Count; j++)
                ResolveEntityCollision(AllPhysicsEntitys[j]);
        }

        for (int i = 0; i < AllPhysicsEntitys.Count; i++)
        {
            PhysicsObjectTest obj = AllPhysicsEntitys[i];
            obj.ExForce       = Vector3.zero;
            obj.PrevEntityPos = obj.transform.position;
            obj.LastSetDir    = obj.IsWalking ? (Vector3)obj.WalkingDir : (Vector3)obj.Velocity.normalized;
        }
    }

    private void UpdateVelocity(PhysicsObjectTest obj)
    {
        obj.ExForce += Vector3.forward * gravity;
        obj.ZVelocity += obj.ExForce.z * obj.InvM * Time.fixedDeltaTime;
        obj.Velocity += obj.InvM * Time.fixedDeltaTime * (Vector2)obj.ExForce;

        obj.Velocity = ApplyGroundFriction(obj, obj.Velocity);

        bool isActivelyWalking = obj.IsWalking && obj.WalkingDir.sqrMagnitude > EPSILON;
        if (isActivelyWalking)
        {
            float maxSpd = (obj.IsSprinting ? obj.WalkSpeed * obj.SprintCoeff : obj.WalkSpeed)
                         * (obj.CanWalkFrameLeft > 0 ? 1f : 0.1f);
            Vector2 walkDir            = obj.WalkingDir.normalized;
            float frictionAccInfluence = obj.Ground ? 1 - Mathf.Clamp01(obj.frictionCoeff * obj.Ground.frictionCoeff * obj.frictionWalkInfluence): 1f;
            float WalkAcceleration     = obj.IsSprinting ? obj.WalkAcceleration * obj.SprintCoeff : obj.WalkAcceleration;
            float currentAlongWalk = Vector2.Dot(obj.Velocity, walkDir);
            float deficit          = maxSpd - currentAlongWalk;

            if (deficit > 0f)
            {
                float accel = WalkAcceleration * frictionAccInfluence * Time.fixedDeltaTime;
                obj.Velocity += walkDir * Mathf.Min(accel, deficit);
            }
        }
        else if (obj.Mode == MovementMode.Grid)
        {
            if (obj.Velocity.magnitude < gridSettleSpeed)
            {
                obj.Velocity = Vector2.zero;
                StartGridSettle(obj);
                return;
            }
        }
    }
#endregion

#region MODE SWITCH
    private void UpdateMode(PhysicsObjectTest obj)
    {
        bool wantsGrid    = obj.Ground && !obj.IsOnSlope
                         && obj.Velocity.magnitude < obj.GridEndureSpeed 
                         && obj.ExForce.magnitude < obj.GridEndureForce;
        bool wantsPhysics = !wantsGrid;

        if (obj.Mode == MovementMode.Physics && obj.Phys.KeepPhysics > 0)
        {
            obj.Phys.KeepPhysics--;
            return;
        }

        if (obj.Mode == MovementMode.Physics && wantsGrid)
        {
            obj.Mode = MovementMode.Grid;

            obj.Grid.CurrentCell  = WorldCenterToAnchorCell(obj.transform.position, obj.Size);
            obj.Grid.TargetCell   = obj.Grid.CurrentCell;
            obj.Grid.CellProgress = 1f;
            obj.Grid.IsSettling   = false;

            if (obj.Velocity.sqrMagnitude > EPSILON)
            {
                Vector2 d = obj.Velocity.normalized;
                obj.Grid.LastMoveDir = new Vector2Int(
                    Mathf.RoundToInt(d.x), Mathf.RoundToInt(d.y));
            }

            Vector2 gridCenter = AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size);
            obj.Grid.PhysicsReturnOffset = (Vector2)obj.transform.position - gridCenter;
        }
        else if (obj.Mode == MovementMode.Grid && wantsPhysics)
        {
            obj.Mode             = MovementMode.Physics;
            obj.Phys.KeepPhysics = keepPhysicsFrames;
        }
    }

    private void SwitchGridToPhysics(PhysicsObjectTest obj)
    {
        if (obj.Mode != MovementMode.Grid) return;

        RemoveFootprintOccupant(obj, obj.Grid.CurrentCell);
        RemoveFootprintOccupant(obj, obj.Grid.TargetCell);

        obj.Mode             = MovementMode.Physics;
        obj.Phys.KeepPhysics = keepPhysicsFrames;

        Vector2Int anchor = WorldCenterToAnchorCell(obj.transform.position, obj.Size);
        AddFootprintOccupant(obj, anchor);
    }
#endregion

#region MOVEMENT
    private void UpdateGridMovement(PhysicsObjectTest obj)
    {
        if (obj.Grid.IsSettling)
        {
            bool hasInput = obj.IsWalking && obj.WalkingDir.sqrMagnitude > EPSILON;
            if (!hasInput) { UpdateGridSettle(obj); return; }
            obj.Grid.IsSettling = false;
        }

        if (obj.Grid.PhysicsReturnOffset.sqrMagnitude > EPSILON) // Physics snap offset decay logic
        {
            obj.Grid.PhysicsReturnOffset = Vector2.Lerp(obj.Grid.PhysicsReturnOffset, Vector2.zero, Time.fixedDeltaTime * snapDecaySpeed);

            if (obj.Grid.PhysicsReturnOffset.sqrMagnitude < EPSILON)
                obj.Grid.PhysicsReturnOffset = Vector2.zero;
        }

        float currentSpeed = obj.Velocity.magnitude;
        if (currentSpeed > stopThreshold)
        {
            Vector2    normalizedVel = obj.Velocity.normalized;
            Vector2Int moveDir       = new(Mathf.RoundToInt(normalizedVel.x),
                                           Mathf.RoundToInt(normalizedVel.y));
            if (moveDir == Vector2Int.zero) moveDir = obj.Grid.LastMoveDir;

            if (obj.Grid.CellProgress >= 1f)
            {
                obj.Grid.CurrentCell  = obj.Grid.TargetCell;
                obj.Grid.CellProgress = 1f;

                obj.Grid.LastMoveDir = moveDir;
                Vector2Int desiredAnchor = obj.Grid.CurrentCell + moveDir;
                TryMoveToCellGrid(obj, desiredAnchor, moveDir);
                //if (obj.Mode != MovementMode.Grid) return;
            }

            if (obj.Grid.CurrentCell != obj.Grid.TargetCell)
            {
                float cellDistance = Vector2.Distance(AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size),
                                                      AnchorCellToWorldCenter(obj.Grid.TargetCell,  obj.Size));
                obj.Grid.CellProgress += currentSpeed * Time.fixedDeltaTime / cellDistance;
                obj.Grid.CellProgress  = Mathf.Clamp01(obj.Grid.CellProgress);
            }
        }
        else obj.Velocity = Vector2.zero;
        Vector2 worldCur    = AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size);
        Vector2 worldTarget = AnchorCellToWorldCenter(obj.Grid.TargetCell,  obj.Size);
        Vector2 newPos      = Vector2.Lerp(worldCur, worldTarget, obj.Grid.CellProgress);

        newPos += obj.Grid.PhysicsReturnOffset; // Add the physics offset!

        obj.transform.position = new Vector3(newPos.x, newPos.y, obj.ZPosition);
    }

    private void StartGridSettle(PhysicsObjectTest obj)
    {
        float cellDistance = Vector2.Distance(
            AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size),
            AnchorCellToWorldCenter(obj.Grid.TargetCell,  obj.Size));
        if (cellDistance < EPSILON) cellDistance = gridSize;

        obj.Grid.CellProgress += gridSettleSpeed * Time.fixedDeltaTime / cellDistance;

        if (obj.Grid.CellProgress >= 1f)
        {
            obj.Grid.CurrentCell  = obj.Grid.TargetCell;
            obj.Grid.CellProgress = 1f;
            obj.Grid.IsSettling   = false;
            SnapPositionToCell(obj, obj.Grid.TargetCell);
            return;
        }

        if (obj.Grid.CellProgress < 0.2f)
        {
            (obj.Grid.CurrentCell, obj.Grid.TargetCell) = (obj.Grid.TargetCell, obj.Grid.CurrentCell);
            obj.Grid.CellProgress = 1f - obj.Grid.CellProgress;
        }

        obj.Grid.IsSettling = true;
    }

    private void UpdateGridSettle(PhysicsObjectTest obj)
    {
        if (obj.Grid.PhysicsReturnOffset.sqrMagnitude > EPSILON) // Physics offset decay
        {
            obj.Grid.PhysicsReturnOffset = Vector2.Lerp(obj.Grid.PhysicsReturnOffset, Vector2.zero, Time.fixedDeltaTime * snapDecaySpeed);
            if (obj.Grid.PhysicsReturnOffset.sqrMagnitude < EPSILON)
                obj.Grid.PhysicsReturnOffset = Vector2.zero;
        }

        obj.Grid.CellProgress += gridSettleSpeed * Time.fixedDeltaTime / gridSize;

        if (obj.Grid.CellProgress >= 1f)
        {
            obj.Grid.CellProgress = 1f;
            obj.Grid.CurrentCell  = obj.Grid.TargetCell;
            obj.Grid.IsSettling   = false;

            Vector2 targetCenter = AnchorCellToWorldCenter(obj.Grid.TargetCell, obj.Size);
            targetCenter += obj.Grid.PhysicsReturnOffset; // Add physics offset same as normal movement
            obj.transform.position = new Vector3(targetCenter.x, targetCenter.y, obj.ZPosition);
            return;
        }

        Vector2 from   = AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size);
        Vector2 to     = AnchorCellToWorldCenter(obj.Grid.TargetCell,  obj.Size);
        Vector2 newPos = Vector2.Lerp(from, to, obj.Grid.CellProgress);

        newPos += obj.Grid.PhysicsReturnOffset; // Add physics offset same as normal movement
        obj.transform.position = new Vector3(newPos.x, newPos.y, obj.ZPosition);
    }

    private void TryMoveToCellGrid(PhysicsObjectTest obj, Vector2Int desiredAnchor, Vector2Int moveDir)
    {
        bool staticWall = IsStaticWall(obj, desiredAnchor); // Static wall check
        if (staticWall)
        {
            bool slid = TrySlideGrid(obj, moveDir);
            if (!slid) obj.Velocity = Vector2.zero;
            return;
        }
        float radius = 0.25f * (obj.Size.x + obj.Size.y) * 0.8f;
        bool diagonal = OverlapCheckHorizontal(obj, obj.Grid.CurrentCell + ((Vector2)moveDir).normalized * radius, radius);
        if (diagonal)
        {
            obj.Velocity = Vector2.zero;
            return;
        } 

        int cnt = GetAnyOccupantInFootprint(obj, desiredAnchor, objBuffer); // Entity check
        if (cnt > 0)
        {
            SwitchGridToPhysics(obj);
            for (int i = 0; i < cnt; i++)
                if (objBuffer[i].Mode == MovementMode.Grid)
                    SwitchGridToPhysics(objBuffer[i]);
            return;
        }

        obj.Grid.TargetCell   = desiredAnchor; // Front is empty!
        obj.Grid.CellProgress = 0f;
        AddFootprintOccupant(obj, desiredAnchor);
    }

    private bool TrySlideGrid(PhysicsObjectTest obj, Vector2Int moveDir)
    {
        if (moveDir.x != 0)
        {
            var slideDir    = new Vector2Int(moveDir.x, 0);
            var slideAnchor = obj.Grid.CurrentCell + slideDir;
            if (!IsStaticWall(obj, slideAnchor) && GetAnyOccupantInFootprint(obj, slideAnchor, objBuffer) < 1)
            {
                obj.Grid.TargetCell   = slideAnchor;
                obj.Grid.CellProgress = 0f;
                obj.Grid.LastMoveDir  = slideDir;
                AddFootprintOccupant(obj, slideAnchor);
                return true;
            }
        }
        if (moveDir.y != 0)
        {
            var slideDir    = new Vector2Int(0, moveDir.y);
            var slideAnchor = obj.Grid.CurrentCell + slideDir;
            if (!IsStaticWall(obj, slideAnchor) && GetAnyOccupantInFootprint(obj, slideAnchor, objBuffer) < 1)
            {
                obj.Grid.TargetCell   = slideAnchor;
                obj.Grid.CellProgress = 0f;
                obj.Grid.LastMoveDir  = slideDir;
                AddFootprintOccupant(obj, slideAnchor);
                return true;
            }
        }

        obj.Grid.TargetCell   = obj.Grid.CurrentCell;
        obj.Grid.CellProgress = 1f;
        return false;
    }

    private void UpdatePhysicsMovementOnlyWall(PhysicsObjectTest obj)
    {
        if (obj.Velocity.magnitude < stopThreshold) { obj.Velocity = Vector2.zero; return; }
        float totalDist = obj.Velocity.magnitude * Time.fixedDeltaTime;
        int   steps = Mathf.Max(1, Mathf.CeilToInt(totalDist / gridSize));
        float dt    = Time.fixedDeltaTime / steps;

        bool collided = false;
        for (int s = 0; s < steps; s++)
        {
            Vector2 delta = obj.Velocity * dt;

            Vector2 origin       = obj.transform.position;
            Vector2 nextPosition = origin + delta;

            if (TryResolveStaticCellCollision(obj, nextPosition, delta.normalized)) // Check static wall collision before entity collision!
            {
                totalDist = obj.Velocity.magnitude * Time.fixedDeltaTime;
                steps     = Mathf.Max(1, Mathf.CeilToInt(totalDist / gridSize));
                dt        = Time.fixedDeltaTime / steps;
                delta     = obj.Velocity * dt;
                collided  = true;
            }

            Vector2Int previousAnchor = WorldCenterToAnchorCell(origin, obj.Size);
            obj.transform.position += (Vector3)delta;
            UpdatePhysicsCellOccupancy(obj, previousAnchor);

            if (collided) break;
        }
    }
#endregion

#region STATIC COLLISION
    private bool TryResolveStaticCellCollision(PhysicsObjectTest obj, Vector2 nextPosition, Vector2 fallbackDir)
    {
        Vector2 origin = obj.transform.position;
        Vector2 delta = nextPosition - origin;
        float distance = delta.magnitude;
        if (distance < EPSILON) return false;
        float radius = 0.25f * (obj.Size.x + obj.Size.y) * 0.8f;

        int hitCount = ZPhysics2D.CircleCastNonAlloc(origin, radius, delta.normalized, staticCastBuffer, distance, obj.WallLayer, 
                                                  obj.zCollider.ZMin + obj.stepUpTolerance + EPSILON, obj.zCollider.ZMax);

        ZCollider2D validHit = null;
        Vector2 normal = Vector2.zero;
        float minHitDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = staticCastBuffer[i];
            if (hit.collider.transform == obj.transform) continue;
            if (hit.distance < minHitDistance)
            {
                minHitDistance = hit.distance;
                normal = hit.normal;
                ZPhysics2D.TryGet(hit.collider, out validHit);
            }
        }

        if (validHit)
        {
            if (normal.sqrMagnitude < EPSILON) normal = -fallbackDir;
            ResolveStaticCollision(obj, validHit, normal.normalized);
            return true;
        }

        return false;
    }

    private void ResolveStaticCollision(PhysicsObjectTest obj, ZCollider2D wall, Vector2 normal)
    {
        float vDotN = Vector2.Dot(obj.Velocity, normal);
        if (vDotN >= 0f) return;

        obj.Velocity -= (1f + wall.bounceCoeff * obj.bounceCoeff) * vDotN * normal;

        if (obj.Velocity.magnitude < stopThreshold)
            obj.Velocity = Vector2.zero;
    }
#endregion

#region DYNAMIC COLLISION
    private void ResolveEntityCollision(PhysicsObjectTest obj)
    {
        Vector2Int currentAnchor = WorldCenterToAnchorCell(obj.transform.position, obj.Size);
        
        for (int x = -1; x <= obj.Size.x; x++) for (int y = -1; y <= obj.Size.y; y++) // Broadphase: Check cells nearby
        {
            Vector2Int queryCell = currentAnchor + new Vector2Int(x, y);
            if (!cellOccupancy.TryGetValue(queryCell, out var occupants)) continue;
            
            for (int o = 0; o < occupants.Count; o++)
            {
                var occupant = occupants[o];
                if (occupant == null || occupant == obj) continue;
                
                if (obj.GetInstanceID() > occupant.GetInstanceID()) continue; // Prevent double collision 
    
                if (obj.zCollider.ZMin + obj.stepUpTolerance < occupant.zCollider.ZMax &&
                    obj.zCollider.ZMax                   > occupant.zCollider.ZMin) // Narrowphase: AABB overlap check
                {
                    Vector2 posA = obj.transform.position;
                    Vector2 posB = occupant.transform.position;
                    Vector2 extentsA = 0.5f * gridSize * (Vector2)obj.Size;
                    Vector2 extentsB = 0.5f * gridSize * (Vector2)occupant.Size;
                    float tolerance = 0.01f;
    
                    bool isOverlappingX = Mathf.Abs(posA.x - posB.x) < extentsA.x + extentsB.x - tolerance;
                    bool isOverlappingY = Mathf.Abs(posA.y - posB.y) < extentsA.y + extentsB.y - tolerance;
    
                    if (isOverlappingX && isOverlappingY)
                    {
                        Vector2 normal = (Vector2)(obj.transform.position - occupant.transform.position);
                        if (normal.sqrMagnitude < EPSILON) normal = Vector2.up;
                        
                        CalculateEntityCollision(obj, occupant, normal.normalized);
                    }
                }
            }
        }
    }

    private void CalculateEntityCollision(PhysicsObjectTest objA, PhysicsObjectTest objB, Vector2 normal)
    {
        Vector2 vA = objA.Velocity;
        Vector2 vB = objB.Velocity;
        float invMA = objA.InvM;
        float invMB = objB.InvM;
    
        Vector2 sep     = (Vector2)(objA.transform.position - objB.transform.position);
        float   dist    = sep.magnitude;
        Vector2 pushDir = dist > EPSILON ? sep / dist : normal;
    
        ////////// If there is Static wall behind, act as wall //////////
        Vector2Int anchorA = WorldCenterToAnchorCell(objA.transform.position, objA.Size);
        Vector2Int anchorB = WorldCenterToAnchorCell(objB.transform.position, objB.Size);
    
        Vector2Int stepA = DirectionToCellStep(pushDir);   // A offset
        Vector2Int stepB = DirectionToCellStep(-pushDir);  // B offset
    
        if (invMA > 0f && IsStaticWall(objA, anchorA + stepA)) invMA = 0f;
        if (invMB > 0f && IsStaticWall(objB, anchorB + stepB)) invMB = 0f;
        /////////////////////////////////////////////////////////////////
    
        float totalInvMass = invMA + invMB;
        if (totalInvMass < EPSILON) return;
    
        float allowedDist = Mathf.Abs(pushDir.x) * (objA.Size.x + objB.Size.x) * 0.5f * gridSize +
                            Mathf.Abs(pushDir.y) * (objA.Size.y + objB.Size.y) * 0.5f * gridSize;

        float vRelative   = Vector2.Dot(vA - vB, normal);
        float penetration = allowedDist - dist;

        const float slop = 0.001f;         // Penetration under this doesn't solve
        const float velocitySlop = 0.05f; // Velocity under this doesn't bounce

        bool needImpulse    = vRelative < -velocitySlop;
        bool needCorrection = penetration > slop; 

        if (objA.Velocity.magnitude < stopThreshold && objB.Velocity.magnitude < stopThreshold) return;
        if (!needImpulse && !needCorrection) return;

        objA.Phys.KeepPhysics = keepPhysicsFrames;
        objB.Phys.KeepPhysics = keepPhysicsFrames;
        if (objA.Mode == MovementMode.Grid) SwitchGridToPhysics(objA);
        if (objB.Mode == MovementMode.Grid) SwitchGridToPhysics(objB);

        if (needImpulse)
        {
            float e         = objA.bounceCoeff * objB.bounceCoeff;
            float j         = -(1f + e) * vRelative / totalInvMass;
            Vector2 impulse = j * normal;

            objA.Velocity = vA + impulse * invMA;
            objB.Velocity = vB - impulse * invMB;
        }

        if (needCorrection)
        {
            float correctAmount = (penetration - slop) * stabilization / totalInvMass;
            Vector2 correction  = pushDir * correctAmount;

            Vector2Int prevCellA = WorldCenterToAnchorCell(objA.transform.position, objA.Size);
            Vector2Int prevCellB = WorldCenterToAnchorCell(objB.transform.position, objB.Size);

            if (interpolateRender)
            {
                objA.SetBodyPartSnapOffset((Vector2)objA.transform.position + correction * invMA);
                objB.SetBodyPartSnapOffset((Vector2)objB.transform.position - correction * invMB);
            }

            objA.transform.position += (Vector3)(correction * invMA);
            objB.transform.position -= (Vector3)(correction * invMB);

            UpdatePhysicsCellOccupancy(objA, prevCellA);
            UpdatePhysicsCellOccupancy(objB, prevCellB);
        }
    }
#endregion

#region VERTICAL PHYS
    private void UpdateVerticalPhysics(PhysicsObjectTest obj)
    {
        Vector2 checkSize = (Vector2)obj.Size - new Vector2(0.2f, 0.2f);

        ZCollider2D floor   = ZPhysics2D.ZBoxGetFloor(
            obj.transform.position, checkSize, 0, obj.floorLayer,
            obj.zCollider.ZMin - obj.stepDownTolerance - EPSILON,
            obj.zCollider.ZMin + obj.stepUpTolerance   + EPSILON);
        ZCollider2D ceiling = ZPhysics2D.ZBoxGetCeiling(
            obj.transform.position, checkSize, 0, obj.floorLayer,
            obj.zCollider.ZMax + obj.stepDownTolerance + EPSILON,
            obj.zCollider.ZMax - obj.stepUpTolerance   - EPSILON);

        Vector3 surfaceNormal = floor ? floor.GetSurfaceNormal() : Vector3.forward;
        Vector2 horizVel  = obj.Mode == MovementMode.Grid ? obj.Velocity : obj.Velocity;
        Vector3 vel       = new(horizVel.x, horizVel.y, obj.ZVelocity);
        bool towardsFloor = Vector3.Dot(surfaceNormal, vel) < EPSILON;

        obj.Ground = floor && floor.ZmaxBox(obj.transform.position, checkSize, 0) > obj.zCollider.ZMin - EPSILON && towardsFloor ? 
                     floor : null;
        obj.IsOnSlope  = Vector3.Dot(surfaceNormal, Vector3.forward) < 1f - EPSILON;

        bool calcGround = false;
        if (obj.Ground)
        {
            if (!obj.IsGroundedPrev && towardsFloor)
            {
                Vector3 reflected   = vel - (1f + obj.bounceCoeff * floor.bounceCoeff) * Vector3.Dot(vel, surfaceNormal) * surfaceNormal;
                obj.ZVelocity       = reflected.z;
                Vector2 newHorizVel = new(reflected.x, reflected.y);
                if (obj.Mode == MovementMode.Grid) obj.Velocity = newHorizVel;
                else                               obj.Velocity = newHorizVel;

                float verAcc       = Mathf.Abs(obj.ExForce.z * obj.InvM);
                float minEscapeVel = verAcc * bounceTolerance;
                if (obj.ZVelocity < minEscapeVel) obj.ZVelocity = 0f;
            }
            else calcGround = true;
        }
        else if (obj.IsGroundedPrev && floor && towardsFloor)
        {
            obj.Ground = floor;
            calcGround = true;
        }

        if (calcGround)
        {
            obj.ZPosition = floor.ZmaxBox(obj.transform.position, checkSize, 0);
            if (obj.IsOnSlope)
            {
                Vector3 slopeVel = vel - Vector3.Dot(vel, surfaceNormal) * surfaceNormal;
                if (obj.Mode == MovementMode.Grid) obj.Velocity = (Vector2)slopeVel;
                else                               obj.Velocity = (Vector2)slopeVel;
                obj.ZVelocity = slopeVel.z;
            }
            else obj.ZVelocity = 0;
            obj.ExForce = new(obj.ExForce.x, obj.ExForce.y, 0);
        }

        obj.ZPosition += obj.ZVelocity * Time.fixedDeltaTime;

        if (ceiling != null && obj.ZVelocity > EPSILON)
        {
            float ceilBottom = ceiling.Zmin(obj.transform.position);
            if (obj.ZPosition >= ceilBottom)
            {
                obj.ZPosition = ceilBottom;
                Vector3 ceilNormal = ceiling.GetSurfaceNormal();
                float vDotN = Vector3.Dot(vel, ceilNormal);
                if (vDotN < 0f)
                {
                    Vector3 reflected   = vel - (1f + obj.bounceCoeff * ceiling.bounceCoeff) * vDotN * ceilNormal;
                    obj.ZVelocity       = reflected.z;
                    Vector2 newHorizVel = new(reflected.x, reflected.y);
                    if (obj.Mode == MovementMode.Grid) obj.Velocity = newHorizVel;
                    else                               obj.Velocity = newHorizVel;
                }
                else
                {
                    obj.ZVelocity = -Mathf.Abs(obj.ZVelocity) * obj.bounceCoeff * ceiling.bounceCoeff;
                }
                if (Mathf.Abs(obj.ZVelocity) < stopThreshold) obj.ZVelocity = 0f;
            }
        }

        if (obj.Ground) obj.CanWalkFrameLeft = keepCanWalkFrames;
        else if (obj.CanWalkFrameLeft > 0) obj.CanWalkFrameLeft--;

        obj.IsGroundedPrev     = obj.Ground;
        Vector3 ep             = obj.transform.position;
        obj.transform.position = new Vector3(ep.x, ep.y, obj.ZPosition);
    }

    private Vector2 ApplyGroundFriction(PhysicsObjectTest obj, Vector2 vel)
    {
        float friction = obj.Ground ? obj.frictionCoeff * obj.Ground.frictionCoeff : obj.airFriction;
        vel *= friction;

        float iceBlend      = Mathf.Clamp01((friction - 0.5f) / 0.5f);
        float effectiveStop = stopThreshold * (1f - iceBlend);

        if (vel.magnitude < effectiveStop) vel = Vector2.zero;
        return vel;
    }
#endregion

#region CELL HELPERS
    private void SnapPositionToCell(PhysicsObjectTest obj, Vector2Int anchorCell)
    {
        Vector2 worldCenter    = AnchorCellToWorldCenter(anchorCell, obj.Size);
        obj.transform.position = new Vector3(worldCenter.x, worldCenter.y, obj.ZPosition);
    }
    private void RebuildCellOccupancy()
    {
        cellOccupancy.Clear();
        for (int i = 0; i < AllPhysicsEntitys.Count; i++)
        {
            PhysicsObjectTest obj = AllPhysicsEntitys[i];
            if (obj.Mode == MovementMode.Grid)
            {
                int cnt = GetOccupiedCells(obj.Grid.CurrentCell, obj.Size, cellBuffer); //might be better remove
                for (int j = 0; j < cnt; j++)
                    AddCellOccupant(cellBuffer[j], obj);
                cnt = GetOccupiedCells(obj.Grid.TargetCell, obj.Size, cellBuffer);
                for (int j = 0; j < cnt; j++)
                    AddCellOccupant(cellBuffer[j], obj);
            }
            else
            {
                Vector2Int anchor = WorldCenterToAnchorCell(obj.transform.position, obj.Size);
                int cnt = GetOccupiedCells(anchor, obj.Size, cellBuffer);
                for (int j = 0; j < cnt; j++)
                    AddCellOccupant(cellBuffer[j], obj);
            }
        }
    }

    private PhysicsObjectTest GetCellOccupant(Vector2Int cell, PhysicsObjectTest self)
    {
        if (!cellOccupancy.TryGetValue(cell, out var occupants)) return null;
        for (int i = 0; i < occupants.Count; i++)
        {
            PhysicsObjectTest occupant = occupants[i];
            if (occupant != null && occupant != self
            && self.zCollider.ZMin + self.stepUpTolerance + EPSILON < occupant.zCollider.ZMax
            && self.zCollider.ZMax                                  > occupant.zCollider.ZMin)
                return occupant;
        }
        return null;
    }

    private int GetAnyOccupantInFootprint(PhysicsObjectTest obj, Vector2Int anchorCell, PhysicsObjectTest[] results)
    {
        HashSet<PhysicsObjectTest> check = new(32);
        int validCnt = 0;
        int cnt = GetOccupiedCells(anchorCell, obj.Size, cellBuffer);
        for (int i = 0; i < cnt; i++)
        {
            var occ = GetCellOccupant(cellBuffer[i], obj);
            if (occ != null && check.Add(occ)) results[validCnt++] = occ;
        }
        return validCnt;
    }

    private void AddCellOccupant(Vector2Int cell, PhysicsObjectTest obj)
    {
        if (!cellOccupancy.TryGetValue(cell, out var occupants))
        {
            occupants = new List<PhysicsObjectTest>();
            cellOccupancy[cell] = occupants;
        }
        if (!occupants.Contains(obj))
            occupants.Add(obj);
    }

    private void RemoveCellOccupant(Vector2Int cell, PhysicsObjectTest obj)
    {
        if (!cellOccupancy.TryGetValue(cell, out var occupants)) return;
        occupants.Remove(obj);
        if (occupants.Count == 0) cellOccupancy.Remove(cell);
    }

    private void RemoveFootprintOccupant(PhysicsObjectTest obj, Vector2Int anchorCell)
    {
        int cnt = GetOccupiedCells(anchorCell, obj.Size, cellBuffer);
        for (int i = 0; i < cnt; i++)
            RemoveCellOccupant(cellBuffer[i], obj);
    }

    private void AddFootprintOccupant(PhysicsObjectTest obj, Vector2Int anchorCell)
    {
        int cnt = GetOccupiedCells(anchorCell, obj.Size, cellBuffer);
        for (int i = 0; i < cnt; i++)
            AddCellOccupant(cellBuffer[i], obj);
    }

    private bool IsStaticWall(PhysicsObjectTest obj, Vector2Int anchorCell)
    {
        int cnt = GetOccupiedCells(anchorCell, obj.Size, cellBuffer);
        for (int i = 0; i < cnt; i++)
        {
            if (OverlapCheckHorizontal(obj, CellToWorld(cellBuffer[i]), 0.4f))
                return true;
        }
        return false;
    }

    private bool OverlapCheckHorizontal(PhysicsObjectTest obj, Vector2 point, float radius)
    {
        int cnt = ZPhysics2D.OverlapCircleNonAlloc(
            point, radius, overlapBuffer,
            obj != null ? obj.WallLayer : ~0,
            obj != null ? obj.zCollider.ZMin + obj.stepUpTolerance + EPSILON : float.MinValue,
            obj != null ? obj.zCollider.ZMax                             : float.MaxValue);

        for (int i = 0; i < cnt; i++)
        {
            Transform t = overlapBuffer[i].transform;
            if (obj != null && t == obj.transform) continue;
            return true;
        }
        return false;
    }

    private Vector2Int DirectionToCellStep(Vector2 dir) => new( dir.x > EPSILON ? 1 : dir.x < -EPSILON ? -1 : 0,
                                                                dir.y > EPSILON ? 1 : dir.y < -EPSILON ? -1 : 0);
    
    private void UpdatePhysicsCellOccupancy(PhysicsObjectTest obj, Vector2Int previousAnchor)
    {
        if (obj.Mode != MovementMode.Physics) return;

        RemoveFootprintOccupant(obj, previousAnchor);
        Vector2Int newAnchor = WorldCenterToAnchorCell(obj.transform.position, obj.Size);
        AddFootprintOccupant(obj, newAnchor);
    }

    private Vector2 AnchorCellToWorldCenter(Vector2Int anchorCell, Vector2Int size)
    {
        Vector2 anchorWorld = CellToWorld(anchorCell);
        Vector2 offset      = (Vector2)(size - Vector2Int.one) * gridSize * 0.5f;
        return anchorWorld + offset;
    }

    private Vector2Int WorldCenterToAnchorCell(Vector2 worldCenter, Vector2Int size)
    {
        Vector2 offset    = 0.5f * gridSize * (Vector2)(size - Vector2Int.one);
        Vector2 anchorPos = worldCenter - offset;
        return new Vector2Int(
            Mathf.RoundToInt(anchorPos.x / gridSize),
            Mathf.RoundToInt(anchorPos.y / gridSize));
    }

    private int GetOccupiedCells(Vector2Int anchorCell, Vector2Int size, Vector2Int[] results)
    {
        int count = 0;
        for (int x = 0; x < size.x; x++) for (int y = 0; y < size.y; y++)
        {
            results[count++] = anchorCell + new Vector2Int(x, y);
            if (count >= results.Length) return results.Length;
        }
        return count;
    }

    private Vector2 CellToWorld(Vector2Int cell) => new(cell.x * gridSize, cell.y * gridSize);
#endregion

    private void Update() // Interpolate rendering
    {
        foreach (var obj in AllPhysicsEntitys)
            obj.DecayBodyPartOffset(renderDecaySpeed, snapDecaySpeed);
    }
}