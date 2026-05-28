using System.Collections.Generic;
using UnityEngine;

public abstract class ZCollider2D : MonoBehaviour
{
    public float zCenter;
    public float height = 1f;

    
    public float frictionCoeff   = 0.85f;
    public float bounceCoeff     = 0.3f;  

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

    public bool isStair;

    public float ZMin => transform.position.z + zCenter - height / 2f;
    public float ZMax => transform.position.z + zCenter + height / 2f;

    public virtual float Zmin(Vector2 origin) => ZMin + SlopeOffset(origin);
    public virtual float Zmax(Vector2 origin) => ZMax + SlopeOffset(origin);
    public virtual float ZminCircle(Vector2 origin, float radius)
    {
        float min = float.MaxValue;
        var samples = ZPhysics2D.CircleSamplePoints(origin, radius);
        foreach (var p in samples) min = Mathf.Min(min, Zmin(p));
        return min;
    }
    public virtual float ZmaxCircle(Vector2 origin, float radius)
    {
        float max = float.MinValue;
        var samples = ZPhysics2D.CircleSamplePoints(origin, radius);
        foreach (var p in samples) max = Mathf.Max(max, Zmax(p));
        return max;
    }
    public virtual float ZminBox(Vector2 origin, Vector2 size, float angle)
    {
        float min = float.MaxValue;
        var samples = ZPhysics2D.BoxSamplePoints(origin, size, angle);
        foreach (var p in samples) min = Mathf.Min(min, Zmin(p));
        return min;
    }
    public virtual float ZmaxBox(Vector2 origin, Vector2 size, float angle)
    {
        float max = float.MinValue;
        var samples = ZPhysics2D.BoxSamplePoints(origin, size, angle);
        foreach (var p in samples) max = Mathf.Max(max, Zmax(p));
        return max;
    }
    protected abstract Collider2D Col { get; }

    protected float SlopeOffset(Vector2 worldPos)
    {
        if (useSlopeDU)
        {
            Bounds b = Col.bounds;
            float range = b.max.y - b.min.y;
            if (range < Mathf.Epsilon) return downMostOffset;
            float t = Mathf.Clamp01((worldPos.y - b.min.y) / range);
            return Mathf.Lerp(downMostOffset, upMostOffset, t);
        }
        if (useSlopeRL)
        {
            Bounds b = Col.bounds;
            float range = b.max.x - b.min.x;
            if (range < Mathf.Epsilon) return leftMostOffset;
            float t = Mathf.Clamp01((worldPos.x - b.min.x) / range);
            return Mathf.Lerp(leftMostOffset, rightMostOffset, t);
        }
        return 0f;
    }

#if UNITY_EDITOR
    public static bool UseZAxis;
    public static int ZCoeff = 1;
    public static bool local = true;

    [NaughtyAttributes.Button] public void ToggleGizmoDimension() => UseZAxis = !UseZAxis;
    [NaughtyAttributes.Button] public void InvertGizmoZ()         => ZCoeff   = -ZCoeff;
    [NaughtyAttributes.Button] public void GizmoAllShowToggle()   => local    = !local;

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

    // ── 기존 드로우 헬퍼 ────────────────────────────────────────────────────

    protected static void DrawArc(Vector3 c, float r, float fromDeg, float toDeg, Vector3 right, Vector3 up, int n = 16)
    {
        float step = (toDeg - fromDeg) / n;
        for (int i = 0; i < n; i++)
        {
            float a0 = (fromDeg + step * i)       * Mathf.Deg2Rad;
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
        bool vert    = dir == CapsuleDirection2D.Vertical;
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
            Gizmos.DrawLine(new Vector3(o.x + p.x, o.y + p.y + zMin, 0),
                            new Vector3(o.x + p.x, o.y + p.y + zMax, 0));
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
            Gizmos.DrawLine(new Vector3(o.x + pts[i].x,     o.y + pts[i].y     + zMin, 0),
                            new Vector3(o.x + pts[i + 1].x, o.y + pts[i + 1].y + zMin, 0));
            Gizmos.DrawLine(new Vector3(o.x + pts[i].x,     o.y + pts[i].y     + zMax, 0),
                            new Vector3(o.x + pts[i + 1].x, o.y + pts[i + 1].y + zMax, 0));
            Gizmos.DrawLine(new Vector3(o.x + pts[i].x,     o.y + pts[i].y     + zMin, 0),
                            new Vector3(o.x + pts[i].x,     o.y + pts[i].y     + zMax, 0));
        }
        float lx = o.x + pts[^1].x;
        Gizmos.DrawLine(new Vector3(lx, o.y + zMin, 0), new Vector3(lx, o.y + zMax, 0));
    }

    // ── 경사로 기즈모용 헬퍼 ────────────────────────────────────────────────
    //
    // 꼭짓점 배치 (face A = v[0..3], face B = v[4..7]):
    //
    //   v[2]──v[3]     v[6]──v[7]
    //    |      |  ──   |      |
    //   v[0]──v[1]     v[4]──v[5]
    //
    // 각 face는 0-1 / 1-3 / 3-2 / 2-0 순서로 quad를 이룹니다.

    /// <summary>
    /// 8개 꼭짓점으로 이루어진 기울어진 직육면체(hexahedron) 와이어프레임을 그립니다.
    /// </summary>
    protected static void DrawHexahedron(Vector3[] v)
    {
        // Face A
        Gizmos.DrawLine(v[0], v[1]); Gizmos.DrawLine(v[1], v[3]);
        Gizmos.DrawLine(v[3], v[2]); Gizmos.DrawLine(v[2], v[0]);
        // Face B
        Gizmos.DrawLine(v[4], v[5]); Gizmos.DrawLine(v[5], v[7]);
        Gizmos.DrawLine(v[7], v[6]); Gizmos.DrawLine(v[6], v[4]);
        // 연결 엣지
        Gizmos.DrawLine(v[0], v[4]); Gizmos.DrawLine(v[1], v[5]);
        Gizmos.DrawLine(v[2], v[6]); Gizmos.DrawLine(v[3], v[7]);
    }

