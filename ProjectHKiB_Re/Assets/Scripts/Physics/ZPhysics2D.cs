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

    public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, float distance, RaycastHit2D[] hitResults, int layerMask, float zMin, float zMax)
    {
        int count = Physics2D.RaycastNonAlloc(origin, direction, hitResults, distance, layerMask);
        int validCount = 0;

        for (int i = 0; i < count; i++)
        {
            var hit = hitResults[i];
            if (!_registry.TryGetValue(hit.collider, out var zCol)) continue;

            if (!zCol.useSlopeDU && !zCol.useSlopeRL)
            {
                if (zCol.ZMin >= zMax || zCol.ZMax <= zMin) continue;
            }
            else
            {
                if (zCol.Zmin(hit.point) >= zMax || zCol.Zmax(hit.point) <= zMin) continue;
            }
            hitResults[validCount++] = hit;
        }

        return validCount;
    }

    private static readonly Collider2D[] _pointOverlapResults = new Collider2D[16];

    public static Vector3 ZRayGetFloorNormal(Vector2 origin, float height, int layerMask)
    {
        int count = Physics2D.OverlapPointNonAlloc(origin, _pointOverlapResults, layerMask);

        ZCollider2D bestCol = null;
        float maxZ = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            if (!_registry.TryGetValue(_pointOverlapResults[i], out var zCol)) continue;
            float currentZTop = zCol.Zmax(origin);
            if (currentZTop <= height && currentZTop > maxZ)
            {
                maxZ = currentZTop;
                bestCol = zCol;
            }
        }
        if (bestCol == null) return new Vector3(0, 0, 1f);
        return bestCol.GetSurfaceNormal();
    }

    public static Vector3 ZRayGetCeilingNormal(Vector2 origin, float height, int layerMask)
    {
        int count = Physics2D.OverlapPointNonAlloc(origin, _pointOverlapResults, layerMask);

        ZCollider2D bestCol = null;
        float minZ = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (!_registry.TryGetValue(_pointOverlapResults[i], out var zCol)) continue;
            float currentZBottom = zCol.Zmin(origin);
            if (currentZBottom >= height && currentZBottom < minZ)
            {
                minZ = currentZBottom;
                bestCol = zCol;
            }
        }
        if (bestCol == null) return new Vector3(0, 0, -1f);
        return bestCol.GetSurfaceNormal(true);
    }
}