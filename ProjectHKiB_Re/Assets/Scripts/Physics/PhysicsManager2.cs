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


// ──────────────────────────────────────────────
//  PhysicsManager2
// ──────────────────────────────────────────────
//
//  Multi-cell support overview
//  ───────────────────────────
//  오브젝트 크기는 PhysicsObjectTest.Size (Vector2Int) 로 정의됩니다.
//  예) Size = (1,1) → 1x1(기존), Size = (2,2) → 2x2, Size = (3,3) → 3x3
//
//  좌표계 규칙
//  • "앵커 셀(Anchor Cell)" = 오브젝트 footprint의 좌하단(左下端) 셀
//  • transform.position     = footprint 전체의 월드 중심
//  • 1x1 오브젝트는 기존과 완전히 동일하게 동작합니다.
//
//  변환 공식 (gridSize = g, Size = (w,h))
//  • AnchorCellToWorldCenter  : CellToWorld(anchor) + (w-1, h-1) * g * 0.5
//  • WorldCenterToAnchorCell  : Round((center - offset) / g),  offset = (w-1,h-1)*g*0.5
//
// ──────────────────────────────────────────────

public class PhysicsManager2 : MonoBehaviour
{
    // ── Entity List ───────────────────────────────
    private List<PhysicsObjectTest> AllPhysicsEntitys = new();

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
    public int physicsIterations = 4;

    // ── Buffer ────────────────────────────────────
    private readonly Collider2D[] overlapBuffer = new Collider2D[32];
    private readonly Vector2Int[] cellBuffer = new Vector2Int[32];
    private readonly PhysicsObjectTest[] objBuffer = new PhysicsObjectTest[32];

    // ── Cell Occupancy Map ────────────────────────
    private readonly Dictionary<Vector2Int, List<PhysicsObjectTest>> cellOccupancy = new();

    // ═════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════

    public void AddPhysicsObject(PhysicsObjectTest obj)
    {
        AllPhysicsEntitys.Add(obj);
        // ★ 앵커 셀 기준으로 초기화
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

        AllPhysicsEntitys = AllPhysicsEntitys.ShuffleList();

        foreach (var obj in AllPhysicsEntitys)
            UpdateVelocity(obj);

        foreach (var obj in AllPhysicsEntitys)
            UpdateVerticalPhysics(obj);

        foreach (var obj in AllPhysicsEntitys)
            UpdateMode(obj);

        RebuildCellOccupancy();
    
        foreach (var obj in AllPhysicsEntitys)
        {
            if (obj.Mode == MovementMode.Grid)
                UpdateGridMovement(obj);
        }

        foreach (var obj in AllPhysicsEntitys)
        {
            if (obj.Mode == MovementMode.Physics)
                UpdatePhysicsMovementOnlyWall(obj); // (기존 UpdatePhysicsMovement에서 엔티티 충돌을 제외한 벽 충돌 전용 함수)
        }

        for (int i = 0; i < physicsIterations; i++)
        {
            ResolveAllEntityCollisions();
        }

        foreach (var obj in AllPhysicsEntitys)
        {
            obj.ExForce       = Vector3.zero;
            obj.PrevEntityPos = obj.transform.position;
            obj.LastSetDir    = obj.IsWalking
                                    ? (Vector3)obj.WalkingDir
                                    : (Vector3)obj.Velocity.normalized;
        }
    }

