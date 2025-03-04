using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Collision Manager", menuName = "Scriptable Objects/Manager/Collision Manager", order = 3)]
public class CollisionManagerSO : ScriptableObject
{
    public MathManagerSO mathManager;

    public Collider2D CheckWall(Vector3 pos, LayerMask wallLayer)
    {
        return Physics2D.OverlapCircle(pos, 0.4f, wallLayer);
    }

    public Collider2D Detect(Vector3 pos, LayerMask detectLayer, float range)
    {
        return Physics2D.OverlapCircle(pos, detectLayer);
    }

    public Collider2D[] DetectAll(Vector3 pos, LayerMask detectLayer, float range)
    {
        return Physics2D.OverlapCircleAll(pos, detectLayer);
    }

    public Vector2 GetAvailableDir(Vector3 pos, Vector2 dir, LayerMask wallLayer)
    {
        Vector3 xTilt = dir.x * Vector3.one;
        Vector3 yTilt = dir.y * Vector3.one;
        if (CheckWall(pos + xTilt, wallLayer))
        {
            return xTilt;
        }
        else if (CheckWall(pos + yTilt, wallLayer))
        {
            return yTilt;
        }
        else if (CheckWall(pos, wallLayer))
        {
            return Random.Range(0, 2).Equals(0) ? xTilt : yTilt;
        }
        return dir;
    }
}
