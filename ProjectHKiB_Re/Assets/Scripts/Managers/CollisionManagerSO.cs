using UnityEngine;

[CreateAssetMenu(fileName = "Collision Manager", menuName = "Scriptable Objects/Manager/Collision Manager", order = 3)]
public class CollisionManagerSO : ScriptableObject
{
    public MathManagerSO mathManager;
    public LayerMask floorInfoProviderLayer;

    public Collider2D CheckWall(Vector3 pos, LayerMask wallLayer)
    {
        return Physics2D.OverlapCircle(pos, 0.4f, wallLayer);
    }

    public Collider2D Detect(Vector3 pos, LayerMask detectLayer, float range)
    {
        return Physics2D.OverlapCircle(pos, range, detectLayer);
    }

    public Collider2D[] DetectAll(Vector3 pos, LayerMask detectLayer, float range)
    {
        return Physics2D.OverlapCircleAll(pos, range, detectLayer);
    }

    public Vector2 GetAvailableDir(Vector3 pos, Vector2 dir, LayerMask wallLayer)
    {
        Vector3 xTilt = (dir.x * Vector3.right).normalized;
        Vector3 yTilt = (dir.y * Vector3.up).normalized;

        if (!dir.x.Equals(0) && !dir.y.Equals(0))
            if (CheckWall(pos + xTilt + yTilt, wallLayer))
                if (Random.Range(0, 2).Equals(0))
                    dir.x = 0;
                else
                    dir.y = 0;

        if (!dir.x.Equals(0))
            if (CheckWall(pos + xTilt, wallLayer))
                dir.x = 0;

        if (!dir.y.Equals(0))
            if (CheckWall(pos + yTilt, wallLayer))
                dir.y = 0;
        return dir;
    }
}
