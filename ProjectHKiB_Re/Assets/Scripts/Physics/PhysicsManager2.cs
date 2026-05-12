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
    public float      CellProgress; // 0~1, 셀 간 보간 진행도
    public Vector2Int InputDir;
    public Vector2Int LastMoveDir; // 마지막으로 이동한 방향 (슬라이딩 지속용)
    public Vector2    Velocity;   // 현재 격자 이동 속도 (units/sec)
    public bool       IsSettling;
}

[System.Serializable]
public class PhysicsState
{
    public Vector2 Velocity;
}


// ──────────────────────────────────────────────
//  PhysicsManager
// ──────────────────────────────────────────────

public class PhysicsManager2 : MonoBehaviour
{
    // ── Entity List ───────────────────────────────
    private List<PhysicsObjectTest> AllPhysicsEntitys = new();

    // ── Constants ─────────────────────────────────
    public const float EPSILON = 0.00001f;

    // ── Inspector Parameters ──────────────────────
    public float gravity                  = -9.8f;
    public float gridSize                 = 1f;
    public float PhysSettleBlendThreshold = 0.5f;
    public float PhysSettleStrength       = 8f;
    public float stopThreshold            = 0.01f;
    public bool enable;
    public float gridSettleSpeed = 3f;

    // ── Buffer ────────────────────────────────────
    private readonly Collider2D[] overlapBuffer = new Collider2D[32];

    // ── Cell Occupancy Map ────────────────────────
    // Dynamic entity occupancy only. Static walls are still queried through ZPhysics2D.
    private readonly Dictionary<Vector2Int, List<PhysicsObjectTest>> cellOccupancy = new();

    // ═════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════

    public void AddPhysicsObject(PhysicsObjectTest obj)
    {
        AllPhysicsEntitys.Add(obj);
        obj.Grid.CurrentCell  = WorldToCell(obj.transform.position);
        obj.Grid.TargetCell   = obj.Grid.CurrentCell;
        obj.Grid.CellProgress = 1f;
    }

    // ═════════════════════════════════════════════
    //  FixedUpdate — Main Pipeline
    // ═════════════════════════════════════════════

    private void FixedUpdate()
    {
        if (!enable) return;
        // ── Phase 1 : Vertical Physics ─────────────
        foreach (var obj in AllPhysicsEntitys)
            UpdateVerticalPhysics(obj);

        // Phase 2: update modes before rebuilding occupancy.
        foreach (var obj in AllPhysicsEntitys)
            UpdateMode(obj);

        // Phase 3: rebuild dynamic entity occupancy.
        RebuildCellOccupancy();

        // ── Phase 4a : Grid Mod Movement ───────────
        foreach (var obj in AllPhysicsEntitys)
        {
            if (obj.Mode == MovementMode.Grid)
                UpdateGridMovement(obj);
        }

        // ── Phase 4b : Physics Mode Movement ────────
        foreach (var obj in AllPhysicsEntitys)
        {
            if (obj.Mode == MovementMode.Physics)
                UpdatePhysicsMovement(obj);
        }

        // ── Phase 5 : PostProcess ───────────────────
        foreach (var obj in AllPhysicsEntitys)
        {
            if (obj.Mode == MovementMode.Physics)
                SettleToGrid(obj);

            obj.ExForce        = Vector3.zero;
            obj.PrevEntityPos  = obj.transform.position;
            obj.LastSetDir     = obj.IsWalking
                                     ? (Vector3)obj.WalkingDir
                                     : (Vector3)obj.Phys.Velocity.normalized;
            RecordRenderPosition(obj);
        }
    }

    // ═════════════════════════════════════════════
    //  Phase 2 — Rebuild Cell Occupancy Map
    // ═════════════════════════════════════════════

    private void RebuildCellOccupancy()
    {
        cellOccupancy.Clear();
        foreach (var obj in AllPhysicsEntitys)
        {
            if (obj.Mode == MovementMode.Grid)
            {
                // Reserve both cells so moving grid entities cannot be crossed mid-step.
                AddCellOccupant(obj.Grid.CurrentCell, obj);
                AddCellOccupant(obj.Grid.TargetCell, obj);
            }
            else
            {
                AddCellOccupant(WorldToCell(obj.transform.position), obj);
            }
        }
    }

