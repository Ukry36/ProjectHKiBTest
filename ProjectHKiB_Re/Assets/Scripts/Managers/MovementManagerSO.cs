using System.Collections;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Movement Manager", menuName = "Scriptable Objects/Manager/Movement Manager", order = 4)]
public class MovementManagerSO : ScriptableObject
{
    public MathManagerSO mathManager;
    public CollisionManagerSO collisionManager;

    // Note that MovePoints are always alligned in grid!!

    // Makes entity to follow movepoint
    // for proper movement this should be called every frame
    public void FollowMovePoint(Transform entityTransform, Transform movePointTransform, float speed)
    {
        entityTransform.position = Vector3.MoveTowards(entityTransform.position, movePointTransform.position, speed * Time.deltaTime);
        //entityTransform.transform.position += speed * (movePointTransform.position - entityTransform.position);
    }

    // Knockback!
    public IEnumerator KnockBackCoroutine(Transform entityTransform, Transform movePointTransform, Vector3 dir, int strength, LayerMask wallLayer)
    {
        int instantMoveNum = (int)(strength * 0.8);

        for (int i = 0; i < strength; i++)
        {
            dir = collisionManager.GetAvailableDir(movePointTransform.position, dir, wallLayer);
            if (dir.Equals(Vector2.zero))
                yield break;

            if (i < instantMoveNum)
            {
                entityTransform.position += dir;
                movePointTransform.position += dir;
                yield return null;
            }
            else
            {
                movePointTransform.position += dir;
                yield return new WaitUntil(() => entityTransform.position.Equals(movePointTransform.position));
            }
        }
    }

    // Proceeds movepoint forward(to dir) 
    public void PushMovePoint(Transform movePointTransform, Vector2 dir, LayerMask wallLayer)
    {
        dir = collisionManager.GetAvailableDir(movePointTransform.position, dir, wallLayer);
        movePointTransform.position += (Vector3)dir;
    }

    // Proceeds movepoint forward(to dir) but calculates whenever it should
    public void WalkMove(Transform entityTransform, Transform movePointTransform, Vector2 dir, LayerMask wallLayer)
    {
        dir = mathManager.SetVectorOne(dir);

        Vector3 movePointPos = movePointTransform.position;
        Vector3 entityPos = entityTransform.position;

        if (!dir.x.Equals(0) && !mathManager.Absolute(movePointPos.x - entityPos.x).Equals(0))
            if (mathManager.Absolute(movePointPos.x + dir.x - entityPos.x) > 0.9f)
                dir.x = 0;

        if (!dir.y.Equals(0) && !mathManager.Absolute(movePointPos.y - entityPos.y).Equals(0))
            if (mathManager.Absolute(movePointPos.y + dir.y - entityPos.y) > 0.9f)
                dir.y = 0;

        if (dir.Equals(Vector2.zero)) return;
        PushMovePoint(movePointTransform, dir, wallLayer);
    }

    // Proceeds movepoint forward(to dir) and move instantly
    public void InstantMove(Transform entityTransform, Transform movePointTransform, Vector2 dir, LayerMask wallLayer)
    {
        PushMovePoint(movePointTransform, dir, wallLayer);
        entityTransform.position = movePointTransform.position;
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
