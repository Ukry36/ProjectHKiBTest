using UnityEngine;

[CreateAssetMenu(fileName = "Collision Manager", menuName = "Scriptable Objects/Manager/Collision Manager", order = 3)]
public class CollisionManagerSO : ScriptableObject
{
    public MathManagerSO mathManager;
    public LayerMask floorInfoProviderLayer;

    public Vector2 GetAvailableDir(Vector3 pos, Vector2 dir, LayerMask wallLayer)
    {
        Vector3 xTilt = (dir.x * Vector3.right).normalized;
        Vector3 yTilt = (dir.y * Vector3.up).normalized;

        if (!dir.x.Equals(0))
            if (Physics2D.OverlapCircle(pos + xTilt, 0.4f, wallLayer))
                dir.x = 0;

        if (!dir.y.Equals(0))
            if (Physics2D.OverlapCircle(pos + yTilt, 0.4f, wallLayer))
                dir.y = 0;

        if (!dir.x.Equals(0) && !dir.y.Equals(0))
            if (Physics2D.OverlapCircle(pos + xTilt + yTilt, 0.4f, wallLayer))
                if (Random.Range(0, 2).Equals(0))
                    dir.x = 0;
                else
                    dir.y = 0;
        return dir;
    }

    //low accuracy
    public Vector3 GetNearestPos(Vector3 pos, Vector3 dir, float distance, LayerMask wallLayer)
    {
        dir = dir.normalized;
        RaycastHit2D hit = Physics2D.Raycast(pos, dir, distance, wallLayer);
        if (!hit)
        {
            return pos + dir * distance;
        }
        return hit.point;
    }
}
