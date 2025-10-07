using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Movement Manager", menuName = "Scriptable Objects/Manager/Movement Manager", order = 4)]
public class MovementManagerSO : ScriptableObject
{
    public MathManagerSO mathManager;
    public DamageParticleDataSO KnockBackChainReactionParticle;

    // Note that MovePoints are always alligned in grid!!
    // Makes entity to follow movepoint
    // for proper movement this should be called every frame
    private const int KNOCKBACKFORCEID = 1;

    public void FollowMovePointIdle(Transform entityTransform, IMovable movable)
    {
        Vector3 force = movable.ExForce.GetForce;
        if (!force.Equals(Vector3.zero))
        {
            Vector3 refdir = GetAvailableDir(entityTransform, movable, force.normalized, movable.WallLayer);
            entityTransform.position = Vector3.MoveTowards
                        (
                            entityTransform.position,
                            entityTransform.position + refdir,
                            force.magnitude * Time.deltaTime
                        );
            movable.MovePoint.transform.position = entityTransform.position;
            AllignMovePoint(movable.MovePoint.transform);
        }
    }

    public void MoveRaw(Transform entityTransform, Vector3 dir, float speed)
    {
        entityTransform.position += speed * Time.deltaTime * dir;
    }

    private readonly WaitForSeconds knockBackWaitTime = new(0.08f);

    // Knockback!
    public IEnumerator KnockBackCoroutine(Transform entityTransform, IMovable movable, Vector3 dir, float strength, float mass, Action KnockBackEnded)
    {
        strength -= mass;
        //Debug.Log("started, (" + dir.x + ", " + dir.y + "), " + strength);
        yield return knockBackWaitTime;
        dir = dir.normalized;
        Vector3 storedDir = dir;
        int ID = this.GetInstanceID() * KNOCKBACKFORCEID;
        Transform movepointTransform = movable.MovePoint.transform;
        float targetProgress = 0;
        float progress = 0;
        Vector3 prevPos;
        for (int i = 0; i < strength; i++)
        {
            targetProgress += dir.magnitude;
            while (progress < targetProgress)
            {
                prevPos = entityTransform.position;
                storedDir = dir;
                dir = GetAvailableDir(entityTransform, movable, dir, movable.WallLayer);
                if (dir == Vector3.zero)
                    break;
                movable.ExForce.SetForce[ID] = 10 * strength * dir;
                //Debug.Log(movable.ExForce.GetForce);
                yield return null;
                progress += Vector3.Distance(prevPos, entityTransform.position);
            }
            if (dir == Vector3.zero)
                break;
        }

        for (int i = (int)(0.5f / Time.deltaTime); i > 0; i--)
        {
            if (dir == Vector3.zero)
                break;
            storedDir = dir;
            strength = strength * 2 * i * Time.deltaTime;
            dir = GetAvailableDir(entityTransform, movable, dir, movable.WallLayer);
            movable.ExForce.SetForce[ID] = strength * dir;
            yield return null;
        }
        KnockBackChainReaction(entityTransform, movepointTransform, storedDir, strength * 0.75f + mass, movable.CanPushLayer);
        KnockBackEnded?.Invoke();
        movable.ExForce.SetForce[ID] = Vector3.zero;
        entityTransform.position = movepointTransform.position;
        Debug.Log("ended!");
    }

    public void KnockBackChainReaction(Transform entityTransform, Transform movePointTransform, Vector3 dir, float strength, LayerMask canPushLayer)
    {
        Collider2D col = Physics2D.OverlapPoint(movePointTransform.position + dir.normalized, canPushLayer);
        if (col && col.transform != entityTransform)
        {
            if (col.transform.TryGetComponent(out IMovable component))
            {
                Debug.DrawLine(component.MovePoint.transform.position, component.MovePoint.transform.position + dir, Color.red, 0.5f);
                component.KnockBack(dir /*- entityTransform.position + component.MovePoint.transform.position*/, strength);
                GameManager.instance.damageParticleManager.PlayHitParticle(KnockBackChainReactionParticle, 0, false, false, component.MovePoint.transform.position, 0);
            }
        }
    }

    public void EndKnockbackEarly(Transform entityTransform, IMovable movable)
    {
        //Debug.Log("canceled");
        movable.ExForce.SetForce[this.GetInstanceID() * KNOCKBACKFORCEID] = Vector3.zero;
        entityTransform.position = movable.MovePoint.transform.position;
    }

