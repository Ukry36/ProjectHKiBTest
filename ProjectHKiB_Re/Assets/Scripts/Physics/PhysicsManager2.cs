using System.Collections.Generic;
using System.Linq;
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
    public Vector2Int InputDir;
    public Vector2Int MoveDir;
    public bool       IsSettling;
}

[System.Serializable]
public class PhysicsState
{
    public int KeepPhysics;
}

public struct Contact
{
    public PhysicsObjectTest objA;
    public PhysicsObjectTest objB; // null if wall
    public Vector2 normal;
    public float penetration;
    public Contact(PhysicsObjectTest objA, PhysicsObjectTest objB, Vector2 normal, float penetration)
    { 
        this.objA = objA;
        this.objB = objB;
        this.normal = normal; 
        this.penetration = penetration; 
    }
}

public class PhysicsManager2 : MonoBehaviour
{
    // ── Entity List ───────────────────────────────
    private List<PhysicsObjectTest> AllPhysicsObjects = new();
    private Dictionary<long, Contact> Contacts = new(100);

    // ── Constants ─────────────────────────────────
    public const float EPSILON = 0.00001f;

    // ── Inspector Parameters ──────────────────────
    public float gravity           = -9.8f;
    public float gridSize          = 1f;
    public float stopThreshold     = 0.01f;
    public bool enable;
    public float gridSettleSpeed   = 3f;
    public float stepUpTolerance   = 0.5f;
    public float stepDownTolerance = 0.2f;
    public float bounceTolerance   = 0.1f;
    public int keepCanWalkFrames   = 4;
    public int keepPhysicsFrames   = 4;
    public float snapDecaySpeed    = 12f;
    public float renderDecaySpeed  = 12f;
    public bool interpolateRender  = true;

    // ── Buffer ────────────────────────────────────
    private readonly Collider2D[] overlapBuffer       = new Collider2D[64];
    private readonly PhysicsObjectTest[] objectBuffer = new PhysicsObjectTest[64];
    private readonly Vector2Int[] cellBuffer          = new Vector2Int[64];
    private readonly RaycastHit2D[] rayBuffer         = new RaycastHit2D[64];

    // ── Cell Occupancy Map ────────────────────────
    private readonly Dictionary<Vector2Int, List<PhysicsObjectTest>> cellOccupancy = new();

    // ═════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════
    
    public void AddPhysicsObject(PhysicsObjectTest obj)
    {
        AllPhysicsObjects.Add(obj);
        obj.Grid.CurrentCell  = WorldCenterToAnchorCell(obj.transform.position, obj.Size);
        obj.Grid.TargetCell   = obj.Grid.CurrentCell;
        obj.Grid.CellProgress = 1f;
    }

    // ═════════════════════════════════════════════
    //  FixedUpdate — Main Pipeline
    // ═════════════════════════════════════════════

    private void FixedUpdate()
    {
        if (!enable) return;
        int maxPhysicsStep  = 2;
        int maxColSolveStep = 10;
        int maxPosSolveStep = 10;

        float dt = Time.fixedDeltaTime / maxPhysicsStep;
        for (int ph = 0; ph < maxPhysicsStep; ph++)
        {
            foreach (var obj in AllPhysicsObjects) // 1. Update all object's movement mode.
                UpdateMode(obj); // no more instant snap when updated to grid (instead, calculate targetCell and cellProgress)

            foreach (var obj in AllPhysicsObjects) // 2. Get velocity from gravity, ExForce and walk intent.
                UpdateVelocity(obj, dt);

            foreach (var obj in AllPhysicsObjects) // 3. Update verical physics. This effects horizontal velocity on a slope. 
                UpdateVerticalPhysics(obj);        // Might be changed
            
            foreach (var obj in AllPhysicsObjects) // 4. Proceed targetCell of grid object if it arrived targetCell
                if (obj.Mode == MovementMode.Grid)
                    UpdateGridTarget(obj);
            
            RebuildCellOccupancy(); //
            
            GetContactList(dt);

            ShuffleContactList();

            for (int co = 0; co < maxColSolveStep; co++)
            {
                foreach (var contact in Contacts.Values)
                    ResolveHorizontalCollision(contact);
            }

            // Update horizontal Position
            foreach (var obj in AllPhysicsObjects)
                UpdatePosition(obj, dt);

            // Correct position of overlapping entities
            for (int ps = 0; ps < maxPosSolveStep; ps++)
            {
                //for (int i = 0; i < Contacts.Count; i++)
                    //ResolveHorizontalPosition(Contacts.Values.ToList()[i]);
            }

            // PostProcess
            foreach (var obj in AllPhysicsObjects)
            {
                obj.ExForce       = Vector3.zero;
                obj.PrevEntityPos = obj.transform.position;
                obj.LastSetDir    = obj.IsWalking
                                        ? (Vector3)obj.WalkingDir
                                        : (Vector3)obj.HorizonVelocity.normalized;
            }
        }
    }

