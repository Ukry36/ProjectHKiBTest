using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Movement Manager", menuName = "Scriptable Objects/Manager/Movement Manager", order = 4)]
public class MovementManagerSO : ScriptableObject
{
    public MathManagerSO mathManager;
    public CollisionManagerSO collisionManager;
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
            Vector3 refdir = collisionManager.GetAvailableDir(movable.MovePoint.transform.position, force.normalized, movable.WallLayer);
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

    public delegate void KnockBackEnded();
    private readonly WaitForSeconds knockBackWaitTime = new WaitForSeconds(0.08f);

    // Knockback!
    public IEnumerator KnockBackCoroutine(Transform entityTransform, IMovable movable, Vector3 dir, float strength, float mass, KnockBackEnded KnockBackEnded)
    {
        strength -= mass;
        Debug.Log("started, (" + dir.x + ", " + dir.y + "), " + strength);
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
                dir = collisionManager.GetAvailableDir(movepointTransform.position, dir, movable.WallLayer);
                if (dir == Vector3.zero)
                    break;
                movable.ExForce.SetForce[ID] = 10 * strength * dir;
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
            dir = collisionManager.GetAvailableDir(movepointTransform.position, dir, movable.WallLayer);
            movable.ExForce.SetForce[ID] = strength * dir;
            yield return null;
        }
        KnockBackChainReaction(entityTransform, movepointTransform, storedDir, strength * 0.75f + mass, movable.CanPushLayer);
        KnockBackEnded?.Invoke();
        movable.ExForce.SetForce[ID] = Vector3.zero;
        entityTransform.position = movepointTransform.position;
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
                GameManager.instance.damageParticleManager.PlayHitParticle(KnockBackChainReactionParticle, 0, false, false, component.MovePoint.transform, 0);
            }
        }
    }

    public void EndKnockbackEarly(Transform entityTransform, IMovable movable)
    {
        Debug.Log("canceled");
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
            refDir = collisionManager.GetAvailableDir(movepointTransform.position, dir, movable.WallLayer);
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
                dir = collisionManager.GetAvailableDir(movepointTransform.position, dir, movable.WallLayer);
                movable.ExForce.SetForce[ID] = speed * block * dir;
                yield return null;
                progress += Vector3.Distance(prevPos, entityTransform.position);
            }
        }
    }

    // Proceeds movepoint forward(to dir) 
    public void PushMovePoint(Transform movePointTransform, Vector2 dir, LayerMask wallLayer)
    {
        dir = collisionManager.GetAvailableDir(movePointTransform.position, dir, wallLayer);
        movePointTransform.position += (Vector3)dir;
        AllignMovePoint(movePointTransform);
    }

    // Proceeds movepoint forward(to dir) but calculates whenever it should
    public void WalkMove(Transform entityTransform, IMovable movable, Vector3 dir)
    {
        float speed = movable.IsSprinting ? movable.Speed.Value * movable.SprintCoeff.Value : movable.Speed.Value;
        dir = mathManager.SetDirection8One(dir).normalized;
        Vector3 refdir = speed * dir + movable.ExForce.GetForce;
        if (dir.Equals(Vector3.zero))
            speed += refdir.magnitude;
        else
            speed = refdir.magnitude;

        refdir = mathManager.SetDirection8One(refdir);
        Vector3 movePointPos = movable.MovePoint.transform.position;
        Vector3 entityPos = entityTransform.position;

        if (!refdir.x.Equals(0) && !mathManager.Absolute(movePointPos.x - entityPos.x).Equals(0))
            if (mathManager.Absolute(movePointPos.x + refdir.x - entityPos.x) > 0.9f)
                refdir.x = 0;

        if (!refdir.y.Equals(0) && !mathManager.Absolute(movePointPos.y - entityPos.y).Equals(0))
            if (mathManager.Absolute(movePointPos.y + refdir.y - entityPos.y) > 0.9f)
                refdir.y = 0;

        if (!refdir.Equals(Vector2.zero))
            PushMovePoint(movable.MovePoint.transform, refdir, movable.WallLayer);

        entityTransform.position = Vector3.MoveTowards(entityTransform.position, movable.MovePoint.transform.position, speed * Time.deltaTime);
    }


    // Proceeds movepoint forward(to dir) and move instantly
    public void InstantMove(Transform entityTransform, IMovable movable, Vector2 dir)
    {
        dir = dir.normalized;
        PushMovePoint(movable.MovePoint.transform, dir, movable.WallLayer);
        entityTransform.position = movable.MovePoint.transform.position;
    }

    // Alligns movepoint if it is out of grid
    public void AllignMovePoint(Transform movePointTransform)
    {
        movePointTransform.position = mathManager.AllignInGrid(movePointTransform.position);
    }

    public void StairAdjust(Transform entityTransform, Transform movePointTransform)
    {
        //maybe deleted later
    }
}