    /*
    public void AttackMove(Transform entityTransform, IMovable movable, Transform targetTransform, float maxDistance)
    {
        Vector3 dir = (targetTransform.position - entityTransform.position).normalized;
        float distance = maxDistance;
        Vector3 targetPos = collisionManager.GetNearestPos(entityTransform.position, dir, distance, movable.WallLayer);

        Transform movepointTransform = movable.MovePoint.transform;
        movepointTransform.position = targetPos - 0.5f * dir;
        AllignMovePoint(movepointTransform);
        entityTransform.position = movepointTransform.position;
    }*/

    public void AttackMove(Transform entityTransform, IMovable movable, Vector3 targetPos, float maxDistance)
    {
        Vector3 dir = (targetPos - entityTransform.position).normalized;
        float distance = Vector3.Distance(targetPos, entityTransform.position);
        distance = distance > maxDistance ? maxDistance : distance;

        Transform movepointTransform = movable.MovePoint.transform;
        Vector3 refDir;
        for (float i = 0; i < distance; i += refDir.magnitude)
        {
            refDir = GetAvailableDir(entityTransform, movable, dir, movable.WallLayer);
            if (refDir.Equals(Vector3.zero))
            {
                AllignMovePoint(movepointTransform);
                entityTransform.position = movepointTransform.position;
                return;
            }
            movepointTransform.position += refDir;
        }
        AllignMovePoint(movepointTransform);
        entityTransform.position = movepointTransform.position;
    }

    public IEnumerator PushApproxCoroutine(Transform entityTransform, IMovable movable, Vector3 dir, int block, float speed)
    {
        dir = dir.normalized;
        int ID = this.GetInstanceID();
        Transform movepointTransform = movable.MovePoint.transform;
        float targetProgress = 0;
        float progress = 0;
        Vector3 prevPos;
        for (int i = 0; i < block; i++)
        {
            targetProgress += dir.magnitude;
            while (progress < targetProgress)
            {
                prevPos = entityTransform.position;
                dir = GetAvailableDir(entityTransform, movable, dir, movable.WallLayer);
                movable.ExForce.SetForce[ID] = speed * block * dir;
                yield return null;
                progress += Vector3.Distance(prevPos, entityTransform.position);
            }
        }
    }

    // Proceeds movepoint forward(to dir) but calculates whenever it should
    public void WalkMove(Transform entityTransform, IMovable movable, float speed, Vector3 dir, LayerMask wallLayer)
    {
        if (movable.IsSprinting) speed *= movable.SprintCoeff;
        Transform movePointTransform = movable.MovePoint.transform;
        dir = dir.normalized;
        Vector3 refDir = speed * dir + movable.ExForce.GetForce;
        if (dir == Vector3.zero)
            speed += refDir.magnitude;
        else
            speed = refDir.magnitude;

        refDir = mathManager.SetDirection8One(refDir); // Direction where i want to go

        Vector3 movePointPos = movePointTransform.position;
        Vector3 entityPos = entityTransform.position;

        float xProgress = 1 - mathManager.Absolute(movePointPos.x - entityPos.x); // 0 to 1 also 1 when stop
        float yProgress = 1 - mathManager.Absolute(movePointPos.y - entityPos.y);
        bool xReversed = (movePointPos.x - entityPos.x) * refDir.x < 0;
        bool yReversed = (movePointPos.y - entityPos.y) * refDir.y < 0;

        bool proceedMovepoint = false;
        if (xProgress == 1 && yProgress == 1) proceedMovepoint = true;
        if (xReversed && xProgress < 0.9f) proceedMovepoint = true;
        if (yReversed && yProgress < 0.9f) proceedMovepoint = true;

        if (proceedMovepoint)
        {
            refDir = GetAvailableDir(entityTransform, movable, refDir, wallLayer);
            movePointTransform.position += refDir;
            AllignMovePoint(movePointTransform);
        }

        entityTransform.position = Vector3.MoveTowards(entityTransform.position, movable.MovePoint.transform.position, speed * Time.deltaTime);
    }


    // Proceeds movepoint forward(to dir) and move instantly
    public void InstantMove(Transform entityTransform, IMovable movable, Vector2 dir)
    {
        Transform movePointTransform = movable.MovePoint.transform;
        dir = mathManager.SetDirection8One(dir).normalized;
        dir = GetAvailableDir(entityTransform, movable, dir, movable.WallLayer);
        movePointTransform.position += (Vector3)dir;
        AllignMovePoint(movePointTransform);
        entityTransform.position = movePointTransform.position;
    }