    /// <summary>If there is entity on cell, return it. Else, return null.</summary>
    private PhysicsObjectTest GetCellOccupant(Vector2Int cell, PhysicsObjectTest ignore = null)
    {
        if (!cellOccupancy.TryGetValue(cell, out var occupants)) return null;

        for (int i = 0; i < occupants.Count; i++)
        {
            PhysicsObjectTest occupant = occupants[i];
            if (occupant != null && occupant != ignore) return occupant;
        }
        return null;
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
        if (occupants.Count == 0)
            cellOccupancy.Remove(cell);
    }

    /// <summary>Check if there is static wall collider.</summary>
    private bool IsStaticWall(PhysicsObjectTest obj, Vector2Int cell)
    {
        Vector2 worldPos = CellToWorld(cell);
        return OverlapCheckHorizontal(obj, worldPos, 0.4f);
    }

    // ═════════════════════════════════════════════
    //  Phase 3 — Update Physics Mode
    // ═════════════════════════════════════════════

    private void UpdateMode(PhysicsObjectTest obj)
    {
        // XY 외력만 판정 (Z 중력 제외)
        float exForceMag = ((Vector2)obj.ExForce).magnitude;
        float threshold  = obj.ModeTransitionThreshold;

        bool wantsPhysics = !obj.IsGrounded || exForceMag >= threshold;
        bool wantsGrid    = obj.IsGrounded && exForceMag < threshold;

        if (obj.Mode == MovementMode.Physics && wantsGrid)
        {
            obj.Mode = MovementMode.Grid;

            obj.Grid.CurrentCell  = WorldToCell(obj.transform.position);
            obj.Grid.TargetCell   = obj.Grid.CurrentCell;
            obj.Grid.CellProgress = 1f;
            obj.Grid.IsSettling   = false;

            obj.Grid.Velocity = obj.Phys.Velocity; // PhysicsVelocity to GridVelocity
            if (obj.Phys.Velocity.sqrMagnitude > EPSILON)
            {
                Vector2 d = obj.Phys.Velocity.normalized;
                obj.Grid.LastMoveDir = new Vector2Int(
                    Mathf.RoundToInt(d.x), Mathf.RoundToInt(d.y));
            }

            obj.Phys.Velocity = Vector2.zero;
            SnapPositionToCell(obj, obj.Grid.CurrentCell);
        }
        else if (obj.Mode == MovementMode.Grid && wantsPhysics)
        {
            obj.Mode = MovementMode.Physics;
            obj.Phys.Velocity = obj.Grid.Velocity; // GridVelocity to PhysicsVelocity
            obj.Grid.Velocity = Vector2.zero;
        }
    }

    // ═════════════════════════════════════════════
    //  Phase 4a — Grid Movement
    // ═════════════════════════════════════════════

