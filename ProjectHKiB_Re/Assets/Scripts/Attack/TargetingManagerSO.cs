using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "TargetingManager", menuName = "Scriptable Objects/Manager/TargetingManager", order = 3)]
public class TargetingManagerSO : ScriptableObject
{
    private readonly float maxRotation = 25;
    private readonly float maxMaintainRotation = 40;
    private readonly float radiusCoeff = 2;
    private readonly float maintainRadiusCoeff = 4;
    private readonly float accuracy = 3f;
    private readonly float revAccuracy = 0.5f;
    private class TargetInfo
    {
        public Transform target;
        public float priority;
        public TargetInfo(Transform target, float priority)
        {
            this.target = target;
            this.priority = priority;
        }
    }

    public bool CheckCurrentTargetDistance(Transform currentTarget, Vector3 position, float radius)
    {
        return Vector3.Distance(currentTarget.position, position) < radius * radiusCoeff;
    }

    public Transform PositianalTarget(Vector3 position, float radius, LayerMask[] targetLayers)
    {
        LayerMask targets = 0;
        for (int i = 0; i < targetLayers.Length; i++)
            targets |= targetLayers[i];

        List<Collider2D> cols = Physics2D.OverlapCircleAll(position, radius, targets).ToList();
        if (cols.Equals(null) || cols.Count.Equals(0))
            return null;

        cols.Sort((a, b) => Vector3.Distance(a.transform.position, position).CompareTo(Vector3.Distance(b.transform.position, position)));
        if (cols[0])
        {
            Debug.DrawLine(position, cols[0].transform.position, Color.yellow, 0.4f);
            return cols[0].transform;
        }

        else
            return null;
    }

    public Transform DirectionalTarget(Transform currentTarget, Vector3 position, float radius, Vector2 lookingDir, LayerMask[] targetLayers)
    {
        Vector3 dir = lookingDir.normalized;
        if (currentTarget)
        {
            if (Vector3.Angle(currentTarget.position - position, dir) < maxMaintainRotation)
                if (radius * maintainRadiusCoeff > Vector3.Distance(currentTarget.position, position))
                {
                    Debug.DrawLine(position, currentTarget.position, Color.cyan, 0.4f);
                    return currentTarget;
                }
        }
        position += dir;
        radius *= radiusCoeff;
        List<TargetInfo> targets = new();
        RaycastHit2D hit;
        int i;
        for (i = 0; i < targetLayers.Length; i++)
        {
            hit = Physics2D.Raycast(position, dir, radius, targetLayers[i]);
            if (hit) targets.Add(new TargetInfo(hit.transform, i));
            if (hit) Debug.DrawLine(position, position + dir * radius, Color.green, 0.4f);
            else Debug.DrawLine(position, position + dir * radius, Color.red, 0.4f);
            for (int j = 1; j < accuracy + 1; j++)
            {
                dir = Quaternion.Euler(0, 0, maxRotation * revAccuracy) * dir;
                hit = Physics2D.Raycast(position, dir, radius, targetLayers[i]);
                if (hit) targets.Add(new TargetInfo(hit.transform, j * revAccuracy + i));
                if (hit) Debug.DrawLine(position, position + dir * radius, Color.green, 0.4f);
                else Debug.DrawLine(position, position + dir * radius, Color.red, 0.4f);
            }
            dir = lookingDir.normalized;
            for (int j = 1; j < accuracy + 1; j++)
            {
                dir = Quaternion.Euler(0, 0, -maxRotation * revAccuracy) * dir;
                hit = Physics2D.Raycast(position, dir, radius, targetLayers[i]);
                if (hit) targets.Add(new TargetInfo(hit.transform, j * revAccuracy + i));
                if (hit) Debug.DrawLine(position, position + dir * radius, Color.green, 0.4f);
                else Debug.DrawLine(position, position + dir * radius, Color.red, 0.4f);
            }
        }

        if (targets.Count > 0)
        {
            for (i = 0; i < targets.Count; i++)
            {
                targets[i].priority += Vector3.Distance(targets[i].target.position, position) / radius;
            }

            targets.Sort((a, b) => a.priority.CompareTo(b.priority));
            return targets[0].target;
        }

        return null;
    }
}