using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class PhysicsManager : MonoBehaviour
{
    // ───────────────────────────────────────────
    //  Inspector 설정
    // ───────────────────────────────────────────
 
    [Header("참조")]
    [SerializeField] private Transform       entityTransform;
    [SerializeField] private IMovable        movable;          // 기존 IMovable 구현체
 
    [Header("격자")]
    [SerializeField] private float           gridSize        = 1f;   // 타일 한 칸 크기
 
    [Header("Z축 (높이)")]
    [SerializeField] private float           gravity         = -9.8f;
    [SerializeField] private float           zColliderHalf   = 0.4f; // Z 충돌 반폭
    /// <summary>ContactFilter2D에 넣을 Z 감지 범위 (minDepth/maxDepth)</summary>
    [SerializeField] private float           zFilterPadding  = 0.5f;
 
    [Header("물리 계수")]
    [SerializeField] private float           frictionCoeff   = 0.85f;   // 바닥 마찰 감속 비율 (per tick)
    [SerializeField] private float           bounceCoeff     = 0.3f;    // 벽 반발 계수
    [SerializeField] private float           airFriction     = 0.98f;   // 공중 감속 비율
    [SerializeField] private float           stopThreshold   = 0.05f;   // 속도 이 이하면 정지
    [SerializeField] private float           stopAccelerateThreshold;
 
    [Header("레이어")]
    [SerializeField] private LayerMask       wallLayer;
    [SerializeField] private LayerMask       floorLayer;
    [SerializeField] private LayerMask       entityLayer;
 
    // ───────────────────────────────────────────
    //  내부 상태
    // ───────────────────────────────────────────
 
    /// <summary>현재 Z 위치 (높이). groundZ 이상.</summary>
    public float ZPosition { get; private set; }
 
    /// <summary>Z축 속도 (위쪽 양수).</summary>
    public float ZVelocity { get; private set; }
 
    /// <summary>현재 바닥에 닿아 있는가.</summary>
    public bool IsGrounded;
 
    // ExForce ID 예약
    private const int GRAVITY_FORCE_ID    = 0;
    private const int GROUND_FORCE_ID     = 1;
    private const int WALL_REACT_FORCE_ID = 2;
 
    // 한 틱에 걸쳐 누적된 "이번 틱 남은 이동 예산" (거리 단위)
    private float moveBudget;
 
    // 이전 틱 엔티티 위치 (이동량 측정용)
    private Vector3 prevEntityPos;
 
    // ContactFilter2D 재사용 (GC 절약)
    private ContactFilter2D contactFilter = new ContactFilter2D();
    private Collider2D[]    overlapBuffer = new Collider2D[16];
 
    // ───────────────────────────────────────────
    //  초기화
    // ───────────────────────────────────────────
 
    private void Awake()
    {
        movable ??= GetComponent<IMovable>();
 
        prevEntityPos = entityTransform.position;
 
        movable.ExForce.SetForce(GRAVITY_FORCE_ID,    Vector3.zero);
        movable.ExForce.SetForce(GROUND_FORCE_ID,     Vector3.zero);
        movable.ExForce.SetForce(WALL_REACT_FORCE_ID, Vector3.zero);
    }
 
    // ───────────────────────────────────────────
    //  main
    // ───────────────────────────────────────────
 
    private void FixedUpdate()
    {
        
        Vector3 totalForce = movable.ExForce.GetSumForce();
        /*bool hasForce      = totalForce != Vector3.zero
                          || movable.Velocity != Vector3.zero
                          || !IsGrounded;
 
        if (!hasForce) return;*/
 
        UpdateZPhysics(totalForce.z);

        Vector2 xyForce = new(totalForce.x, totalForce.y);
        Vector2 xyVel   = new(movable.Velocity.x, movable.Velocity.y);
 
        xyVel += xyForce / movable.Mass * Time.fixedDeltaTime;
 
        moveBudget = xyVel.magnitude;
 
        if (moveBudget <= stopThreshold)
        {
            xyVel = ApplyFriction(xyVel);
            movable.Velocity = new Vector3(xyVel.x, xyVel.y, movable.Velocity.z);
            SnapMovePointToGrid();
            return;
        }
 
        Vector2 moveDir = xyVel.normalized;
        moveDir = PhysicsHorizontalMovement(moveDir, ref xyVel, ref moveBudget);

        xyVel = ApplyFriction(xyVel);

        movable.Velocity = new Vector3(xyVel.x, xyVel.y, movable.Velocity.z);
 
        TrackMovePoint();
 
        prevEntityPos = entityTransform.position;
    }
 
    // ───────────────────────────────────────────
    //  vertical movement
    // ───────────────────────────────────────────
 
    private void UpdateZPhysics(float zForce)
    {
        if (IsGrounded && zForce <= 0f)
        {
            movable.ExForce.SetForce(GROUND_FORCE_ID, Vector3.back * zForce);
            ZVelocity  = 0f;
        }
        else
        {
            // acceleration
            ZVelocity += gravity * Time.fixedDeltaTime;
            ZPosition += ZVelocity * Time.fixedDeltaTime;
            
            int cnt = Physics2D.OverlapCircleNonAlloc(movable.MovePoint.transform.position, 0.4f, overlapBuffer, floorLayer, contactFilter.minDepth -0.01f, contactFilter.maxDepth);
            IsGrounded = cnt > 0;

            if (IsGrounded) // grounded now!
            {
                ZPosition  = overlapBuffer[0].transform.position.z;
                ZVelocity  = -ZVelocity * bounceCoeff;
                if (Mathf.Abs(ZVelocity) < stopThreshold)
                    ZVelocity = 0f;
                if (Mathf.Abs(ZVelocity) > stopAccelerateThreshold)
                    ZVelocity = stopAccelerateThreshold * Mathf.Sign(ZVelocity);
            }
        }
        // check Renderer component's offset!!!!!!!!!!!!!!!!!!!!
        Vector3 ep = entityTransform.position;
        entityTransform.position = new Vector3(ep.x, ep.y, ZPosition);
 
        contactFilter.useDepth  = true;
        contactFilter.minDepth  = ZPosition - zColliderHalf;
        contactFilter.maxDepth  = ZPosition + zColliderHalf;
        contactFilter.useTriggers = false;
    }
 
    // ───────────────────────────────────────────
    //  horizontal movement
    // ───────────────────────────────────────────
 
    private Vector2 PhysicsHorizontalMovement(Vector2 dir, ref Vector2 vel, ref float budget)
    {
        Transform mpTr = movable.MovePoint.transform;
        int safetyLoop = 20;
 
        while (budget > stopThreshold && safetyLoop-- > 0)
        {
            float stepDist = Mathf.Min(budget, gridSize);
 
            Vector2 targetPos = SnapToGrid((Vector2)mpTr.position + dir * stepDist);
 
            RaycastHit2D hit = CastWithFilter((Vector2)mpTr.position, dir, stepDist);
 
            if (!hit)
            {
                mpTr.position = new Vector3(targetPos.x, targetPos.y, ZPosition);
                budget -= stepDist;
            }
            else
            {
                IMovable hitMovable = hit.collider.GetComponentInParent<IMovable>();
                if (hitMovable != null)
                {
                    TransferForce(hitMovable, vel, hit.normal);
                }
 
                Vector2 reflect = Vector2.Reflect(vel, hit.normal) * bounceCoeff;
                vel = reflect;
                dir = vel.normalized;
                budget = vel.magnitude;
 
                movable.ExForce.SetForce(WALL_REACT_FORCE_ID, new Vector3(-vel.x, -vel.y, 0f) * movable.Mass);
 
                dir = GetAvailableDirHorizontal((Vector2)mpTr.position, dir);
 
                if (dir == Vector2.zero)
                    break;
            }
 
            if (budget <= 1f)
                break;
        }
 
        SnapMovePointToGrid();
        return dir;
    }

    private Vector2 GetAvailableDirHorizontal(Vector2 origin, Vector2 dir)
    {
        if (dir == Vector2.zero) return Vector2.zero;
 
        // check if entity can simply go through
        if (!OverlapCheckHorizontal(origin + dir.normalized * gridSize, 0.2f))
        {
            movable.LastSetDir = dir;
            return dir;
        }

        // else, there is something
        Vector2 refDir = dir;
        Vector2 xTilt = (dir.x * Vector2.right).normalized;
        Vector2 yTilt = (dir.y * Vector2.up).normalized;
 
        bool xWalled = dir.x != 0f && OverlapCheckHorizontal(origin + xTilt, 0.4f);
        bool yWalled = dir.y != 0f && OverlapCheckHorizontal(origin + yTilt, 0.4f);
 
        if (xWalled) refDir.x = 0f;
        if (yWalled) refDir.y = 0f;
 
        // if there is no wall at x, y dir, it might mean there is only diagonal wall
        // if so, randomly choose where to go
        if (!xWalled && !yWalled)
        {
            if (OverlapCheckHorizontal(origin + xTilt + yTilt, 0.4f))
                refDir = Random.value > 0.5f ? refDir.x * Vector2.right : refDir.y * Vector2.up;
        }
 
        // seek 45d slides 
        if (refDir == Vector2.zero)
        {
            Vector2 slideUp   = Quaternion.Euler(0, 0,  45) * dir;
            Vector2 slideDown = Quaternion.Euler(0, 0, -45) * dir;
 
            bool canUp   = !OverlapCheckHorizontal(origin + slideUp   * 0.5f, 0.2f);
            bool canDown = !OverlapCheckHorizontal(origin + slideDown  * 0.5f, 0.2f);
 
            if (canUp && canDown)
            {
                refDir = Random.value > 0.5f ? slideUp : slideDown;
                movable.LastSetDir = refDir;
                return refDir;
            }
            if (canUp)   { movable.LastSetDir = slideUp;   return slideUp;   }
            if (canDown) { movable.LastSetDir = slideDown;  return slideDown; }
 
            movable.LastSetDir = dir;
            return Vector2.zero; // completely stops
        }
 
        movable.LastSetDir = refDir;
        return refDir;
    }
 
    // ───────────────────────────────────────────
    //  friction
    // ───────────────────────────────────────────
 
    /// <summary>
    /// 규칙 5: 바닥에 닿아 있으면 마찰, 공중이면 공기저항.
    /// </summary>
    private Vector2 ApplyFriction(Vector2 vel)
    {
        if (IsGrounded)
        {
            // 마찰: 속도에 비례한 반대 방향 감속
            vel *= frictionCoeff;
            if (vel.magnitude < stopThreshold)
                vel = Vector2.zero;
        }
        else
        {
            // 공기저항
            vel *= airFriction;
        }
        return vel;
    }
 
    // ───────────────────────────────────────────
    //  엔티티 → MovePoint 추적
    // ───────────────────────────────────────────
 
    /// <summary>
    /// 엔티티는 자유 위치이므로, MovePoint를 향해 부드럽게 이동.
    /// (보간 속도는 movable.MoveSpeed 등 기존 변수 활용)
    /// </summary>
    private void TrackMovePoint()
    {
        Transform mpTr      = movable.MovePoint.transform;
        Vector3   targetPos = new(mpTr.position.x, mpTr.position.y, ZPosition);
 
        // 기존 IMovable의 이동 속도 변수를 활용한 선형 보간
        entityTransform.position = Vector3.MoveTowards(
            entityTransform.position,
            targetPos,
            movable.Speed * Time.fixedDeltaTime
        );
    }

    public void FollowMovePointIdle()
    {
        Vector3 force = movable.ExForce.GetSumForce();
        if (!force.Equals(Vector3.zero))
        {
            Vector3 refdir = GetAvailableDirHorizontal(entityTransform.position, force.normalized);
            entityTransform.position = Vector3.MoveTowards
                        (
                            entityTransform.position,
                            entityTransform.position + refdir,
                            ((Vector2)force).magnitude * Time.deltaTime
                        );
            movable.MovePoint.transform.position = entityTransform.position;
            SnapMovePointToGrid();
        }
    }
 
    // ───────────────────────────────────────────
    //  힘 전달 (엔티티 간 충돌)
    // ───────────────────────────────────────────
 
    private void TransferForce(IMovable target, Vector2 vel, Vector2 normal)
    {
        Vector3 impulse = (Vector3)vel * movable.Mass;
        target.ExForce.SetForce(entityTransform.GetInstanceID(), impulse); // AddForce: 1틱 적용 후 소멸하는 임시 힘
    }
 
    // ───────────────────────────────────────────
    //  Util
    // ───────────────────────────────────────────
 
    private bool OverlapCheckHorizontal(Vector2 point, float radius)
    {
        int cnt = Physics2D.OverlapCircleNonAlloc(point, radius, overlapBuffer, wallLayer, contactFilter.minDepth, contactFilter.maxDepth);
        for (int i = 0; i < cnt; i++)
        {
            Transform t = overlapBuffer[i].transform;
            if (t == entityTransform) continue;
            if (t == movable.MovePoint.transform) continue;
            return true;
        }
        return false;
    }

 
    /// <summary>ContactFilter2D(Z범위 포함)로 레이캐스트.</summary>
    private RaycastHit2D CastWithFilter(Vector2 origin, Vector2 dir, float dist)
    {
        RaycastHit2D[] hits = new RaycastHit2D[8];
        int cnt = Physics2D.Raycast(origin, dir, contactFilter, hits, dist);
        for (int i = 0; i < cnt; i++)
        {
            if (hits[i].transform == entityTransform) continue;
            if (hits[i].transform == movable.MovePoint.transform) continue;
            return hits[i];
        }
        return default;
    }
 
    /// <summary>위치를 gridSize 격자에 스냅.</summary>
    private Vector2 SnapToGrid(Vector2 pos)
    {
        return new Vector2(
            Mathf.Round(pos.x / gridSize) * gridSize,
            Mathf.Round(pos.y / gridSize) * gridSize
        );
    }
 
    /// <summary>MovePoint를 현재 위치에서 격자 스냅.</summary>
    private void SnapMovePointToGrid()
    {
        Transform mpTr   = movable.MovePoint.transform;
        Vector2   snapped = SnapToGrid(mpTr.position);
        mpTr.position = new Vector3(snapped.x, snapped.y, ZPosition);
    }
 
    // ───────────────────────────────────────────
    //  외부 API
    // ───────────────────────────────────────────
 
    /// <summary>외부에서 Z 방향 즉시 속도 추가 (점프 등).</summary>
    public void AddZImpulse(float zSpeed)
    {
        ZVelocity += zSpeed;
    }
 
    /// <summary>XY 즉시 속도 추가.</summary>
    public void AddXYImpulse(Vector2 vel)
    {
        Vector3 cur = movable.Velocity;
        movable.Velocity = new Vector3(cur.x + vel.x, cur.y + vel.y, cur.z);
    }
 
    /// <summary>현재 Z 위치를 직접 설정 (텔레포트 등).</summary>
    public void SetZ(float z)
    {
    }
}
 