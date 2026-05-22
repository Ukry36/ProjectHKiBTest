using System;
using System.Collections.Generic;
using UnityEngine;

public static class ZPhysics2D
{
    private static readonly Dictionary<Collider2D, ZCollider2D> _registry = new();

    public static void Register(Collider2D col, ZCollider2D zCol) => _registry[col] = zCol;

    public static void Unregister(Collider2D col) => _registry.Remove(col);

    public static bool TryGet(Collider2D col, out ZCollider2D result) => _registry.TryGetValue(col, out result);

    private static int FilterByZ(Vector2 checkOrigin, Collider2D[] results, int count, float zMin, float zMax)
    {
        int validCount = 0;
        for (int i = 0; i < count; i++)
        {
            if (!_registry.TryGetValue(results[i], out var zCol)) continue;

            if (!zCol.useSlopeDU && !zCol.useSlopeRL)
            {
                if (zCol.ZMin >= zMax || zCol.ZMax <= zMin) continue;
            }
            else
            {
                // 원이나 박스의 중심점(checkOrigin)에서 가장 가까운 콜라이더 표면의 좌표를 구해 높이 판별의 기준으로 삼습니다.
                Vector2 closestPos = results[i].ClosestPoint(checkOrigin);
                if (zCol.Zmin(closestPos) >= zMax || zCol.Zmax(closestPos) <= zMin) continue;
            }

            results[validCount++] = results[i];
        }
        return validCount;
    }

