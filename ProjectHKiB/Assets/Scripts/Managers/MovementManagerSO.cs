using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Movement Manager", menuName = "Scriptable Objects/Manager/Movement Manager", order = 2)]
public class MovementManager : ScriptableObject
{
    public MathManagerSO mathManager;
    public CollisionManagerSO collisionManager;

    // Note that MovePoints are always alligned in grid!!
    public void FollowMovePoint(Transform entityTransform, Transform movePointTransform, float speed)
    {
        entityTransform.DOMove(movePointTransform.position, speed);
    }

    public IEnumerator KnockBackCoroutine(Transform entityTransform, Transform movePointTransform, Vector2 dir, int strength, LayerMask wallLayer)
    {
        int instantMoveNum = (int)(strength * 0.8);

        for (int i = 0; i < strength; i++)
        {
            dir = collisionManager.GetAvailableDir(movePointTransform.position, dir, wallLayer);
            if (dir.Equals(Vector2.zero))
            {
                yield break;
            }
            if (i < instantMoveNum)
                entityTransform.position += (Vector3)dir;
            movePointTransform.position += (Vector3)dir;
            yield return null;
        }
    }



    public void WalkMove(Transform entityTransform, Transform movePointTransform, Vector2 dir, LayerMask wallLayer)
    {
        float horizontal = dir.x;
        float vertical = dir.y;

        Vector3 movePointPos = movePointTransform.position;
        Vector3 entityPos = entityTransform.position;
        Vector3 destination = movePointPos;
        float tilt;

        if (horizontal != 0)
        {
            tilt = mathManager.Absolute(movePointPos.x - entityPos.x);
            if (tilt < 0.1f)
            {
                destination.x = movePointPos.x + horizontal;
            }
            else if (tilt < 0.9f)
            {
                destination.x = horizontal > 0 ?
                mathManager.Ceiling(entityPos.x) : mathManager.Floor(entityPos.x);
            }
            if (!Equals(destination.x, movePointPos.x))
                if (!collisionManager.CheckWall(destination, wallLayer))
                    movePointPos = destination;
        }

        if (vertical != 0)
        {
            tilt = mathManager.Absolute(movePointPos.y - entityPos.y);
            if (tilt < 0.1f)
            {
                destination.y = movePointPos.y + vertical;
            }
            else if (tilt < 0.9f)
            {
                destination.y = vertical > 0 ?
                mathManager.Ceiling(entityPos.y) : mathManager.Floor(entityPos.y);
            }
            if (!Equals(destination.y, movePointPos.y))
                if (!collisionManager.CheckWall(destination, wallLayer))
                    movePointPos = destination;
        }

        movePointTransform.position = movePointPos;
    }

    public void PushMovePoint(Transform movePointTransform, Vector2 dir, LayerMask wallLayer)
    {
        dir = collisionManager.GetAvailableDir(movePointTransform.position, dir, wallLayer);
        movePointTransform.position += (Vector3)dir;
    }

    public void InstantMove(Transform entityTransform, Transform movePointTransform, Vector2 dir, LayerMask wallLayer)
    {
        PushMovePoint(movePointTransform, dir, wallLayer);
        entityTransform.position = movePointTransform.position;
    }

    public void AllignMovePoint(Transform movePointTransform)
    {
        movePointTransform.position = mathManager.AllignInGrid(movePointTransform.position);
    }

    public void StairAdjust(Transform entityTransform, Transform movePointTransform)
    {
        //maybe deleted later
    }
}