    private void UpdateVelocity(PhysicsObjectTest obj)
    {
        obj.ExForce += Vector3.forward * gravity;
        obj.ZVelocity += obj.ExForce.z * obj.InvM * Time.fixedDeltaTime;
        obj.Velocity += obj.InvM * Time.fixedDeltaTime * (Vector2)obj.ExForce;

        obj.Velocity = ApplyFriction(obj, obj.Velocity);

        bool isActivelyWalking = obj.IsWalking && obj.WalkingDir.sqrMagnitude > EPSILON;
        if (isActivelyWalking)
        {
            float maxSpd = (obj.IsSprinting ? obj.WalkSpeed * obj.SprintCoeff : obj.WalkSpeed)
                         * (obj.CanWalkFrameLeft > 0 ? 1f : 0.1f);
            Vector2 walkDir            = obj.WalkingDir.normalized;
            float frictionAccInfluence = 1 - Mathf.Clamp01(obj.frictionCoeff * obj.frictionWalkInfluence);
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

    // ═════════════════════════════════════════════
    //  Rebuild Cell Occupancy
    // ═════════════════════════════════════════════

    private void RebuildCellOccupancy()
    {
        cellOccupancy.Clear();
        foreach (var obj in AllPhysicsEntitys)
        {
            if (obj.Mode == MovementMode.Grid)
            {
                //int cnt = GetOccupiedCells(obj.Grid.CurrentCell, obj.Size, cellBuffer);
                //for (int i = 0; i < cnt; i++)
                    //AddCellOccupant(cellBuffer[i], obj);
                int cnt = GetOccupiedCells(obj.Grid.TargetCell, obj.Size, cellBuffer);
                for (int i = 0; i < cnt; i++)
                    AddCellOccupant(cellBuffer[i], obj);
            }
            else
            {
                // ★ 물리 모드: 현재 위치에서 앵커 셀 계산 후 footprint 등록
                Vector2Int anchor = WorldCenterToAnchorCell(obj.transform.position, obj.Size);
                int cnt = GetOccupiedCells(anchor, obj.Size, cellBuffer);
                for (int i = 0; i < cnt; i++)
                    AddCellOccupant(cellBuffer[i], obj);
            }
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

    private bool HasStaticWallOnLeadingEdge(PhysicsObjectTest obj, Vector2Int anchorCell, Vector2Int stepDir)
    {
        if (stepDir.x != 0)
        {
            int edgeX = stepDir.x > 0 ? anchorCell.x + obj.Size.x : anchorCell.x - 1;
            for (int y = anchorCell.y; y < anchorCell.y + obj.Size.y; y++)
            {
                if (OverlapCheckHorizontal(obj, CellToWorld(new Vector2Int(edgeX, y)), 0.4f))
                    return true;
            }
        }
        if (stepDir.y != 0)
        {
            int edgeY = stepDir.y > 0 ? anchorCell.y + obj.Size.y : anchorCell.y - 1;
            for (int x = anchorCell.x; x < anchorCell.x + obj.Size.x; x++)
            {
                if (OverlapCheckHorizontal(obj, CellToWorld(new Vector2Int(x, edgeY)), 0.4f))
                    return true;
            }
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

            if (obj.Velocity.sqrMagnitude > EPSILON)
            {
                Vector2 d = obj.Velocity.normalized;
                obj.Grid.LastMoveDir = new Vector2Int(
                    Mathf.RoundToInt(d.x), Mathf.RoundToInt(d.y));
            }

            Vector2 gridCenter = AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size);
            obj.Grid.PhysicsReturnOffset = (Vector2)obj.transform.position - gridCenter;

            //Vector2 snapTarget = AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size);
            //if (interpolateRender) obj.SetBodyPartSnapOffset(snapTarget);
            //SnapPositionToCell(obj, obj.Grid.CurrentCell);
            //Debug.Log("do something else");
        }
        else if (obj.Mode == MovementMode.Grid && wantsPhysics)
        {
            obj.Mode             = MovementMode.Physics;
            obj.Phys.KeepPhysics = keepPhysicsFrames;
        }
    }

    // ═════════════════════════════════════════════
    //  Phase 4a — Grid Movement
    // ═════════════════════════════════════════════

    private void UpdateGridMovement(PhysicsObjectTest obj)
    {
        if (obj.Grid.IsSettling)
        {
            bool hasInput = obj.IsWalking && obj.WalkingDir.sqrMagnitude > EPSILON;
            if (!hasInput) { UpdateGridSettle(obj); return; }
            obj.Grid.IsSettling = false;
        }

        // ────────────────────────────────────────────────────────
        // ★ 1. 오프셋 감쇄 로직 추가
        if (obj.Grid.PhysicsReturnOffset.sqrMagnitude > EPSILON)
        {
            // 기존 인스펙터 변수인 snapDecaySpeed를 재활용하여 부드럽게 복구
            obj.Grid.PhysicsReturnOffset = Vector2.Lerp(
                obj.Grid.PhysicsReturnOffset, 
                Vector2.zero, 
                Time.fixedDeltaTime * snapDecaySpeed
            );

            // 오프셋이 눈에 안 띌 정도로 작아지면 연산 최적화를 위해 완전히 0으로 고정
            if (obj.Grid.PhysicsReturnOffset.sqrMagnitude < 0.0001f)
            {
                obj.Grid.PhysicsReturnOffset = Vector2.zero;
            }
        }
        // ────────────────────────────────────────────────────────

        float currentSpeed = obj.Velocity.magnitude;
        if (currentSpeed > EPSILON)
        {
            Vector2    normalizedVel = obj.Velocity.normalized;
            Vector2Int moveDir       = new(
                Mathf.RoundToInt(normalizedVel.x),
                Mathf.RoundToInt(normalizedVel.y));
            if (moveDir == Vector2Int.zero) moveDir = obj.Grid.LastMoveDir;

            if (obj.Grid.CellProgress >= 1f)
            {
                obj.Grid.CurrentCell  = obj.Grid.TargetCell;
                obj.Grid.CellProgress = 1f;

                obj.Grid.LastMoveDir = moveDir;
                Vector2Int desiredAnchor = obj.Grid.CurrentCell + moveDir;
                TryMoveToCellGrid(obj, desiredAnchor, moveDir);
                if (obj.Mode != MovementMode.Grid) return;
            }

            if (obj.Grid.CurrentCell != obj.Grid.TargetCell)
            {
                float cellDistance = Vector2.Distance(
                    AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size),
                    AnchorCellToWorldCenter(obj.Grid.TargetCell,  obj.Size));
                obj.Grid.CellProgress += currentSpeed * Time.fixedDeltaTime / cellDistance;
                obj.Grid.CellProgress  = Mathf.Clamp01(obj.Grid.CellProgress);
            }
        }

        // ────────────────────────────────────────────────────────
        // ★ 2. 최종 위치에 감쇄 중인 오프셋을 더해서 적용
        Vector2 worldCur    = AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size);
        Vector2 worldTarget = AnchorCellToWorldCenter(obj.Grid.TargetCell,  obj.Size);
        Vector2 newPos      = Vector2.Lerp(worldCur, worldTarget, obj.Grid.CellProgress);

        // 이 한 줄이 지터링을 없애주는 핵심!
        newPos += obj.Grid.PhysicsReturnOffset; 

        obj.transform.position = new Vector3(newPos.x, newPos.y, obj.ZPosition);        
        // ────────────────────────────────────────────────────────
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
        // ★ 오프셋 감쇄 추가
        if (obj.Grid.PhysicsReturnOffset.sqrMagnitude > EPSILON)
        {
            obj.Grid.PhysicsReturnOffset = Vector2.Lerp(
                obj.Grid.PhysicsReturnOffset, 
                Vector2.zero, 
                Time.fixedDeltaTime * snapDecaySpeed
            );
            if (obj.Grid.PhysicsReturnOffset.sqrMagnitude < EPSILON)
                obj.Grid.PhysicsReturnOffset = Vector2.zero;
        }

        obj.Grid.CellProgress += gridSettleSpeed * Time.fixedDeltaTime / gridSize;

        if (obj.Grid.CellProgress >= 1f)
        {
            obj.Grid.CellProgress = 1f;
            obj.Grid.CurrentCell  = obj.Grid.TargetCell;
            obj.Grid.IsSettling   = false;

            // ★ Settle 완료 시 강제 스냅되더라도 오프셋은 반영해서 위치가 튀지 않게 함
            Vector2 targetCenter = AnchorCellToWorldCenter(obj.Grid.TargetCell, obj.Size);
            targetCenter += obj.Grid.PhysicsReturnOffset; 
            obj.transform.position = new Vector3(targetCenter.x, targetCenter.y, obj.ZPosition);
            return;
        }

        Vector2 from   = AnchorCellToWorldCenter(obj.Grid.CurrentCell, obj.Size);
        Vector2 to     = AnchorCellToWorldCenter(obj.Grid.TargetCell,  obj.Size);
        Vector2 newPos = Vector2.Lerp(from, to, obj.Grid.CellProgress);

        // ★ 오프셋 적용
        newPos += obj.Grid.PhysicsReturnOffset;

        obj.transform.position = new Vector3(newPos.x, newPos.y, obj.ZPosition);
    }

    /// <summary>
    /// Try moving to the desiredAnchor.
    /// </summary>
    private void TryMoveToCellGrid(PhysicsObjectTest obj, Vector2Int desiredAnchor, Vector2Int moveDir)
    {
        // 1. 정적 벽 검사 (벽이 있으면 슬라이딩 혹은 정지)
        bool staticWall = IsStaticWall(obj, desiredAnchor);
        if (staticWall)
        {
            bool slid = TrySlideGrid(obj, moveDir);
            if (!slid) obj.Velocity = Vector2.zero;
            return;
        }

        // 2. 다른 엔티티(오브젝트) 점유 검사
        int cnt = GetAnyOccupantInFootprint(obj, desiredAnchor, objBuffer);
        if (cnt > 0)
        {
            // 핵심: 앞에 엔티티가 가로막고 있다면 즉시 물리 모드로 전환!
            // 이를 통해 2단계 솔버(ResolveAllEntityCollisions)가 이들을 고체 충돌로 처리하게 유도합니다.
            SwitchGridToPhysics(obj);
            for (int i = 0; i < cnt; i++)
            {
                if (objBuffer[i].Mode == MovementMode.Grid)
                    SwitchGridToPhysics(objBuffer[i]);
            }
            return;
        }

        // 3. 전방이 완전히 비어있다면 정상적으로 그리드 이동 목표 설정
        obj.Grid.TargetCell   = desiredAnchor;
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

    // ═════════════════════════════════════════════
    //  Phase 4b — Physics Movement (CCD)
    // ═════════════════════════════════════════════

    private void UpdatePhysicsMovementOnlyWall(PhysicsObjectTest obj)
    {
        float totalDist = obj.Velocity.magnitude * Time.fixedDeltaTime;
        if (totalDist < EPSILON) return;

        int   steps = Mathf.Max(1, Mathf.CeilToInt(totalDist / gridSize));
        float dt    = Time.fixedDeltaTime / steps;

        bool collided = false;
        for (int s = 0; s < steps; s++)
        {
            Vector2 delta = obj.Velocity * dt;
            if (delta.sqrMagnitude < EPSILON) break;

            Vector2 origin       = obj.transform.position;
            Vector2 nextPosition = origin + delta;

            // 정적 장벽(Wall)만 체크하여 슬라이딩 속도 연산
            if (TryResolveStaticCellCollision(obj, nextPosition, delta.normalized))
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

    private bool TryResolveStaticCellCollision(PhysicsObjectTest obj, Vector2 nextPosition, Vector2 fallbackDir)
    {
        Vector2Int currentAnchor = WorldCenterToAnchorCell(obj.transform.position, obj.Size);
        Vector2Int nextAnchor    = WorldCenterToAnchorCell(nextPosition,           obj.Size);

        if (TryGetStaticCellCollisionNormal(obj, currentAnchor, nextAnchor, fallbackDir, out Vector2 staticNormal))
        {
            ResolveStaticCollision(obj, staticNormal);
            return true;
        }
        return false;
    }

    // ═════════════════════════════════════════════
    //  충돌 처리
    // ═════════════════════════════════════════════

    /// <summary>
    /// ★ 정적 벽 충돌 법선 계산.
    /// 선두 엣지(leading edge) 검사 → 다음 footprint 전체 검사 순으로 진행.
    /// </summary>
    private bool TryGetStaticCellCollisionNormal(PhysicsObjectTest obj, Vector2Int currentAnchor, Vector2Int nextAnchor, Vector2 fallbackDir, out Vector2 normal)
    {
        normal = Vector2.zero;

        // 1. 현재 위치가 이미 벽 안에 있음 (비정상 상태 복구)
        if (IsStaticWall(obj, currentAnchor))
        {
            normal = -fallbackDir.normalized;
            return true;
        }

        Vector2Int dir = DirectionToCellStep(fallbackDir);

        // 2. 선두 엣지 검사 (단일 축)
        if (dir.x != 0 && HasStaticWallOnLeadingEdge(obj, currentAnchor, new Vector2Int(dir.x, 0)))
            normal += new Vector2(-dir.x, 0f);
        if (dir.y != 0 && HasStaticWallOnLeadingEdge(obj, currentAnchor, new Vector2Int(0, dir.y)))
            normal += new Vector2(0f, -dir.y);

        if (normal.sqrMagnitude > 0)
        {
            normal.Normalize();
            return true;
        }

        // 3. 다음 footprint 전체 검사
        if (nextAnchor == currentAnchor) return false;
        if (!IsStaticWall(obj, nextAnchor)) return false;

        Vector2Int anchorDelta = nextAnchor - currentAnchor;
        if      (anchorDelta.x != 0 && anchorDelta.y == 0) normal = new Vector2(-Mathf.Sign(anchorDelta.x), 0f);
        else if (anchorDelta.y != 0 && anchorDelta.x == 0) normal = new Vector2(0f, -Mathf.Sign(anchorDelta.y));
        else                                                 normal = -fallbackDir;

        if (normal.sqrMagnitude < EPSILON) normal = Vector2.up;
        normal.Normalize();
        return true;
    }

    private Vector2Int DirectionToCellStep(Vector2 dir)
    {
        return new Vector2Int(
            dir.x > EPSILON ? 1 : dir.x < -EPSILON ? -1 : 0,
            dir.y > EPSILON ? 1 : dir.y < -EPSILON ? -1 : 0);
    }

    private void UpdatePhysicsCellOccupancy(PhysicsObjectTest obj, Vector2Int previousAnchor)
    {
        if (obj.Mode != MovementMode.Physics) return;

        RemoveFootprintOccupant(obj, previousAnchor);
        Vector2Int newAnchor = WorldCenterToAnchorCell(obj.transform.position, obj.Size);
        AddFootprintOccupant(obj, newAnchor);
    }

    private void ResolveAllEntityCollisions()
    {
        for (int i = 0; i < AllPhysicsEntitys.Count; i++)
        {
            var obj = AllPhysicsEntitys[i];
            Vector2Int currentAnchor = WorldCenterToAnchorCell(obj.transform.position, obj.Size);
            
            // Broadphase: 3x3 주변 셀 검색 (놓치는 오브젝트가 없도록)
            for (int x = -1; x <= obj.Size.x; x++)
            {
                for (int y = -1; y <= obj.Size.y; y++)
                {
                    Vector2Int queryCell = currentAnchor + new Vector2Int(x, y);
                    if (!cellOccupancy.TryGetValue(queryCell, out var occupants)) continue;
                    
                    for (int o = 0; o < occupants.Count; o++)
                    {
                        var occupant = occupants[o];
                        if (occupant == null || occupant == obj) continue;
                        
                        // 이중 충돌 연산 방지
                        if (obj.GetInstanceID() > occupant.GetInstanceID()) continue;
    
                        // 1. Z축(높이) 레이어 겹침 검사
                        if (obj.zCollider.ZMin + stepUpTolerance < occupant.zCollider.ZMax &&
                            obj.zCollider.ZMax > occupant.zCollider.ZMin)
                        {
                            // 2. Narrowphase: 실제 2D 평면 상의 AABB(사각형) 겹침 검사
                            Vector2 posA = obj.transform.position;
                            Vector2 posB = occupant.transform.position;
                            
                            // 각 오브젝트의 중심에서 끝점까지의 거리 (절반 크기)
                            Vector2 extentsA = (Vector2)obj.Size * gridSize * 0.5f;
                            Vector2 extentsB = (Vector2)occupant.Size * gridSize * 0.5f;
    
                            // 스치기만 해도 밀리는 것을 방지하기 위한 미세한 여유 공간 (필요시 조절)
                            float tolerance = 0.01f;
    
                            // X축과 Y축 모두 거리가 (A의 절반 + B의 절반)보다 가까워야 겹친 것
                            bool isOverlappingX = Mathf.Abs(posA.x - posB.x) < (extentsA.x + extentsB.x) - tolerance;
                            bool isOverlappingY = Mathf.Abs(posA.y - posB.y) < (extentsA.y + extentsB.y) - tolerance;
    
                            // ★ 실제로 영역이 파고들었을 때만 물리 충돌 발생
                            if (isOverlappingX && isOverlappingY)
                            {
                                Vector2 normal = posA - posB;
                                if (normal.sqrMagnitude < EPSILON) normal = Vector2.up;
                                
                                TryResolveEntityCollision(obj, occupant, normal.normalized);
                            }
                        }
                    }
                }
            }
        }
    }

    private bool TryResolveEntityCollision(PhysicsObjectTest objA, PhysicsObjectTest objB, Vector2 normal)
    {
        Vector2 vA = objA.Velocity;
        Vector2 vB = objB.Velocity;
        
        float invMA = objA.InvM;
        float invMB = objB.InvM;
    
        Vector2 sep     = (Vector2)objA.transform.position - (Vector2)objB.transform.position;
        float   dist    = sep.magnitude;
        Vector2 pushDir = dist > EPSILON ? sep / dist : normal;
    
        // ──────────────────────────────────────────────────────────
        // ★ [보완] 샌드위치 압박 시 벽 뚫림 방지: 밀리는 방향 뒤에 벽이 있다면 무한 질량(InvM = 0)화
        Vector2Int anchorA = WorldCenterToAnchorCell(objA.transform.position, objA.Size);
        Vector2Int anchorB = WorldCenterToAnchorCell(objB.transform.position, objB.Size);
    
        Vector2Int stepA = DirectionToCellStep(pushDir);   // A가 밀려나는 방향 셀 오프셋
        Vector2Int stepB = DirectionToCellStep(-pushDir);  // B가 밀려나는 방향 셀 오프셋
    
        if (invMA > 0f && IsStaticWall(objA, anchorA + stepA)) invMA = 0f;
        if (invMB > 0f && IsStaticWall(objB, anchorB + stepB)) invMB = 0f;
        // ──────────────────────────────────────────────────────────
    
        float totalInvMass = invMA + invMB;
        if (totalInvMass < EPSILON) return false; // 둘 다 벽에 고정되어 있다면 무시
        
    
        float allowedDist =
        Mathf.Abs(pushDir.x) * (objA.Size.x + objB.Size.x) * 0.5f * gridSize +
        Mathf.Abs(pushDir.y) * (objA.Size.y + objB.Size.y) * 0.5f * gridSize;

        float vRelative   = Vector2.Dot(vA - vB, normal);
        float penetration = allowedDist - dist;

        // ──────────────────────────────────────────────────────────
        // ★ 핵심 1: 허용 오차(Slop) 설정
        const float slop = 0.01f;
        const float velocitySlop = 0.05f; // 이 이하의 미세한 속도 차이는 반발력(Bounce) 무시

        // ★ 핵심 2: 실제로 밀어내야 할 양(penetration > slop)이 있을 때만 보정 필요 판정
        bool needImpulse    = vRelative < -velocitySlop;
        bool needCorrection = penetration > slop; 

        // 둘 다 필요 없다면 아무런 행동(모드 전환 포함)도 하지 않고 즉시 종료!
        if (!needImpulse && !needCorrection) return false;
        objA.Phys.KeepPhysics = keepPhysicsFrames;
        objB.Phys.KeepPhysics = keepPhysicsFrames;

        // ★ 핵심 3: 진짜 연산이 필요할 때만 그리드 -> 물리 모드 전환 (지터링 방지)
        if (objA.Mode == MovementMode.Grid) SwitchGridToPhysics(objA);
        if (objB.Mode == MovementMode.Grid) SwitchGridToPhysics(objB);
        // ──────────────────────────────────────────────────────────

        if (needImpulse)
        {
            float e         = (objA.bounceCoeff + objB.bounceCoeff) * 0.5f;
            float j         = -(1f + e) * vRelative / totalInvMass;
            Vector2 impulse = j * normal;

            objA.Velocity = vA + impulse * invMA;
            objB.Velocity = vB - impulse * invMB;
        }

        if (needCorrection)
        {
            // ★ 핵심 4: percent를 1.0f로 상향 조정하여 잔여 겹침 없이 100% 분리시킴
            const float percent = 1.0f; 

            // penetration이 이미 slop보다 크다는 것을 위에서 보장했으므로 Max가 필요 없음
            float correctAmount = (penetration - slop) * percent / totalInvMass;
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

        return true;
    }

    private void ResolveStaticCollision(PhysicsObjectTest obj, Vector2 normal)
    {
        float vDotN = Vector2.Dot(obj.Velocity, normal);
        if (vDotN >= 0f) return;

        obj.Velocity -= (1f + obj.bounceCoeff) * vDotN * normal;

        if (obj.Velocity.magnitude < stopThreshold)
            obj.Velocity = Vector2.zero;
    }

    // ═════════════════════════════════════════════
    //  Vertical Physics
    // ═════════════════════════════════════════════

    private void UpdateVerticalPhysics(PhysicsObjectTest obj)
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
        Vector2 horizVel  = obj.Mode == MovementMode.Grid ? obj.Velocity : obj.Velocity;
        Vector3 vel       = new(horizVel.x, horizVel.y, obj.ZVelocity);
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
                Vector3 reflected   = vel - (1f + obj.bounceCoeff) * Vector3.Dot(vel, surfaceNormal) * surfaceNormal;
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
            obj.IsGrounded = true;
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
                    Vector3 reflected   = vel - (1f + obj.bounceCoeff) * vDotN * ceilNormal;
                    obj.ZVelocity       = reflected.z;
                    Vector2 newHorizVel = new(reflected.x, reflected.y);
                    if (obj.Mode == MovementMode.Grid) obj.Velocity = newHorizVel;
                    else                               obj.Velocity = newHorizVel;
                }
                else
                {
                    obj.ZVelocity = -Mathf.Abs(obj.ZVelocity) * obj.bounceCoeff;
                }
                if (Mathf.Abs(obj.ZVelocity) < stopThreshold) obj.ZVelocity = 0f;
            }
        }

        if (obj.IsGrounded) obj.CanWalkFrameLeft = keepCanWalkFrames;
        else if (obj.CanWalkFrameLeft > 0) obj.CanWalkFrameLeft--;

        obj.IsGroundedPrev     = obj.IsGrounded;
        Vector3 ep             = obj.transform.position;
        obj.transform.position = new Vector3(ep.x, ep.y, obj.ZPosition);
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

    private Vector2 ApplyFriction(PhysicsObjectTest obj, Vector2 vel)
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

    // ──────────────────────────────────────────────
    // ★ Multi-Cell 좌표 변환 유틸리티
    // ──────────────────────────────────────────────

    /// <summary>
    /// 앵커 셀(좌하단)을 footprint 월드 중심 좌표로 변환.
    /// Size = (1,1) 일 때 기존 CellToWorld 와 동일.
    /// </summary>
    public Vector2 AnchorCellToWorldCenter(Vector2Int anchorCell, Vector2Int size)
    {
        Vector2 anchorWorld = CellToWorld(anchorCell);
        Vector2 offset      = (Vector2)(size - Vector2Int.one) * gridSize * 0.5f;
        return anchorWorld + offset;
    }

    /// <summary>
    /// 월드 중심 좌표를 footprint 앵커 셀(좌하단)로 변환.
    /// Size = (1,1) 일 때 기존 WorldToCell 과 동일.
    /// </summary>
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
        foreach (var obj in AllPhysicsEntitys)
            obj.DecayBodyPartOffset(renderDecaySpeed, snapDecaySpeed);
    }
}