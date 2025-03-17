using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Movement Manager", menuName = "Scriptable Objects/Manager/Movement Manager", order = 4)]
public class MovementManagerSO : ScriptableObject
{
    public MathManagerSO mathManager;
    public CollisionManagerSO collisionManager;

    // Note that MovePoints are always alligned in grid!!
    public void FollowMovePoint(Transform entityTransform, Transform movePointTransform, float speed)
    {
        entityTransform.position = Vector3.MoveTowards(entityTransform.position, movePointTransform.position, speed * Time.deltaTime);
        //entityTransform.transform.position += speed * (movePointTransform.position - entityTransform.position);
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
        dir = (dir.x < 0 ? Vector3.left : dir.x > 0 ? Vector3.right : Vector3.zero)
            + (dir.y < 0 ? Vector3.down : dir.y > 0 ? Vector3.up : Vector3.zero);

        Vector3 movePointPos = movePointTransform.position;
        Vector3 entityPos = entityTransform.position;

        if (!mathManager.Absolute(movePointPos.x - entityPos.x).Equals(0))
            if (mathManager.Absolute(movePointPos.x + dir.x - entityPos.x) > 0.9f)
                dir.x = 0;

        if (!mathManager.Absolute(movePointPos.y - entityPos.y).Equals(0))
            if (mathManager.Absolute(movePointPos.y + dir.y - entityPos.y) > 0.9f)
                dir.y = 0;

        if (dir.Equals(Vector2.zero)) return;

        dir = collisionManager.GetAvailableDir(movePointPos, dir, wallLayer);
        movePointTransform.position += (Vector3)dir;
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
