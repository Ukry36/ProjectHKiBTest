using System.Collections.Generic;
using UnityEngine;

public abstract class ZCollider2D : MonoBehaviour
{
    public float zCenter;
    public float height = 1f;

    [NaughtyAttributes.HideIf("useSlopeRL")]
    public bool useSlopeDU;
    [NaughtyAttributes.ShowIf("useSlopeDU")]
    public float upMostOffset;
    [NaughtyAttributes.ShowIf("useSlopeDU")]
    public float downMostOffset;

    [NaughtyAttributes.HideIf("useSlopeDU")]
    public bool useSlopeRL;
    [NaughtyAttributes.ShowIf("useSlopeRL")]
    public float leftMostOffset;
    [NaughtyAttributes.ShowIf("useSlopeRL")]
    public float rightMostOffset;
    

    public float ZMin => transform.position.z + zCenter - height / 2f;
    public float ZMax => transform.position.z + zCenter + height / 2f;

    public abstract float Zmin(Vector2 horizontalPos);
    public abstract float Zmax(Vector2 horizontalPos);

    protected abstract Collider2D Col { get; }

#if UNITY_EDITOR
    public static bool UseZAxis; // false = 2D, true = 3D
    public static int ZCoeff = 1; // 
    public static bool local = true;
    
    [NaughtyAttributes.Button]
    public void ToggleGizmoDimension()
    {
        UseZAxis = !UseZAxis;
    }

    [NaughtyAttributes.Button]
    public void InvertGizmoZ()
    {
        ZCoeff = -ZCoeff;
    }

    [NaughtyAttributes.Button]
    public void GizmoAllShowToggle() => local = !local;
    protected abstract void DrawGizmo();