    private void UpdateVelocity(PhysicsObjectTest obj, float dt) // Get acceleration of ExForce, gravity and walk intent
    {
        obj.ExForce += Vector3.forward * gravity;
        obj.HorizonVelocity += dt * obj.invM * (Vector2)obj.ExForce;
        obj.ZVelocity += dt * obj.invM * obj.ExForce.z;

        obj.HorizonVelocity = ApplyGroundFriction(obj, obj.HorizonVelocity);

        bool isActivelyWalking = obj.IsWalking && obj.WalkingDir.sqrMagnitude > EPSILON;
        
        if (isActivelyWalking)
        {
            float maxSpd = (obj.IsSprinting ? obj.WalkSpeed * obj.SprintCoeff : obj.WalkSpeed)
                         * (obj.CanWalkFrameLeft > 0 ? 1f : 0.1f);
            Vector2 walkDir            = obj.WalkingDir.normalized;
            float frictionAccInfluence = 1 - Mathf.Clamp01(obj.frictionCoeff * obj.frictionWalkInfluence);
            float WalkAcceleration     = obj.IsSprinting ? obj.WalkAcceleration * obj.SprintCoeff : obj.WalkAcceleration;
            float currentAlongWalk    = Vector2.Dot(obj.HorizonVelocity, walkDir);
            float deficit             = maxSpd - currentAlongWalk;

            if (deficit > 0f)
            {
                float accel = WalkAcceleration * frictionAccInfluence * Time.fixedDeltaTime;
                obj.HorizonVelocity += walkDir * Mathf.Min(accel, deficit);
            }
        }
        else if (obj.Mode == MovementMode.Grid)
        {
            if (obj.HorizonVelocity.magnitude < gridSettleSpeed)
            {
                obj.HorizonVelocity = obj.HorizonVelocity.normalized * (gridSettleSpeed - EPSILON);
                StartGridSettle(obj);
            }
        }
    }

    private void GetContactList(float dt)
    {
        Contacts.Clear();
        foreach (var obj in AllPhysicsObjects)
        {
            switch (obj.Mode)
            {
                case MovementMode.Grid: GetGridContact(obj, dt); break;
                case MovementMode.Physics: GetPhysContact(obj, dt); break;
            }
        }
        // if grid with static wall, don't add to contact list
        // if grid with any another entity, update to physics mode
    }

    private void GetGridContact(PhysicsObjectTest obj, float dt)
    {
        bool checkStatic = obj.Grid.CellProgress >= 1f;
        Vector2Int desiredAnchor = obj.Grid.TargetCell;

        bool staticWall = false;
        if (checkStatic) staticWall = IsStaticWall(obj, desiredAnchor);
        int gridCnt = GetOccupantsInFootprint(obj, desiredAnchor, objectBuffer);
        bool physContacted = Cast(obj, dt, true, true);

        if (!staticWall && gridCnt < 1 && !physContacted) // no wall nor entity
        {
            obj.Grid.TargetCell   = desiredAnchor;
            obj.Grid.CellProgress = 0f;
            AddFootprintOccupant(obj, desiredAnchor);
            return;
        }
        else if (staticWall && gridCnt < 1 && !physContacted) //only static collision 
        {
            TrySlideGrid(obj, obj.Grid.MoveDir);
            return;
        }

        for (int i = 0; i < gridCnt; i++)
        {
            Contact contact = new(obj, objectBuffer[i], -obj.HorizonVelocity.normalized, 0); // this might end up looking bad
            Contacts[GetContactID(obj.ID, objectBuffer[i].ID)] = contact;                    // might be better using casting method
            SwitchGridToPhysics(objectBuffer[i]);
        }

        if (physContacted || gridCnt > 0) SwitchGridToPhysics(obj);
    }