    private void UpdateGridMovement(PhysicsObjectTest obj)
    {
        // ─ 정착 중 처리 (기존과 동일) ─
        if (obj.Grid.IsSettling)
        {
            bool hasInput = obj.IsWalking && obj.WalkingDir.sqrMagnitude > EPSILON;
            if (!hasInput)
            {
                UpdateGridSettle(obj);
                return;
            }
            obj.Grid.IsSettling = false;
        }
    
        // ─ InputDir 갱신 (기존과 동일) ─
        Vector2Int inputDir = Vector2Int.zero;
        if (obj.IsWalking && obj.WalkingDir.sqrMagnitude > EPSILON)
        {
            Vector2 wd = obj.WalkingDir.normalized;
            inputDir = new Vector2Int(Mathf.RoundToInt(wd.x), Mathf.RoundToInt(wd.y));
        }
        obj.Grid.InputDir = inputDir;
    
        float maxSpd = (obj.IsSprinting ? obj.WalkSpeed * obj.SprintCoeff : obj.WalkSpeed) * (obj.IsGrounded ? 1f : 0.1f);
        float frictionAccInfluence = 1 - Mathf.Clamp01(obj.frictionCoeff * obj.frictionWalkInfluence);
        float WalkAcceleration = obj.IsSprinting ? obj.WalkAcceleration * obj.SprintCoeff : obj.WalkAcceleration;
    
        // ─ Grid Velocity 2D 연산 ───────────────────────────────────
        // Physics 모드와 동일하게 "마찰 → 보행 가속" 순서로 처리한다.
        obj.Grid.Velocity = ApplyFriction(obj, obj.Grid.Velocity);
        
        if (inputDir != Vector2Int.zero)
        {
            Vector2 walkDir = ((Vector2)inputDir).normalized;
        
            float currentAlongWalk = Vector2.Dot(obj.Grid.Velocity, walkDir);
            float deficit          = maxSpd - currentAlongWalk;
        
            if (deficit > 0f)
            {
                float accel = WalkAcceleration * frictionAccInfluence * Time.fixedDeltaTime;
                obj.Grid.Velocity += walkDir * Mathf.Min(accel, deficit);
            }
        }
        else
        {
            if (obj.Grid.Velocity.magnitude < gridSettleSpeed)
            {
                obj.Grid.Velocity = Vector2.zero;
                StartGridSettle(obj);
                return;
            }
        }
    
        float currentSpeed = obj.Grid.Velocity.magnitude;
        if (currentSpeed < EPSILON) return;
    
        // 현재 관성(Velocity)이 향하는 지배적인 방향을 추출
        Vector2 normalizedVel = obj.Grid.Velocity.normalized;
        Vector2Int moveDir = new Vector2Int(
            Mathf.RoundToInt(normalizedVel.x),
            Mathf.RoundToInt(normalizedVel.y)
        );
        // 속도가 너무 작아 반올림 시 0이 될 경우 이전 방향 유지
        if (moveDir == Vector2Int.zero) moveDir = obj.Grid.LastMoveDir; 
    
        // ─ 다음 셀 결정 (목표 셀 도착 시) ──────────────────
        if (obj.Grid.CellProgress >= 1f)
        {
            obj.Grid.CurrentCell  = obj.Grid.TargetCell;
            obj.Grid.CellProgress = 1f;
    
            // 인풋이 아닌, 현재 관성의 방향(moveDir)으로 이동 시도
            obj.Grid.LastMoveDir = moveDir;
            Vector2Int desiredCell = obj.Grid.CurrentCell + moveDir;
            TryMoveToCell(obj, desiredCell, moveDir);
        }
    
        // ─ 셀 간 보간 진행 ───────────────────────────────────
        if (obj.Grid.CurrentCell != obj.Grid.TargetCell)
        {
            // currentSpeed (Velocity의 크기)를 사용하여 보간 진행
            float cellDistance = Vector2.Distance(CellToWorld(obj.Grid.CurrentCell), CellToWorld(obj.Grid.TargetCell));
            obj.Grid.CellProgress += currentSpeed * Time.fixedDeltaTime / cellDistance;
            obj.Grid.CellProgress  = Mathf.Clamp01(obj.Grid.CellProgress);
        }
    
        // ─ 위치 적용 (기존과 동일) ─────────────────────────────────────────
        Vector2 worldCur    = CellToWorld(obj.Grid.CurrentCell);
        Vector2 worldTarget = CellToWorld(obj.Grid.TargetCell);
        Vector2 newPos      = Vector2.Lerp(worldCur, worldTarget, obj.Grid.CellProgress);
        obj.transform.position = new Vector3(newPos.x, newPos.y, obj.ZPosition);
    }

    // ═════════════════════════════════════════════
    //  Grid Settle (마찰 정지 후 셀 정착)
    // ═════════════════════════════════════════════

    /// <summary>
    /// 마찰로 멈출 때 호출. 현재 CellProgress 기준으로
    /// 가장 가까운 셀을 TargetCell로 재설정하고 IsSettling = true.
    /// progress < 0.5 → CurrentCell로 역방향 활주 (Current/Target 교환, Progress 반전)
    /// progress >= 0.5 → TargetCell로 그대로 계속 활주
    /// </summary>
    private void StartGridSettle(PhysicsObjectTest obj)
    {
        // If it is already on grid, return;
        float cellDistance = Vector2.Distance(CellToWorld(obj.Grid.CurrentCell), CellToWorld(obj.Grid.TargetCell));
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
        
        if (obj.Grid.CellProgress < 0.2f) // This let entity go backwards
        {
            (obj.Grid.CurrentCell, obj.Grid.TargetCell) =
                (obj.Grid.TargetCell, obj.Grid.CurrentCell);
            obj.Grid.CellProgress = 1f - obj.Grid.CellProgress;
        }

        obj.Grid.IsSettling = true;
    }

    /// <summary>
    /// IsSettling 중 매 FixedUpdate: gridSettleSpeed 속도로 CellProgress를 1까지 진행.
    /// 도달하면 IsSettling 해제 후 셀 중앙에 스냅.
    /// </summary>
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

