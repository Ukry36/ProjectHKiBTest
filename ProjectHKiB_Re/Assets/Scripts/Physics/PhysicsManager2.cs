
using System.Collections.Generic;
using UnityEngine;

public class GridState
{
    public Vector2Int CurrentCell;
    public Vector2Int TargetCell;
    public float      CellProgress;
    public Vector2Int InputDir;
}

public class PhysicsState
{
    public Vector2 Velocity;
    public float   ZVelocity;
}

public enum MovementMode { Grid, Physics }

public class PhysicsManager2 : MonoBehaviour
{
    /*
    private List<PhysicsObjectTest> AllPhysicsEntitys = new();
    private Dictionary<Vector2Int, PhysicsObjectTest> cellOccupancy = new();
    private Collider2D[] overlapBuffer = new Collider2D[32];
    public float gravity = -9.8f;
    public float gridSize = 1f; 
    public float settleBlendThreshold = 0.5f;
    public float settleStrength = 8f;
    public float settleQuitDist = 0.1f;
    public float stopThreshold = 0.01f;  
    public int MaxPhysicsStep = 10;
    
    private void UpdateMode(PhysicsObjectTest obj)
    {
        bool hasInput      = obj.Grid.InputDir != Vector2Int.zero;
        bool lowExForce    = ((Vector2)obj.ExForce).magnitude < obj.ModeTransitionThreshold;
        bool shouldBeGrid  = obj.IsGrounded && hasInput && lowExForce;
        bool shouldBePhys  = !obj.IsGrounded 
                          || obj.Phys.Velocity.magnitude > obj.ModeTransitionThreshold;
    
        if (obj.Mode == MovementMode.Physics && shouldBeGrid && !shouldBePhys)
        {
            obj.Mode = MovementMode.Grid;
            // Physics→Grid 전환 시 현재 위치를 가장 가까운 셀로 스냅
            obj.Grid.CurrentCell = WorldToCell(obj.transform.position);
            obj.Grid.TargetCell  = obj.Grid.CurrentCell;
            obj.Grid.CellProgress = 0f;
            obj.Phys.Velocity = Vector2.zero;
        }
        else if (obj.Mode == MovementMode.Grid && shouldBePhys)
        {
            obj.Mode = MovementMode.Physics;
            // Grid→Physics 전환 시 이동 방향을 속도로 변환
            obj.Phys.Velocity = (Vector2)obj.Grid.InputDir * obj.WalkSpeed;
        }
    }

    private void UpdateGridMovement(PhysicsObjectTest obj)
    {
        // 목표 셀에 도착했을 때만 다음 셀 결정
        if (obj.Grid.CellProgress >= 1f || obj.Grid.CurrentCell == obj.Grid.TargetCell)
        {
            obj.Grid.CurrentCell  = obj.Grid.TargetCell;
            obj.Grid.CellProgress = 0f;

            if (obj.Grid.InputDir == Vector2Int.zero) return;

            Vector2Int desired = obj.Grid.CurrentCell + obj.Grid.InputDir;
            var occupant = GetCellOccupant(desired); // 아래 참고

            if (occupant == null)
            {
                obj.Grid.TargetCell = desired;  // 빈 칸 → 이동
            }
            else if (occupant.Mode == MovementMode.Physics)
            {
                // Physics 객체 충돌 → 충격량 전달 후 슬라이드 시도
                ApplyWalkImpulse(obj, occupant, obj.Grid.InputDir);
                TrySlideGrid(obj, obj.Grid.InputDir); // 45° 우회
            }
            else
            {
                // 정적 벽 or 다른 Grid 객체 → 슬라이드만
                TrySlideGrid(obj, obj.Grid.InputDir);
            }
        }

        // 셀 간 보간 진행
        float speed = obj.IsSprinting ? obj.WalkSpeed * obj.SprintCoeff : obj.WalkSpeed;
        obj.Grid.CellProgress += speed * Time.fixedDeltaTime / gridSize;
        obj.Grid.CellProgress  = Mathf.Clamp01(obj.Grid.CellProgress);

        Vector2 worldCur    = CellToWorld(obj.Grid.CurrentCell);
        Vector2 worldTarget = CellToWorld(obj.Grid.TargetCell);
        Vector2 newPos      = Vector2.Lerp(worldCur, worldTarget, obj.Grid.CellProgress);
        obj.transform.position = new Vector3(newPos.x, newPos.y, obj.ZPosition);
    }

    // 슬라이드: X축 우선, Y축 우선, 대각선 45° 우회 순으로 시도
    private void TrySlideGrid(PhysicsObjectTest obj, Vector2Int inputDir)
    {
        // X 축 단독 시도
        if (inputDir.x != 0 && GetCellOccupant(obj.Grid.CurrentCell + new Vector2Int(inputDir.x, 0)) == null)
        { obj.Grid.TargetCell = obj.Grid.CurrentCell + new Vector2Int(inputDir.x, 0); return; }

        // Y 축 단독 시도
        if (inputDir.y != 0 && GetCellOccupant(obj.Grid.CurrentCell + new Vector2Int(0, inputDir.y)) == null)
        { obj.Grid.TargetCell = obj.Grid.CurrentCell + new Vector2Int(0, inputDir.y); return; }

        // 완전히 막힘 → 현재 셀 유지
        obj.Grid.TargetCell = obj.Grid.CurrentCell;
    }

    private void UpdatePhysicsMovement(PhysicsObjectTest obj)
    {
        obj.Phys.Velocity = ApplyFriction(obj, obj.Phys.Velocity);
        obj.Phys.Velocity += (Vector2)obj.ExForce / obj.Mass * Time.fixedDeltaTime;

        // CCD 서브스텝: 한 스텝에 gridSize 이상 이동 못하도록
        float remaining = obj.Phys.Velocity.magnitude * Time.fixedDeltaTime;
        int   steps     = Mathf.CeilToInt(remaining / gridSize) + 1;
        float dt        = Time.fixedDeltaTime / steps;

        for (int s = 0; s < steps; s++)
        {
            Vector2 delta = obj.Phys.Velocity * dt;
            var hit = CastWithFilterHorizontal(obj, obj.transform.position, 
                                               delta.normalized, delta.magnitude);
            if (!hit)
            {
                obj.transform.position += new Vector3(delta.x, delta.y, 0);
            }
            else
            {
                bool isMovable = hit.collider.TryGetComponent(out PhysicsObjectTest other);
                if (isMovable) ResolveEntityCollision(obj, other, hit.normal);
                else           ResolveStaticCollision(obj, hit.normal);
                break; // 충돌 후 이번 프레임 서브스텝 종료
            }
        }
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

    private void ResolveStaticCollision(PhysicsObjectTest obj, Vector2 normal, Vector2 origin)
    {
        if (obj.IsWalkingDominant)
        {
            //obj.TempVelocity = GetModifiedVelocityHorizontal(obj, origin, obj.TempVelocity);
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
                //obj.TempVelocity = GetModifiedVelocityHorizontal(obj, origin, obj.TempVelocity);
            }
            else              // obj == phys, other == walk → obj Impulse
            {
                obj.TempVelocity -= (1f + obj.bounceCoeff) * Vector2.Dot(obj.TempVelocity, normal) * normal;
            }

            if (otherWalking) // other == walk → other slide (use its LastSetDir)
            {
                //otherObj.TempVelocity = GetModifiedVelocityHorizontal(otherObj, otherObj.MovePoint.transform.position, otherObj.TempVelocity);
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

    private void RebuildCellOccupancy()
    {
        cellOccupancy.Clear();
        foreach (var obj in AllPhysicsEntitys)
        {
            if (obj.Mode != MovementMode.Grid) continue;
            // CurrentCell + TargetCell 모두 예약
            cellOccupancy[obj.Grid.CurrentCell] = obj;
            cellOccupancy[obj.Grid.TargetCell]  = obj;
        }
    }

    private PhysicsObjectTest GetCellOccupant(Vector2Int cell)
    {
        // 정적 벽 확인
        Vector2 worldPos = CellToWorld(cell);
        if (OverlapCheckHorizontal(null, worldPos, 0.4f)) return null; // 정적 → null 반환 별도 처리

        cellOccupancy.TryGetValue(cell, out var occupant);
        return occupant;
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
    }*/
    
}