    private void GetPhysContact(PhysicsObjectTest obj, float dt) => Cast(obj, dt, false, false);
    
    private bool Cast(PhysicsObjectTest obj, float dt, bool ignoreStatic, bool ignoreGrid)
    {
        bool detected = false;
        int cnt = obj.zCollider.Cast(obj.HorizonVelocity.normalized, rayBuffer, obj.HorizonVelocity.magnitude * dt);
        Debug.Log("cast: " + cnt);
        for (int i = 0; i < cnt; i++)
        {
            PhysicsObjectTest another = null;
            if (rayBuffer[i].transform.TryGetComponent(out PhysicsObjectTest hitObj)) 
            {
                another = hitObj;
                if (another.ID == obj.ID) continue;
                if (ignoreGrid && another.Mode == MovementMode.Grid) continue;
                else SwitchGridToPhysics(another);
            }
            else if (ignoreStatic) continue;
            int colliderID = ZPhysics2D.TryGet(rayBuffer[i].collider, out var c) ? c.ID : 0;
            int anotherID = another ? another.ID : colliderID;
            
            ColliderDistance2D colliderDistance = obj.zCollider.Distance(rayBuffer[i].collider);
            float penetration = colliderDistance.distance < 0 ? -colliderDistance.distance : 0;

            Contact contact = new(obj, another, rayBuffer[i].normal, penetration);
            Contacts[GetContactID(obj.ID, anotherID)] = contact;
            detected = true;
        }
        return detected;
    }

    private long GetContactID(int objAID, int objBID)
    {
        int IdA = Mathf.Min(objAID, objBID);
        int IdB = Mathf.Max(objAID, objBID);
        
        return ((long)IdA << 32) | (uint)IdB;
    }

    private bool TrySlideGrid(PhysicsObjectTest obj, Vector2Int moveDir)
    {
        if (moveDir.x != 0)
        {
            var slideDir    = new Vector2Int(moveDir.x, 0);
            var slideAnchor = obj.Grid.CurrentCell + slideDir;
            if (!IsStaticWall(obj, slideAnchor) && GetOccupantsInFootprint(obj, slideAnchor, objectBuffer) < 1)
            {
                obj.Grid.TargetCell   = slideAnchor;
                obj.Grid.CellProgress = 0f;
                obj.Grid.MoveDir  = slideDir;
                obj.HorizonVelocity = Vector2.right * obj.HorizonVelocity.x;
                AddFootprintOccupant(obj, slideAnchor);
                return true;
            }
        }
        if (moveDir.y != 0)
        {
            var slideDir    = new Vector2Int(0, moveDir.y);
            var slideAnchor = obj.Grid.CurrentCell + slideDir;
            if (!IsStaticWall(obj, slideAnchor) && GetOccupantsInFootprint(obj, slideAnchor, objectBuffer) < 1)
            {
                obj.Grid.TargetCell   = slideAnchor;
                obj.Grid.CellProgress = 0f;
                obj.Grid.MoveDir  = slideDir;
                obj.HorizonVelocity = Vector2.up * obj.HorizonVelocity.y;
                AddFootprintOccupant(obj, slideAnchor);
                return true;
            }
        }

        obj.Grid.TargetCell   = obj.Grid.CurrentCell;
        obj.Grid.CellProgress = 1f;
        obj.HorizonVelocity = Vector2.zero;
        return false;
    }

    private void UpdateGridTarget(PhysicsObjectTest obj)
    {
        if (obj.Grid.CellProgress >= 1f)
        {
            Vector2    normalizedVel = obj.HorizonVelocity.normalized;
            Vector2Int moveDir       = new(Mathf.RoundToInt(normalizedVel.x), Mathf.RoundToInt(normalizedVel.y));

            if (moveDir == Vector2Int.zero) moveDir = obj.Grid.MoveDir;
            obj.Grid.MoveDir = moveDir;
            obj.Grid.CurrentCell  = obj.Grid.TargetCell;
            obj.Grid.TargetCell  += moveDir;
        }
    }
    private void ShuffleContactList() => AllPhysicsObjects = AllPhysicsObjects.ShuffleList();