        Vector2 from   = CellToWorld(obj.Grid.CurrentCell);
        Vector2 to     = CellToWorld(obj.Grid.TargetCell);
        Vector2 newPos = Vector2.Lerp(from, to, obj.Grid.CellProgress);
        obj.transform.position = new Vector3(newPos.x, newPos.y, obj.ZPosition);
    }

    /// <summary>
    /// desiredCell 로 이동을 시도한다.
    /// 막혀 있으면 슬라이드, Physics 오브젝트면 충격량 전달 후 슬라이드.
    /// </summary>
    private void TryMoveToCell(PhysicsObjectTest obj, Vector2Int desiredCell, Vector2Int moveDir)
    {
        bool staticWall          = IsStaticWall(obj, desiredCell);
        PhysicsObjectTest occupant = GetCellOccupant(desiredCell, obj);

        if (!staticWall && occupant == null)
        {
            obj.Grid.TargetCell   = desiredCell;
            obj.Grid.CellProgress = 0f;
            AddCellOccupant(desiredCell, obj);
            return;
        }

        if (occupant != null)
            ApplyWalkImpulseToPhysics(obj, occupant, moveDir);

        bool slid = TrySlideGrid(obj, moveDir);
        if (!slid) obj.Grid.Velocity = Vector2.zero; // 완전히 막힘 → 즉시 정지
    }

    // 슬라이드 성공 여부 반환
    private bool TrySlideGrid(PhysicsObjectTest obj, Vector2Int inputDir)
    {
        if (inputDir.x != 0)
        {
            var slideX = new Vector2Int(inputDir.x, 0);
            var cellX  = obj.Grid.CurrentCell + slideX;
            if (!IsStaticWall(obj, cellX) && GetCellOccupant(cellX, obj) == null)
            {
                obj.Grid.TargetCell      = cellX;
                obj.Grid.CellProgress    = 0f;
                obj.Grid.LastMoveDir     = slideX;
                AddCellOccupant(cellX, obj);
                return true;
            }
        }
        if (inputDir.y != 0)
        {
            var slideY = new Vector2Int(0, inputDir.y);
            var cellY  = obj.Grid.CurrentCell + slideY;
            if (!IsStaticWall(obj, cellY) && GetCellOccupant(cellY, obj) == null)
            {
                obj.Grid.TargetCell      = cellY;
                obj.Grid.CellProgress    = 0f;
                obj.Grid.LastMoveDir     = slideY;
                AddCellOccupant(cellY, obj);
                return true;
            }
        }

        obj.Grid.TargetCell   = obj.Grid.CurrentCell;
        obj.Grid.CellProgress = 1f;
        return false;
    }

    /// <summary>Grid 엔티티가 Physics 엔티티에 충격량 전달.</summary>
    private void ApplyWalkImpulseToPhysics(PhysicsObjectTest walker, PhysicsObjectTest target, Vector2Int dir)
    {
        float spd     = walker.IsSprinting ? walker.WalkSpeed * walker.SprintCoeff : walker.WalkSpeed;
        float impulse = walker.Mass * spd / Mathf.Max(target.Mass, EPSILON);

        if (target.Mode == MovementMode.Grid)
            SwitchGridToPhysics(target);

        target.Phys.Velocity += (1f + target.bounceCoeff) * impulse * ((Vector2)dir).normalized;
    }

    private void SwitchGridToPhysics(PhysicsObjectTest obj)
    {
        if (obj.Mode != MovementMode.Grid) return;

        RemoveCellOccupant(obj.Grid.CurrentCell, obj);
        RemoveCellOccupant(obj.Grid.TargetCell, obj);

        obj.Mode          = MovementMode.Physics;
        obj.Phys.Velocity = obj.Grid.Velocity;
        obj.Grid.Velocity = Vector2.zero;

        AddCellOccupant(WorldToCell(obj.transform.position), obj);
    }

    // ═════════════════════════════════════════════
    //  Phase 4b — Physics 이동 (CCD 서브스텝)
    // ═════════════════════════════════════════════

    private void UpdatePhysicsMovement(PhysicsObjectTest obj)
    {
        bool isActivelyWalking = obj.IsWalking && obj.WalkingDir.sqrMagnitude > EPSILON;

        // ─ 마찰 적용 (항상 전체 속도에 적용) ─────────────────────────────────
        obj.Phys.Velocity = ApplyFriction(obj, obj.Phys.Velocity);

        // ─ 외력 ───────────────────────────────────────────────────────────────
        obj.Phys.Velocity += (Vector2)obj.ExForce / Mathf.Max(obj.Mass, EPSILON) * Time.fixedDeltaTime;

        // ─ 보행 가속 ──────────────────────────────────────────────────────────
        if (isActivelyWalking)
        {
            float maxSpd = (obj.IsSprinting ? obj.WalkSpeed * obj.SprintCoeff : obj.WalkSpeed)
                           * (obj.IsGrounded ? 1f : 0.1f);
            Vector2 walkDir = obj.WalkingDir.normalized;
            float frictionAccInfluence = 1 - Mathf.Clamp01(obj.frictionCoeff * obj.frictionWalkInfluence);
            float WalkAcceleration = obj.IsSprinting ? obj.WalkAcceleration * obj.SprintCoeff : obj.WalkAcceleration;

            float currentAlongWalk = Vector2.Dot(obj.Phys.Velocity, walkDir);
            float deficit          = maxSpd - currentAlongWalk;

            if (deficit > 0f)
            {
                float accel = WalkAcceleration * frictionAccInfluence * Time.fixedDeltaTime;
                obj.Phys.Velocity += walkDir * Mathf.Min(accel, deficit);
            }
            // deficit <= 0 (초과 속도)인 경우 마찰이 자연스럽게 maxSpd로 수렴시켜 줌
        }

        // ─ CCD Sub Step ────────────────────────────────────────────
        float totalDist = obj.Phys.Velocity.magnitude * Time.fixedDeltaTime;
        if (totalDist < EPSILON) return;

        int   steps = Mathf.Max(1, Mathf.CeilToInt(totalDist / gridSize));
        float dt    = Time.fixedDeltaTime / steps;

        for (int s = 0; s < steps; s++)
        {
            Vector2 delta = obj.Phys.Velocity * dt;
            if (delta.sqrMagnitude < EPSILON) break;

            Vector2 origin       = obj.transform.position;
            Vector2 nextPosition = origin + delta;
            if (TryResolveCellCollision(obj, nextPosition, delta.normalized))
            {
                if (obj.Phys.Velocity.sqrMagnitude < stopThreshold * stopThreshold) break;
                continue;
            }

            Vector2Int previousCell = WorldToCell(origin);
            obj.transform.position += new Vector3(delta.x, delta.y, 0f);
            UpdatePhysicsCellOccupancy(obj, previousCell);
        }
    }

    // ═════════════════════════════════════════════
    //  충돌 처리
    // ═════════════════════════════════════════════

    /// <summary>Resolve static or entity collision using grid cells.</summary>
    private bool TryResolveCellCollision(PhysicsObjectTest obj, Vector2 nextPosition, Vector2 fallbackDir)
    {
        Vector2Int currentCell = WorldToCell(obj.transform.position);
        Vector2Int nextCell    = WorldToCell(nextPosition);

        if (TryGetStaticCellCollisionNormal(obj, currentCell, nextCell, fallbackDir, out Vector2 staticNormal))
        {
            ResolveStaticCollision(obj, staticNormal);
            return true;
        }

        return TryResolveDynamicCellCollision(obj, currentCell, nextCell, fallbackDir);
    }

    private bool TryGetStaticCellCollisionNormal(PhysicsObjectTest obj, Vector2Int currentCell, Vector2Int nextCell, Vector2 fallbackDir, out Vector2 normal)
    {
        normal = Vector2.zero;
        if (IsStaticWall(obj, currentCell))
        {
            normal = -fallbackDir;
            if (normal.sqrMagnitude < EPSILON)
                normal = Vector2.up;
            normal.Normalize();
            return true;
        }

        Vector2Int dirCell = DirectionToCellStep(fallbackDir);

        if (dirCell.x != 0)
        {
            Vector2Int xCell = currentCell + new Vector2Int(dirCell.x, 0);
            if (IsStaticWall(obj, xCell))
                normal += new Vector2(-dirCell.x, 0f);
        }

        if (dirCell.y != 0)
        {
            Vector2Int yCell = currentCell + new Vector2Int(0, dirCell.y);
            if (IsStaticWall(obj, yCell))
                normal += new Vector2(0f, -dirCell.y);
        }

        if (normal.sqrMagnitude >= EPSILON)
        {
            normal.Normalize();
            return true;
        }

        if (nextCell == currentCell) return false;

        Vector2Int cellDelta = nextCell - currentCell;

        if (cellDelta.x != 0)
        {
            Vector2Int xCell = currentCell + new Vector2Int(cellDelta.x, 0);
            if (IsStaticWall(obj, xCell))
                normal += new Vector2(-Mathf.Sign(cellDelta.x), 0f);
        }

        if (cellDelta.y != 0)
        {
            Vector2Int yCell = currentCell + new Vector2Int(0, cellDelta.y);
            if (IsStaticWall(obj, yCell))
                normal += new Vector2(0f, -Mathf.Sign(cellDelta.y));
        }

        if (normal.sqrMagnitude >= EPSILON)
        {
            normal.Normalize();
            return true;
        }

        if (!IsStaticWall(obj, nextCell)) return false;

        if (cellDelta.x != 0 && cellDelta.y == 0)
            normal = new Vector2(-Mathf.Sign(cellDelta.x), 0f);
        else if (cellDelta.y != 0 && cellDelta.x == 0)
            normal = new Vector2(0f, -Mathf.Sign(cellDelta.y));
        else
            normal = -fallbackDir;

        if (normal.sqrMagnitude < EPSILON)
            normal = Vector2.up;

        normal.Normalize();
        return true;
    }

    private bool TryResolveDynamicCellCollision(PhysicsObjectTest obj, Vector2Int currentCell, Vector2Int nextCell, Vector2 fallbackDir)
    {
        if (TryResolveCellOccupant(obj, currentCell, -fallbackDir))
            return true;

        Vector2Int dirCell = DirectionToCellStep(fallbackDir);

        if (dirCell.x != 0)
        {
            Vector2 normal = new(-dirCell.x, 0f);
            if (TryResolveCellOccupant(obj, currentCell + new Vector2Int(dirCell.x, 0), normal))
                return true;
        }

        if (dirCell.y != 0)
        {
            Vector2 normal = new(0f, -dirCell.y);
            if (TryResolveCellOccupant(obj, currentCell + new Vector2Int(0, dirCell.y), normal))
                return true;
        }

        if (dirCell.x != 0 && dirCell.y != 0)
        {
            Vector2Int diagonalCell = currentCell + dirCell;
            if (TryResolveCellOccupant(obj, diagonalCell, -fallbackDir))
                return true;
        }

        if (nextCell != currentCell)
            return TryResolveCellOccupant(obj, nextCell, -fallbackDir);

        return false;
    }

    private bool TryResolveCellOccupant(PhysicsObjectTest obj, Vector2Int cell, Vector2 fallbackNormal)
    {
        PhysicsObjectTest occupant = GetCellOccupant(cell, obj);
        if (occupant == null) return false;

        Vector2 normal = (Vector2)obj.transform.position - (Vector2)occupant.transform.position;
        if (normal.sqrMagnitude < EPSILON)
            normal = fallbackNormal;
        if (normal.sqrMagnitude < EPSILON)
            normal = Vector2.up;

        return TryResolveEntityCollision(obj, occupant, normal.normalized);
    }

    private Vector2Int DirectionToCellStep(Vector2 dir)
    {
        return new Vector2Int(
            dir.x > EPSILON ? 1 : dir.x < -EPSILON ? -1 : 0,
            dir.y > EPSILON ? 1 : dir.y < -EPSILON ? -1 : 0);
    }

    private void UpdatePhysicsCellOccupancy(PhysicsObjectTest obj, Vector2Int previousCell)
    {
        if (obj.Mode != MovementMode.Physics) return;

        RemoveCellOccupant(previousCell, obj);
        AddCellOccupant(WorldToCell(obj.transform.position), obj);
    }

    /// <summary>정적 벽과의 충돌 (반발 + 미끄러짐).</summary>
    private bool TryResolveEntityCollision(PhysicsObjectTest objA, PhysicsObjectTest objB, Vector2 normal)
    {
        bool aIsGrid = objA.Mode == MovementMode.Grid;
        bool bIsGrid = objB.Mode == MovementMode.Grid;

        Vector2 vA = aIsGrid ? objA.Grid.Velocity : objA.Phys.Velocity;
        Vector2 vB = bIsGrid ? objB.Grid.Velocity : objB.Phys.Velocity;
        float   mA = Mathf.Max(objA.Mass, EPSILON);
        float   mB = Mathf.Max(objB.Mass, EPSILON);

        float vRel = Vector2.Dot(vA - vB, normal);
        if (vRel >= 0f) return false;

        float e = (objA.bounceCoeff + objB.bounceCoeff) * 0.5f;
        float j = -(1f + e) * vRel / (1f / mA + 1f / mB);

        Vector2 impulse = j * normal;

        if (aIsGrid) SwitchGridToPhysics(objA);
        if (bIsGrid) SwitchGridToPhysics(objB);

        objA.Phys.Velocity = vA + impulse / mA;
        objB.Phys.Velocity = vB - impulse / mB;
        return true;
    }

    private void ResolveStaticCollision(PhysicsObjectTest obj, Vector2 normal)
    {
        float vDotN = Vector2.Dot(obj.Phys.Velocity, normal);
        if (vDotN >= 0f) return; // 이미 벽에서 멀어지는 중

        // 법선 방향 반발
        obj.Phys.Velocity -= (1f + obj.bounceCoeff) * vDotN * normal;

        // 마찰에 의한 접선 감속
        Vector2 tangent       = obj.Phys.Velocity - Vector2.Dot(obj.Phys.Velocity, normal) * normal;
        obj.Phys.Velocity     = tangent * obj.frictionCoeff;

        if (obj.Phys.Velocity.magnitude < stopThreshold)
            obj.Phys.Velocity = Vector2.zero;
    }

    /// <summary>두 Physics 엔티티 간 충돌 (운동량 보존).</summary>
    private void ResolveEntityCollision(PhysicsObjectTest objA,
                                         PhysicsObjectTest objB,
                                         Vector2           normal)
    {
        // Grid 모드 엔티티가 섞인 경우
        bool aIsGrid = objA.Mode == MovementMode.Grid;
        bool bIsGrid = objB.Mode == MovementMode.Grid;

        if (aIsGrid || bIsGrid)
        {
            // Grid ↔ Physics: 물리 엔티티에만 충격량
            if (!aIsGrid)
            {
                float vDotN = Vector2.Dot(objA.Phys.Velocity, normal);
                if (vDotN < 0f)
                    objA.Phys.Velocity -= (1f + objA.bounceCoeff) * vDotN * normal;
            }
            if (!bIsGrid)
            {
                float vDotN = Vector2.Dot(objB.Phys.Velocity, -normal);
                if (vDotN < 0f)
                    objB.Phys.Velocity -= (1f + objB.bounceCoeff) * vDotN * (-normal);
            }
            return;
        }

        // Physics ↔ Physics: 계수 반발 탄성 충돌
        Vector2 vA   = objA.Phys.Velocity;
        Vector2 vB   = objB.Phys.Velocity;
        float   mA   = objA.Mass;
        float   mB   = objB.Mass;

        float vRel = Vector2.Dot(vA - vB, normal);
        if (vRel >= 0f) return; // 이미 멀어지는 중

        float e = (objA.bounceCoeff + objB.bounceCoeff) * 0.5f;
        float j = -(1f + e) * vRel / (1f / mA + 1f / mB);

        Vector2 impulse        = j * normal;
        objA.Phys.Velocity    += impulse / mA;
        objB.Phys.Velocity    -= impulse / mB;
    }

    // ═════════════════════════════════════════════
    //  Update Vertical Physics
    // ═════════════════════════════════════════════

    private void UpdateVerticalPhysics(PhysicsObjectTest obj)
    {
        obj.ExForce += Vector3.forward * gravity;

        int cnt = ZPhysics2D.OverlapCircleNonAlloc(
            obj.transform.position, 0.4f,
            overlapBuffer, obj.floorLayer,
            obj.zCollider.ZMin - EPSILON, obj.zCollider.ZMax + EPSILON);

        obj.ZVelocity  += obj.ExForce.z / obj.Mass * Time.fixedDeltaTime;
        obj.IsGrounded  = cnt > 0;

        if (obj.IsGrounded && obj.ExForce.z <= EPSILON)
        {
            obj.ZPosition = overlapBuffer[0].ZGetTop();
            if (!obj.IsGroundedPrev) // Manage bounce
            {
                obj.ZVelocity = -obj.ZVelocity * obj.bounceCoeff;
                if (Mathf.Abs(obj.ZVelocity) < stopThreshold) obj.ZVelocity = 0f;
                obj.ZVelocity = Mathf.Clamp(obj.ZVelocity,
                    -obj.stopAccelerateThreshold, obj.stopAccelerateThreshold);
            }
            else // Cancel out downforce
            {
                obj.ExForce   += Vector3.back * obj.ExForce.z;
                obj.ZVelocity  = 0f;
            }
        }
        else
        {
            obj.ZPosition += obj.ZVelocity * Time.fixedDeltaTime;
        }

        obj.IsGroundedPrev     = obj.IsGrounded;
        Vector3 ep             = obj.transform.position;
        obj.transform.position = new Vector3(ep.x, ep.y, obj.ZPosition);
    }

    // ═════════════════════════════════════════════
    //  Grid Snap / Settle
    // ═════════════════════════════════════════════

    /// <summary>Settle Physics Mode Entity To The Grid Smoothly.</summary>
    private void SettleToGrid(PhysicsObjectTest obj)
    {
        float speed = obj.Phys.Velocity.magnitude;
        if (speed >= PhysSettleBlendThreshold) return;

        float   t         = 1f - Mathf.Clamp01(speed / PhysSettleBlendThreshold);
        float   blend     = t * PhysSettleStrength * Time.fixedDeltaTime;
        Vector2 entityPos = obj.transform.position;
        Vector2 nearCell  = CellToWorld(WorldToCell(entityPos));
        Vector2 delta     = nearCell - entityPos;

        if (delta.magnitude < EPSILON)
        {
            obj.transform.position = new Vector3(nearCell.x, nearCell.y, obj.ZPosition);
            obj.Phys.Velocity      = Vector2.zero;
            return;
        }

        Vector2 newPos         = Vector2.Lerp(entityPos, nearCell, blend);
        obj.transform.position = new Vector3(newPos.x, newPos.y, obj.ZPosition);
        obj.Phys.Velocity     += delta.normalized * (delta.magnitude * t * PhysSettleStrength * Time.fixedDeltaTime);
    }

    /// <summary>Snap Entity To Grid Instantly When Mode Update.</summary>
    private void SnapPositionToCell(PhysicsObjectTest obj, Vector2Int cell)
    {
        Vector2 worldPos       = CellToWorld(cell);
        obj.transform.position = new Vector3(worldPos.x, worldPos.y, obj.ZPosition);
    }

    // ═════════════════════════════════════════════
    // Friction
    // ═════════════════════════════════════════════

    private Vector2 ApplyFriction(PhysicsObjectTest obj, Vector2 vel)
    {
        float friction = obj.IsGrounded ? obj.frictionCoeff : obj.airFriction;
        vel *= friction;

        // frictionCoeff 0.5 이하 → stopThreshold 그대로
        // frictionCoeff 1.0     → stopThreshold 거의 0 (자연 감속만으로 멈춤)
        float iceBlend      = Mathf.Clamp01((friction - 0.5f) / 0.5f);
        float effectiveStop = stopThreshold * (1f - iceBlend);

        if (vel.magnitude < effectiveStop) vel = Vector2.zero;
        return vel;
    }

    // ═════════════════════════════════════════════
    //  공간 쿼리 헬퍼
    // ═════════════════════════════════════════════

    private bool OverlapCheckHorizontal(PhysicsObjectTest obj, Vector2 point, float radius)
    {
        int cnt = ZPhysics2D.OverlapCircleNonAlloc(
            point, radius, overlapBuffer,
            obj != null ? obj.WallLayer : ~0,
            obj != null ? obj.zCollider.ZMin : float.MinValue,
            obj != null ? obj.zCollider.ZMax : float.MaxValue);

        for (int i = 0; i < cnt; i++)
        {
            Transform t = overlapBuffer[i].transform;
            if (obj != null && t == obj.transform) continue;
            return true;
        }
        return false;
    }

    // ═════════════════════════════════════════════
    //  좌표 변환 헬퍼
    // ═════════════════════════════════════════════

    /// <summary>월드 위치 → 셀 좌표 (반올림).</summary>
    public Vector2Int WorldToCell(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / gridSize),
            Mathf.RoundToInt(worldPos.y / gridSize));
    }

    /// <summary>셀 좌표 → 월드 위치 (셀 중앙).</summary>
    public Vector2 CellToWorld(Vector2Int cell)
    {
        return new Vector2(cell.x * gridSize, cell.y * gridSize);
    }

    // ═════════════════════════════════════════════
    //  렌더 보간
    // ═════════════════════════════════════════════

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
            Vector3 interpolated   = Vector3.Lerp(obj._prevRenderPos, obj._nextRenderPos, alpha);
            obj.transform.position = interpolated;
        }
    }
}