    // Alligns movepoint if it is out of grid
    public void AllignMovePoint(Transform movePointTransform)
    {
        movePointTransform.position = mathManager.AllignInGrid(movePointTransform.position);
    }

    public Vector2 GetAvailableDir(Transform entityTransform, IMovable movable, Vector2 dir, LayerMask wallLayer)
    {
        Transform movePointTransform = movable.MovePoint.transform;
        Vector2 refDir = dir;
        bool canSlideUp = true;
        bool canSlideDown = true;
        bool canGoSimply = true;
        Collider2D[] colliders = new Collider2D[10];

        // if there is no wall at dir simply go to that dir
        int colLen = Physics2D.OverlapCircleNonAlloc(movePointTransform.position + (Vector3)dir, 0.2f, colliders, wallLayer);
        for (int i = 0; i < colLen; i++)
        {
            if (colliders[i].transform == entityTransform) continue;
            else if (colliders[i].transform == movePointTransform) continue;
            else canGoSimply = false;
        }
        if (canGoSimply)
        {
            movable.LastSetDir = dir;
            return dir;
        }

        Vector3 xTilt = (dir.x * Vector3.right).normalized;
        Vector3 yTilt = (dir.y * Vector3.up).normalized;

        // if x dir walled
        if (dir.x != 0)
            if (Physics2D.OverlapCircle(movePointTransform.position + xTilt, 0.4f, wallLayer))
                refDir.x = 0; // dirx = 0

        // if y dir walled, dir.y = 0
        if (dir.y != 0)
            if (Physics2D.OverlapCircle(movePointTransform.position + yTilt, 0.4f, wallLayer))
                refDir.y = 0;

        // if two dir was clear, check diagonal
        if (refDir.x != 0 && refDir.y != 0)
            if (Physics2D.OverlapCircle(movePointTransform.position + xTilt + yTilt, 0.4f, wallLayer))
                if (UnityEngine.Random.value > 0.5f)
                    refDir.x = 0;
                else
                    refDir.y = 0;

        // if two dir was walled, check diagonal
        if (refDir == Vector2.zero)
        {
            colLen = Physics2D.OverlapCircleNonAlloc(movePointTransform.position + Quaternion.Euler(0, 0, 45) * dir * 0.5f, 0.2f, colliders, wallLayer);
            for (int i = 0; i < colLen; i++)
            {
                if (colliders[i].transform == entityTransform) continue;
                else if (colliders[i].transform == movePointTransform) continue;
                else { canSlideUp = false; break; }
            }
            colLen = Physics2D.OverlapCircleNonAlloc(movePointTransform.position + Quaternion.Euler(0, 0, -45) * dir * 0.5f, 0.2f, colliders, wallLayer);
            for (int i = 0; i < colLen; i++)
            {
                if (colliders[i].transform == entityTransform) continue;
                else if (colliders[i].transform == movePointTransform) continue;
                else { canSlideDown = false; break; }
            }

            if (canSlideDown && canSlideUp)
            {
                refDir = UnityEngine.Random.value > 0.5f ? Quaternion.Euler(0, 0, 45) * dir : Quaternion.Euler(0, 0, -45) * dir;
                movable.LastSetDir = refDir;
                return refDir;
            }

            if (canSlideUp)
            {
                refDir = Quaternion.Euler(0, 0, 45) * dir;
                movable.LastSetDir = refDir;
                return refDir;
            }

            if (canSlideDown)
            {
                refDir = Quaternion.Euler(0, 0, -45) * dir;
                movable.LastSetDir = refDir;
                return refDir;
            }
            movable.LastSetDir = dir;
        }
        else movable.LastSetDir = refDir;
        return refDir;
    }

    public void InitialDodgeMove(Transform entityTransform, IMovable movable, IDodgeable dodgeable, Vector3 dir)
    {
        dir = dir.normalized;
        Transform movePointTransform = movable.MovePoint.transform;
        for (float i = dodgeable.InitialDodgeMaxDistance; i >= 0; i--)
            if (!Physics2D.OverlapCircle(movePointTransform.position + dir * i, 0.2f, movable.WallLayer))
            {
                movePointTransform.position += dir * i;
                break;
            }

        AllignMovePoint(movePointTransform);
        entityTransform.position = movePointTransform.position;
    }

    public void ProceedMovePoint(Transform entityTransform, IMovable movable, Vector3 dir)
    {
        dir = GetAvailableDir(entityTransform, movable, dir.normalized, movable.WallLayer);
        movable.MovePoint.transform.position += dir;
        AllignMovePoint(movable.MovePoint.transform);
    }
}