    public void ResolveHorizontalCollision(Contact contact)
    {
        // calculate velocity
        PhysicsObjectTest a = contact.objA;
        PhysicsObjectTest b = contact.objB;
        if (!b) { ResolveHorizontalStaticCollision(contact); return; }

        Vector2 relativeVelocity = b.HorizonVelocity - a.HorizonVelocity;

        float velAlongNormal = Vector2.Dot(relativeVelocity, contact.normal);
        Debug.Log($"velAlongNormal={velAlongNormal} normal={contact.normal}");
        if (velAlongNormal > 0) return; // already moving away

        float e = Mathf.Min(a.Restitution, b.Restitution);

        float massSum = a.invM + b.invM;

        float j = -(1f + e) * velAlongNormal;
        j /= massSum;

        Vector2 impulse = contact.normal * j;
        
        a.HorizonVelocity -= a.invM * impulse;
        b.HorizonVelocity += b.invM * impulse;

        // apply friction
        Vector2 tangent = new(-contact.normal.y, contact.normal.x);
        float velAlongTangent = Vector2.Dot(b.HorizonVelocity - a.HorizonVelocity, tangent);

        float jt = -velAlongTangent / massSum;

        float mu = Mathf.Min(a.frictionCoeff, b.frictionCoeff);
        float maxFriction = j * mu;

        jt = Mathf.Clamp(jt, -maxFriction, maxFriction);

        Vector2 frictionImpulse = tangent * jt;
        a.HorizonVelocity -= a.invM * frictionImpulse;
        b.HorizonVelocity += b.invM * frictionImpulse;
    }

    public void ResolveHorizontalStaticCollision(Contact contact)
    {
        // calculate velocity
        PhysicsObjectTest a = contact.objA;
        Vector2 relativeVelocity = - a.HorizonVelocity;

        float velAlongNormal = Vector2.Dot(relativeVelocity, contact.normal);
        if (velAlongNormal > 0) return; // already moving away

        float j = -(1f +  a.Restitution) * velAlongNormal * a.Mass;
        Vector2 impulse = contact.normal * j;
        a.HorizonVelocity -= a.invM * impulse;

        // apply friction
        Vector2 tangent = new(-contact.normal.y, contact.normal.x);
        float velAlongTangent = Vector2.Dot(-a.HorizonVelocity, tangent);

        float jt = -velAlongTangent * a.Mass;

        float mu = a.frictionCoeff;
        float maxFriction = j * mu;

        jt = Mathf.Clamp(jt, -maxFriction, maxFriction);

        Vector2 frictionImpulse = tangent * jt;
        a.HorizonVelocity -= a.invM * frictionImpulse;
    }

    public void ResolveHorizontalPosition(Contact contact, float percent = 0.2f, float slop = 0.01f) // Baumgarte Stabilization
    {
        PhysicsObjectTest a = contact.objA;
        PhysicsObjectTest b = contact.objB;
        if (!b) { ResolveHorizontalStaticPosition(contact); return; }

        float massSum = a.invM + b.invM;
        if (massSum == 0f) return;
        
        float penetrationCorrection = Mathf.Max(contact.penetration - slop, 0f);
        Vector3 correction = (penetrationCorrection / massSum) * percent * contact.normal;
        
        a.transform.position -= a.invM * correction;
        b.transform.position += b.invM * correction;
    }

    private void ResolveHorizontalStaticPosition(Contact contact, float percent = 0.2f, float slop = 0.01f)
    {
        PhysicsObjectTest a = contact.objA;
        
        float penetrationCorrection = Mathf.Max(contact.penetration - slop, 0f);
        Vector3 correction = (penetrationCorrection / a.invM) * percent * contact.normal;
        
        a.transform.position -= a.invM * correction;
    }

    public void UpdatePosition(PhysicsObjectTest obj, float dt)
    {
        switch (obj.Mode)
        {
            case MovementMode.Grid:    UpdateHorizontalGridPosition(obj, dt);    break;
            case MovementMode.Physics: UpdateHorizontalPhysicsPosition(obj, dt); break;
        }
        obj.ZPosition += dt * obj.ZVelocity;
    }
    
    public void UpdateHorizontalGridPosition(PhysicsObjectTest obj, float dt)
    {
        if (obj.Grid.IsSettling)
        {
            bool hasInput = obj.IsWalking && obj.WalkingDir.sqrMagnitude > EPSILON;
            if (!hasInput) { UpdateGridSettle(obj); return; }
            obj.Grid.IsSettling = false;
        }

        float currentSpeed = obj.HorizonVelocity.magnitude;
        if (currentSpeed < EPSILON) return;

        if (obj.Grid.CurrentCell != obj.Grid.TargetCell)
        {
            float cellDistance = Vector2.Distance(
                AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size),
                AnchorCellToWorldCenter(obj.Grid.TargetCell,  obj.Size));
            obj.Grid.CellProgress += currentSpeed * dt / cellDistance;
            obj.Grid.CellProgress  = Mathf.Clamp01(obj.Grid.CellProgress);
        }