    public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.OverlapCircleNonAlloc(point, radius, results, layerMask);
        return FilterByZ(point, results, count, zMin, zMax);
    }

    public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.OverlapBoxNonAlloc(point, size, angle, results, layerMask);
        return FilterByZ(point, results, count, zMin, zMax);
    }

    public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.OverlapPointNonAlloc(point, results, layerMask);
        return FilterByZ(point, results, count, zMin, zMax);
    }

    public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, float distance, RaycastHit2D[] hitResults, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.RaycastNonAlloc(origin, direction, hitResults, distance, layerMask);
        int validCount = 0;

        for (int i = 0; i < count; i++)
        {
            var hit = hitResults[i];
            if (!_registry.TryGetValue(hit.collider, out var zCol)) continue;

            if (!zCol.useSlopeDU && !zCol.useSlopeRL)
            { if (zCol.ZMin >= zMax || zCol.ZMax <= zMin) continue; }
            else
            { if (zCol.Zmin(hit.point) >= zMax || zCol.Zmax(hit.point) <= zMin) continue; }
            hitResults[validCount++] = hit;
        }

        return validCount;
    }

    public static int BoxCastNonAlloc(Vector2 origin, Vector2 boxSize, float angle, Vector2 direction, RaycastHit2D[] hitResults, float distance, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.BoxCastNonAlloc(origin, boxSize, angle, direction, hitResults, distance, layerMask);
        int validCount = 0;

        for (int i = 0; i < count; i++)
        {
            var hit = hitResults[i];
            if (!_registry.TryGetValue(hit.collider, out var zCol)) continue;

            if (!zCol.useSlopeDU && !zCol.useSlopeRL)
            { if (zCol.ZMin >= zMax || zCol.ZMax <= zMin) continue; }
            else
            { if (zCol.Zmin(hit.point) >= zMax || zCol.Zmax(hit.point) <= zMin) continue; }
            hitResults[validCount++] = hit;
        }

        return validCount;
    }

    private static readonly Collider2D[] _pointOverlapResults = new Collider2D[16];

    /// <summary>
    /// returns the collider which has highest point 
    /// inside the area of vertical line from zMin to zMax
    /// </summary>
    public static ZCollider2D ZPointGetFloor(Vector2 origin, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.OverlapPointNonAlloc(origin, _pointOverlapResults, layerMask);

        ZCollider2D bestCol = null;
        float maxZ = zMin;

        for (int i = 0; i < count; i++)
        {
            if (!_registry.TryGetValue(_pointOverlapResults[i], out var zCol)) continue;
            float currentZTop = zCol.Zmax(origin);
            if (currentZTop <= zMax && currentZTop > maxZ)
            {
                maxZ = currentZTop;
                bestCol = zCol;
            }
        }
        return bestCol;
    }

    /// <summary>
    /// returns the collider which has lowest point 
    /// inside the area of vertical line from zMin to zMax
    /// </summary>
    public static ZCollider2D ZPointGetCeiling(Vector2 origin, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.OverlapPointNonAlloc(origin, _pointOverlapResults, layerMask);

        ZCollider2D bestCol = null;
        float minZ = zMax;

        for (int i = 0; i < count; i++)
        {
            if (!_registry.TryGetValue(_pointOverlapResults[i], out var zCol)) continue;
            float currentZBottom = zCol.Zmin(origin);
            if (currentZBottom >= zMin && currentZBottom < minZ)
            {
                minZ = currentZBottom;
                bestCol = zCol;
            }
        }
        return bestCol;
    }

    public static IEnumerable<Vector2> BoxSamplePoints(Vector2 center, Vector2 size, float angle)
    {
        int xCount = Mathf.CeilToInt(size.x) * 2;
        int yCount = Mathf.CeilToInt(size.y) * 2;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        Vector2 dirX = rotation * Vector2.right;
        Vector2 dirY = rotation * Vector2.up;
        Vector2 hdx = dirX * (size.x / xCount);
        Vector2 hdy = dirY * (size.y / yCount);
        Vector2 bl = center - (0.5f * size.x * dirX) - (0.5f * size.y * dirY);

        for (int i = 0; i <= xCount; i++)
            for (int j = 0; j <= yCount; j++)
                yield return bl + (i * hdx) + (j * hdy);
    }

    public static IEnumerable<Vector2> CircleSamplePoints(Vector2 center, float radius)
    {
        yield return center;
        yield return center + Vector2.up    * radius;
        yield return center + Vector2.down  * radius;
        yield return center + Vector2.left  * radius;
        yield return center + Vector2.right * radius;
    }
    
    /// <summary>
    /// returns the collider which has highest point 
    /// inside the area of box whose vertical coverage is from zMin to zMax
    /// </summary>
    public static ZCollider2D ZBoxGetFloor(Vector2 origin, Vector2 size, float angle, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.OverlapBoxNonAlloc(origin, size, angle, _pointOverlapResults, layerMask);

        ZCollider2D bestCol = null;
        float maxZ = zMin;
        var samples = BoxSamplePoints(origin, size, angle);

        for (int i = 0; i < count; i++)
        {
            if (!_registry.TryGetValue(_pointOverlapResults[i], out var zCol)) continue;

            float currentZTop = float.MinValue;
            foreach (var p in samples)
                currentZTop = Mathf.Max(currentZTop, zCol.Zmax(p));

            if (currentZTop <= zMax && currentZTop > maxZ)
            {
                maxZ = currentZTop;
                bestCol = zCol;
            }
        }
        return bestCol;
    }

    /// <summary>
    /// returns the collider which has lowest point 
    /// inside the area of box whose vertical coverage is from zMin to zMax
    /// </summary>
    public static ZCollider2D ZBoxGetCeiling(Vector2 origin, Vector2 size, float angle, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.OverlapBoxNonAlloc(origin, size, angle, _pointOverlapResults, layerMask);

        ZCollider2D bestCol = null;
        float minZ = zMax;
        var samples = BoxSamplePoints(origin, size, angle);

        for (int i = 0; i < count; i++)
        {
            if (!_registry.TryGetValue(_pointOverlapResults[i], out var zCol)) continue;

            float currentZBottom = float.MaxValue;
            foreach (var p in samples)
                currentZBottom = Mathf.Min(currentZBottom, zCol.Zmin(p));

            if (currentZBottom >= zMin && currentZBottom < minZ)
            {
                minZ = currentZBottom;
                bestCol = zCol;
            }
        }
        return bestCol;
    }

    /// <summary>
    /// returns the collider which has highest point 
    /// inside the area of cilinder whose vertical coverage is from zMin to zMax
    /// </summary>
    public static ZCollider2D ZCircleGetFloor(Vector2 origin, float radius, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.OverlapCircleNonAlloc(origin, radius, _pointOverlapResults, layerMask);

        ZCollider2D bestCol = null;
        float maxZ = zMin;
        var samples = CircleSamplePoints(origin, radius);

        for (int i = 0; i < count; i++)
        {
            if (!_registry.TryGetValue(_pointOverlapResults[i], out var zCol)) continue;

            float currentZTop = float.MinValue;
            foreach (var p in samples)
                currentZTop = Mathf.Max(currentZTop, zCol.Zmax(p));

            if (currentZTop <= zMax && currentZTop > maxZ)
            {
                maxZ = currentZTop;
                bestCol = zCol;
            }
        }
        return bestCol;
    }

    /// <summary>
    /// returns the collider which has lowest point 
    /// inside the area of cilinder whose vertical coverage is from zMin to zMax
    /// </summary>
    public static ZCollider2D ZCircleGetCeiling(Vector2 origin, float radius, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.OverlapCircleNonAlloc(origin, radius, _pointOverlapResults, layerMask);

        ZCollider2D bestCol = null;
        float minZ = zMax;
        var samples = CircleSamplePoints(origin, radius);

        for (int i = 0; i < count; i++)
        {
            if (!_registry.TryGetValue(_pointOverlapResults[i], out var zCol)) continue;

            float currentZBottom = float.MaxValue;
            foreach (var p in samples)
                currentZBottom = Mathf.Min(currentZBottom, zCol.Zmin(p));

            if (currentZBottom >= zMin && currentZBottom < minZ)
            {
                minZ = currentZBottom;
                bestCol = zCol;
            }
        }
        return bestCol;
    }
}