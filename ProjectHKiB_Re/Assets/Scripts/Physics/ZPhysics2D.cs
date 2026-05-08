using System;
using System.Collections.Generic;
using UnityEngine;

public static class ZPhysics2D
{
    private static readonly Dictionary<Collider2D, ZCollider2D> _registry = new();

    public static void Register(Collider2D col, ZCollider2D zCol) => _registry[col] = zCol;

    public static void Unregister(Collider2D col) => _registry.Remove(col);

    public static bool TryGet(Collider2D col, out ZCollider2D result) => _registry.TryGetValue(col, out result);

    private static int FilterByZ(Collider2D[] results, int count, float zMin, float zMax)
    {
        int validCount = 0;
        for (int i = 0; i < count; i++)
        {
            if (!_registry.TryGetValue(results[i], out var zCol)) continue;
            if (zCol.ZMin >= zMax || zCol.ZMax <= zMin) continue;

            results[validCount++] = results[i];
        }
        return validCount;
    }

    public static int OverlapCircleNonAlloc(
        Vector2 point, float radius,
        Collider2D[] results,
        int layerMask,
        float zMin, float zMax)
    {
        int count = Physics2D.OverlapCircleNonAlloc(point, radius, results, layerMask);
        return FilterByZ(results, count, zMin, zMax);
    }

    public static int OverlapBoxNonAlloc(
        Vector2 point, Vector2 size, float angle,
        Collider2D[] results,
        int layerMask,
        float zMin, float zMax)
    {
        int count = Physics2D.OverlapBoxNonAlloc(point, size, angle, results, layerMask);
        return FilterByZ(results, count, zMin, zMax);
    }

    public static int RaycastNonAlloc(
        Vector2 origin, Vector2 direction, float distance,
        RaycastHit2D[] hitResults,
        Collider2D[] colBuffer,
        int layerMask,
        float zMin, float zMax)
    {
        int count = Physics2D.RaycastNonAlloc(origin, direction, hitResults, distance, layerMask);

        for (int i = 0; i < count; i++)
            colBuffer[i] = hitResults[i].collider;

        int validCount = FilterByZ(colBuffer, count, zMin, zMax);

        int writeIdx = 0;
        for (int i = 0; i < count; i++)
        {
            if (Array.IndexOf(colBuffer, hitResults[i].collider, 0, validCount) >= 0)
                hitResults[writeIdx++] = hitResults[i];
        }
        return validCount;
    }
}