        Vector2 worldCur    = AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size);
        Vector2 worldTarget = AnchorCellToWorldCenter(obj.Grid.TargetCell,  obj.Size);
        Vector2 newPos      = Vector2.Lerp(worldCur, worldTarget, obj.Grid.CellProgress);
        obj.transform.position = new Vector3(newPos.x, newPos.y, obj.ZPosition);
    }
    
    public void UpdateHorizontalPhysicsPosition(PhysicsObjectTest obj, float dt)
    {
        if (obj.HorizonVelocity.magnitude < EPSILON) return;
        obj.transform.position += dt * new Vector3(obj.HorizonVelocity.x, obj.HorizonVelocity.y, 0f);
    }
    // ═════════════════════════════════════════════
    //  Rebuild Cell Occupancy
    // ═════════════════════════════════════════════

    private void RebuildCellOccupancy()
    {
        cellOccupancy.Clear();
        foreach (var obj in AllPhysicsObjects)
        {
            if (obj.Mode == MovementMode.Grid)
            {
                int cnt = GetOccupiedCells(obj.Grid.CurrentCell, obj.Size, cellBuffer); // might be better remove this? check later
                for (int i = 0; i < cnt; i++)
                    AddCellOccupant(cellBuffer[i], obj);
                cnt = GetOccupiedCells(obj.Grid.TargetCell, obj.Size, cellBuffer);
                for (int i = 0; i < cnt; i++)
                    AddCellOccupant(cellBuffer[i], obj);
            }
            /*else
            {
                Vector2Int anchor = WorldCenterToAnchorCell(obj.transform.position, obj.Size);

                int cnt = GetOccupiedCells(anchor, obj.Size, cellBuffer); // might be better remove this? check later
                for (int i = 0; i < cnt; i++)
                    AddCellOccupant(cellBuffer[i], obj);
            }*/
        }
    }

    /// <summary>특정 셀에 자신이 아닌 다른 점유자가 있으면 반환. 없으면 null.</summary>
    private PhysicsObjectTest GetCellOccupant(Vector2Int cell, PhysicsObjectTest self)
    {
        if (!cellOccupancy.TryGetValue(cell, out var occupants)) return null;
        for (int i = 0; i < occupants.Count; i++)
        {
            PhysicsObjectTest occupant = occupants[i];
            if (occupant != null && occupant != self
            && self.zCollider.ZMin + stepUpTolerance < occupant.zCollider.ZMax
            && self.zCollider.ZMax > occupant.zCollider.ZMin)
                return occupant;
        }
        return null;
    }

    /// <summary>
    /// Find all occupants in the footprint area. It contains multiple same objects!!!
    /// </summary>
    private int GetOccupantsInFootprint(PhysicsObjectTest obj, Vector2Int anchorCell, PhysicsObjectTest[] results)
    {
        var seen = new HashSet<PhysicsObjectTest>();
        int count = 0;
        int allCellCnt = GetOccupiedCells(anchorCell, obj.Size, cellBuffer);
        for (int i = 0; i < allCellCnt; i++)
        {
            var occ = GetCellOccupant(cellBuffer[i], obj);
            if (occ != null && seen.Add(occ)) results[count++] = occ;
            if (count >= results.Length) return results.Length;
        }
        return count;
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

    // ═════════════════════════════════════════════
    //  Phase 3 — Update Mode
    // ═════════════════════════════════════════════

    private void UpdateMode(PhysicsObjectTest obj)
    {
        float exForceMag = ((Vector2)obj.ExForce).magnitude;
        float threshold  = obj.ModeTransitionThreshold;

        bool wantsGrid    = obj.IsGrounded && exForceMag < threshold && !obj.IsOnSlope;
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

            if (obj.HorizonVelocity.sqrMagnitude > EPSILON)
            {
                Vector2 d = obj.HorizonVelocity.normalized;
                obj.Grid.MoveDir = new Vector2Int(
                    Mathf.RoundToInt(d.x), Mathf.RoundToInt(d.y));
            }

            Vector2 snapTarget = AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size);
            if (interpolateRender) obj.SetBodyPartSnapOffset(snapTarget);
            SnapPositionToCell(obj, obj.Grid.CurrentCell);
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
    }


    // ═════════════════════════════════════════════
    //  Grid Settle
    // ═════════════════════════════════════════════

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
            (obj.Grid.CurrentCell, obj.Grid.TargetCell) =
                (obj.Grid.TargetCell, obj.Grid.CurrentCell);
            obj.Grid.CellProgress = 1f - obj.Grid.CellProgress;
        }

        obj.Grid.IsSettling = true;
    }

    private void UpdateGridSettle(PhysicsObjectTest obj)
    {
        obj.Grid.CellProgress += gridSettleSpeed * Time.fixedDeltaTime / gridSize;

        if (obj.Grid.CellProgress >= 1f)
        {
            obj.Grid.CellProgress = 1f;
            obj.Grid.CurrentCell  = obj.Grid.TargetCell;
            obj.Grid.IsSettling   = false;
            SnapPositionToCell(obj, obj.Grid.TargetCell);
            return;
        }

        Vector2 from   = AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size);
        Vector2 to     = AnchorCellToWorldCenter(obj.Grid.TargetCell,  obj.Size);
        Vector2 newPos = Vector2.Lerp(from, to, obj.Grid.CellProgress);
        obj.transform.position = new Vector3(newPos.x, newPos.y, obj.ZPosition);
    }


    

    // ═════════════════════════════════════════════
    //  Vertical Physics
    // ═════════════════════════════════════════════

    private void UpdateVerticalPhysics(PhysicsObjectTest obj) // Update vertical physics including position change
    {
        Vector2 checkSize = (Vector2)obj.Size - new Vector2(0.2f, 0.2f);

        ZCollider2D floor   = ZPhysics2D.ZBoxGetFloor(
            obj.transform.position, checkSize, 0, obj.floorLayer,
            obj.zCollider.ZMin - stepDownTolerance - EPSILON,
            obj.zCollider.ZMin + stepUpTolerance   + EPSILON);
        ZCollider2D ceiling = ZPhysics2D.ZBoxGetCeiling(
            obj.transform.position, checkSize, 0, obj.floorLayer,
            obj.zCollider.ZMax + stepDownTolerance + EPSILON,
            obj.zCollider.ZMax - stepUpTolerance   - EPSILON);

        Vector3 surfaceNormal = floor ? floor.GetSurfaceNormal() : Vector3.forward;
        Vector3 vel       = new(obj.HorizonVelocity.x, obj.HorizonVelocity.y, obj.ZVelocity);
        bool towardsFloor = Vector3.Dot(surfaceNormal, vel) < EPSILON;

        obj.IsGrounded = floor
                      && floor.ZmaxBox(obj.transform.position, checkSize, 0) > obj.zCollider.ZMin - EPSILON
                      && towardsFloor;
        obj.IsOnSlope  = Vector3.Dot(surfaceNormal, Vector3.forward) < 1f - EPSILON;

        bool calcGround = false;
        if (obj.IsGrounded)
        {
            if (!obj.IsGroundedPrev && towardsFloor)
            {
                Vector3 reflected   = vel - (1f + obj.Restitution) * Vector3.Dot(vel, surfaceNormal) * surfaceNormal;
                obj.ZVelocity       = reflected.z;
                obj.HorizonVelocity = new(reflected.x, reflected.y);
                float verAcc        = Mathf.Abs(obj.ExForce.z / Mathf.Max(obj.Mass, EPSILON));
                float minEscapeVel  = verAcc * bounceTolerance;
                if (obj.ZVelocity < minEscapeVel) obj.ZVelocity = 0f;
            }
            else calcGround = true;
        }
        else if (obj.IsGroundedPrev && floor && towardsFloor)
        {
            obj.IsGrounded = true;
            calcGround = true;
        }

        if (calcGround)
        {
            obj.ZPosition = floor.ZmaxBox(obj.transform.position, checkSize, 0);
            if (obj.IsOnSlope)
            {
                Vector3 slopeVel = vel - Vector3.Dot(vel, surfaceNormal) * surfaceNormal;
                obj.HorizonVelocity = (Vector2)slopeVel;
                obj.ZVelocity = slopeVel.z;
            }
            else obj.ZVelocity = 0;
            obj.ExForce = new(obj.ExForce.x, obj.ExForce.y, 0);
        }

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
                    Vector3 reflected   = vel - (1f + obj.Restitution) * vDotN * ceilNormal;
                    obj.ZVelocity       = reflected.z;
                    obj.HorizonVelocity = new(reflected.x, reflected.y);
                }
                else
                {
                    obj.ZVelocity = -Mathf.Abs(obj.ZVelocity) * obj.Restitution;
                }
                if (Mathf.Abs(obj.ZVelocity) < stopThreshold) obj.ZVelocity = 0f;
            }
        }

        if (obj.IsGrounded) obj.CanWalkFrameLeft = keepCanWalkFrames;
        else if (obj.CanWalkFrameLeft > 0) obj.CanWalkFrameLeft--;

        obj.IsGroundedPrev = obj.IsGrounded;
    }

    // ═════════════════════════════════════════════
    //  Grid Snap
    // ═════════════════════════════════════════════

    private void SnapPositionToCell(PhysicsObjectTest obj, Vector2Int anchorCell)
    {
        Vector2 worldCenter    = AnchorCellToWorldCenter(anchorCell, obj.Size);
        obj.transform.position = new Vector3(worldCenter.x, worldCenter.y, obj.ZPosition);
    }

    // ═════════════════════════════════════════════
    //  Friction
    // ═════════════════════════════════════════════

    private Vector2 ApplyGroundFriction(PhysicsObjectTest obj, Vector2 vel)
    {
        float friction = obj.IsGrounded ? obj.frictionCoeff : obj.airFriction;
        vel *= friction;

        float iceBlend      = Mathf.Clamp01((friction - 0.5f) / 0.5f);
        float effectiveStop = stopThreshold * (1f - iceBlend);

        if (vel.magnitude < effectiveStop) vel = Vector2.zero;
        return vel;
    }

    // ═════════════════════════════════════════════
    //  Helpers
    // ═════════════════════════════════════════════

    private bool OverlapCheckHorizontal(PhysicsObjectTest obj, Vector2 point, float radius)
    {
        int cnt = ZPhysics2D.OverlapCircleNonAlloc(
            point, radius, overlapBuffer,
            obj != null ? obj.WallLayer : ~0,
            obj != null ? obj.zCollider.ZMin + stepUpTolerance + EPSILON : float.MinValue,
            obj != null ? obj.zCollider.ZMax                             : float.MaxValue);

        for (int i = 0; i < cnt; i++)
        {
            Transform t = overlapBuffer[i].transform;
            if (obj != null && t == obj.transform) continue;
            return true;
        }
        return false;
    }

    public Vector2 AnchorCellToWorldCenter(Vector2Int anchorCell, Vector2Int size)
    {
        Vector2 anchorWorld = CellToWorld(anchorCell);
        Vector2 offset      = 0.5f * gridSize * (Vector2)(size - Vector2Int.one);
        return anchorWorld + offset;
    }

    public Vector2Int WorldCenterToAnchorCell(Vector2 worldCenter, Vector2Int size)
    {
        Vector2 offset    = 0.5f * gridSize * (Vector2)(size - Vector2Int.one);
        Vector2 anchorPos = worldCenter - offset;
        return new Vector2Int(
            Mathf.RoundToInt(anchorPos.x / gridSize),
            Mathf.RoundToInt(anchorPos.y / gridSize));
    }

    public int GetOccupiedCells(Vector2Int anchorCell, Vector2Int size, Vector2Int[] results)
    {
        int count = 0;
        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        {
            results[count++] = anchorCell + new Vector2Int(x, y);
            if (count >= results.Length) return results.Length;
        }
        return count;
    }

    // ── 하위 호환 래퍼 (1x1 기준 기존 코드용) ────────────
    public Vector2Int WorldToCell(Vector2 worldPos)  => WorldCenterToAnchorCell(worldPos, Vector2Int.one);
    public Vector2    CellToWorld(Vector2Int cell)   => new(cell.x * gridSize, cell.y * gridSize);

    // ═════════════════════════════════════════════
    //  Interpolate Render
    // ═════════════════════════════════════════════

    private void Update()
    {
        foreach (var obj in AllPhysicsObjects)
            obj.DecayBodyPartOffset(renderDecaySpeed, snapDecaySpeed);
    }
}