    /// <summary>
    /// 경사로가 적용된 폴리곤 압출 기즈모를 그립니다 (3D 뷰).
    /// 각 꼭짓점의 Z 위치를 경사로 보간으로 계산합니다.
    /// </summary>
    protected void DrawSlopedExtrudedPolygon3D(Vector2[] pts, Vector3 origin)
    {
        if (pts == null || pts.Length < 2) return;
        for (int i = 0; i < pts.Length; i++)
        {
            int j = (i + 1) % pts.Length;
            Vector2 wI = new Vector2(origin.x + pts[i].x, origin.y + pts[i].y);
            Vector2 wJ = new Vector2(origin.x + pts[j].x, origin.y + pts[j].y);
            float offI = SlopeOffset(wI), offJ = SlopeOffset(wJ);

            Vector3 botI = origin + new Vector3(pts[i].x, pts[i].y, ZCoeff * (ZMin + offI));
            Vector3 botJ = origin + new Vector3(pts[j].x, pts[j].y, ZCoeff * (ZMin + offJ));
            Vector3 topI = origin + new Vector3(pts[i].x, pts[i].y, ZCoeff * (ZMax + offI));
            Vector3 topJ = origin + new Vector3(pts[j].x, pts[j].y, ZCoeff * (ZMax + offJ));

            Gizmos.DrawLine(botI, botJ);
            Gizmos.DrawLine(topI, topJ);
            Gizmos.DrawLine(botI, topI);
        }
    }

    /// <summary>
    /// 경사로가 적용된 폴리곤 압출 기즈모를 그립니다 (2D 뷰 – Z → Y 투영).
    /// </summary>
    protected void DrawSlopedExtrudedPolygon2D(Vector2[] pts, Vector3 origin)
    {
        if (pts == null || pts.Length < 2) return;
        for (int i = 0; i < pts.Length; i++)
        {
            int j = (i + 1) % pts.Length;
            Vector2 wI = new Vector2(origin.x + pts[i].x, origin.y + pts[i].y);
            Vector2 wJ = new Vector2(origin.x + pts[j].x, origin.y + pts[j].y);
            float offI = SlopeOffset(wI), offJ = SlopeOffset(wJ);

            var botI = new Vector3(wI.x, wI.y + ZMin + offI, 0);
            var botJ = new Vector3(wJ.x, wJ.y + ZMin + offJ, 0);
            var topI = new Vector3(wI.x, wI.y + ZMax + offI, 0);
            var topJ = new Vector3(wJ.x, wJ.y + ZMax + offJ, 0);

            Gizmos.DrawLine(botI, botJ);
            Gizmos.DrawLine(topI, topJ);
            Gizmos.DrawLine(botI, topI);
        }
    }
#endif

    // ── 충돌 관련 ────────────────────────────────────────────────────────────

    protected virtual Vector2 GetZCheckPosition(Collider2D otherCollider)
    {
        return otherCollider.ClosestPoint(transform.position);
    }

    public bool OverlapsZ(ZCollider2D other)
    {
        Vector2 checkPos = GetZCheckPosition(other.Col);
        return Zmin(checkPos) < other.Zmax(checkPos) && Zmax(checkPos) > other.Zmin(checkPos);
    }

    public bool OverlapsZ(Collider2D other)
    {
        Vector2 checkPos = GetZCheckPosition(other);
        return Zmin(checkPos) < other.transform.position.z && Zmax(checkPos) > other.transform.position.z;
    }

    public bool OverlapsZ(ZCollider2D other, Vector2 checkPos) // overload for checking specific position
    {
        return Zmin(checkPos) < other.Zmax(checkPos) && Zmax(checkPos) > other.Zmin(checkPos);
    }

    public bool OverlapsZ(Collider2D other, Vector2 checkPos)
    {
        return Zmin(checkPos) < other.transform.position.z && Zmax(checkPos) > other.transform.position.z;
    }

    public int OverlapCollider(ContactFilter2D filter, Collider2D[] results)
    {
        int count = Col.OverlapCollider(filter, results);
        int validCount = 0;
        for (int i = 0; i < count; i++)
        {
            if (!ZPhysics2D.TryGet(results[i], out var other)) continue;
            if (!OverlapsZ(other)) continue;
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

    public Vector3 GetSurfaceNormal(bool isCeiling = false)
    {
        float multiplier = isCeiling ? -1f : 1f;

        if (!isStair)
        {
            if (useSlopeDU)
            {
                Bounds b = Col.bounds;
                float range = b.max.y - b.min.y;
                if (range < Mathf.Epsilon) return new Vector3(0, 0, multiplier);

                float dzdy = (upMostOffset - downMostOffset) / range;
                return new Vector3(0, -dzdy * multiplier, multiplier).normalized;
            }

            if (useSlopeRL)
            {
                Bounds b = Col.bounds;
                float range = b.max.x - b.min.x;
                if (range < Mathf.Epsilon) return new Vector3(0, 0, multiplier);

                float dzdx = (rightMostOffset - leftMostOffset) / range;
                return new Vector3(-dzdx * multiplier, 0, multiplier).normalized;
            }
        }
        return new Vector3(0, 0, multiplier);
    }

    public Vector2 ClosestPoint(Vector2 point) => Col.ClosestPoint(point);

    protected virtual void Awake()     => ZPhysics2D.Register(Col, this);
    protected virtual void OnDestroy() => ZPhysics2D.Unregister(Col);
}