    private void OnDrawGizmosSelected()
    {
        if (Col == null || !local) return;
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.9f);
        DrawGizmo();
    }

    private void OnDrawGizmos()
    {
        if (Col == null || local) return;
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.9f);
        DrawGizmo();
    }

    protected static void DrawArc(Vector3 c, float r, float fromDeg, float toDeg, Vector3 right, Vector3 up, int n = 16)
    {
        float step = (toDeg - fromDeg) / n;
        for (int i = 0; i < n; i++)
        {
            float a0 = (fromDeg + step * i) * Mathf.Deg2Rad;
            float a1 = (fromDeg + step * (i + 1)) * Mathf.Deg2Rad;
            Gizmos.DrawLine(
                c + (right * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * r,
                c + (right * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * r);
        }
    }

    protected static void DrawCircleXY(Vector3 c, float r, int n = 32) =>
        DrawArc(c, r, 0, 360, Vector3.right, Vector3.up, n);

    protected static void DrawCylinder3D(Vector3 c, float r, float zMin, float zMax)
    {
        DrawCircleXY(new Vector3(c.x, c.y, zMin), r);
        DrawCircleXY(new Vector3(c.x, c.y, zMax), r);
        for (int i = 0; i < 4; i++)
        {
            float a = Mathf.PI * 0.5f * i;
            var o = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * r;
            Gizmos.DrawLine(new Vector3(c.x, c.y, zMin) + o, new Vector3(c.x, c.y, zMax) + o);
        }
    }

    protected static void DrawCapsuleXY(Vector3 c, Vector2 size, CapsuleDirection2D dir, float z)
    {
        bool vert = dir == CapsuleDirection2D.Vertical;
        Vector3 bodyDir = vert ? Vector3.up : Vector3.right;
        Vector3 perpDir = vert ? Vector3.right : Vector3.up;
        float r        = (vert ? size.x : size.y) / 2f;
        float halfBody = Mathf.Max(0f, (vert ? size.y : size.x) / 2f - r);

        Vector3 b = new Vector3(c.x, c.y, z);
        Vector3 cap1 = b + bodyDir * halfBody;
        Vector3 cap2 = b - bodyDir * halfBody;

        DrawArc(cap1, r,   0, 180, perpDir, bodyDir, 16);
        DrawArc(cap2, r, 180, 360, perpDir, bodyDir, 16);
        Gizmos.DrawLine(cap1 + perpDir * r, cap2 + perpDir * r);
        Gizmos.DrawLine(cap1 - perpDir * r, cap2 - perpDir * r);
    }

    protected static void DrawPolygonAtZ(Vector2[] pts, Vector3 origin, float z)
    {
        if (pts == null || pts.Length < 2) return;
        for (int i = 0; i < pts.Length; i++)
        {
            int j = (i + 1) % pts.Length;
            Gizmos.DrawLine(origin + new Vector3(pts[i].x, pts[i].y, z),
                            origin + new Vector3(pts[j].x, pts[j].y, z));
        }
    }

    protected static void DrawExtrudedPolygon3D(Vector2[] pts, Vector3 origin, float zMin, float zMax)
    {
        DrawPolygonAtZ(pts, origin, zMin);
        DrawPolygonAtZ(pts, origin, zMax);
        foreach (var p in pts)
            Gizmos.DrawLine(origin + new Vector3(p.x, p.y, zMin), origin + new Vector3(p.x, p.y, zMax));
    }

    protected static void DrawExtrudedPolygon2D(Vector2[] pts, Vector3 o, float zMin, float zMax)
    {
        if (pts == null || pts.Length < 2) return;
        DrawPolygonAtZ(pts, o + Vector3.up * zMin, 0);
        DrawPolygonAtZ(pts, o + Vector3.up * zMax, 0);
        foreach (var p in pts)
            Gizmos.DrawLine(new Vector3(o.x + p.x, o.y + p.y + zMin, 0), new Vector3(o.x + p.x, o.y + p.y + zMax, 0));
    }

    protected static void DrawEdgeAtZ(Vector2[] pts, Vector3 origin, float z)
    {
        if (pts == null || pts.Length < 2) return;
        for (int i = 0; i < pts.Length - 1; i++)
            Gizmos.DrawLine(origin + new Vector3(pts[i].x, pts[i].y, z),
                            origin + new Vector3(pts[i + 1].x, pts[i + 1].y, z));
    }

    protected static void DrawExtrudedEdge3D(Vector2[] pts, Vector3 origin, float zMin, float zMax)
    {
        DrawEdgeAtZ(pts, origin, zMin);
        DrawEdgeAtZ(pts, origin, zMax);
        foreach (var p in pts)
            Gizmos.DrawLine(origin + new Vector3(p.x, p.y, zMin), origin + new Vector3(p.x, p.y, zMax));
    }

    protected static void DrawExtrudedEdge2D(Vector2[] pts, Vector2 o, float zMin, float zMax)
    {
        if (pts == null || pts.Length < 2) return;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Gizmos.DrawLine(new Vector3(o.x + pts[i].x, o.y + pts[i].y + zMin, 0), new Vector3(o.x + pts[i + 1].x, o.y + pts[i+1].y + zMin, 0));
            Gizmos.DrawLine(new Vector3(o.x + pts[i].x, o.y + pts[i].y + zMax, 0), new Vector3(o.x + pts[i + 1].x, o.y + pts[i+1].y + zMax, 0));
            Gizmos.DrawLine(new Vector3(o.x + pts[i].x, o.y + pts[i].y + zMin, 0), new Vector3(o.x + pts[i].x,     o.y + pts[i].y + zMax, 0));
        }
        float lx = o.x + pts[^1].x;
        Gizmos.DrawLine(new Vector3(lx, o.y + zMin, 0), new Vector3(lx, o.y + zMax, 0));
    }

#endif


    public bool OverlapsZ(ZCollider2D other) => ZMin < other.ZMax && ZMax > other.ZMin;
    public bool OverlapsZ(Collider2D other) => ZMin < other.transform.position.z && ZMax > other.transform.position.z;

    public int OverlapCollider(ContactFilter2D filter, Collider2D[] results)
    {
        int count = Col.OverlapCollider(filter, results);
        int validCount = 0;
        for (int i = 0; i < count; i++)
        {
            if (ZPhysics2D.TryGet(results[i], out var other))
            {
                if (!OverlapsZ(other)) continue;
            }
            else if (!OverlapsZ(Col)) continue;
            
            results[validCount++] = results[i];
        }
        return validCount;
    }

    public int OverlapCollider(ContactFilter2D filter, List<Collider2D> results)
    {
        int count = Col.OverlapCollider(filter, results);
        int validCount = 0;
        for (int i = 0; i < count; i++)
        {
            if (!ZPhysics2D.TryGet(results[i], out var other)) continue;
            if (!OverlapsZ(other)) continue;
            results[validCount++] = results[i];
        }
        results.RemoveRange(validCount, count - validCount);
        return validCount;
    }

    protected virtual void Awake()   => ZPhysics2D.Register(Col, this);
    protected virtual void OnDestroy() => ZPhysics2D.Unregister(Col);
}