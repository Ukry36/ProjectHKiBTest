using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

public class StandingCGGravity : MonoBehaviour
{
    [Serializable]
    public class GravityElement
    {
        public Transform transform;
        public float minLocalAngle;
        public float maxLocalAngle;
        public Tween tween;
        public float currentTargetAngle;
    }

    public List<GravityElement> gravityElements;

    void Update()
    {
        for (int i = 0; i < gravityElements.Count; i++)
        {
            GravityElement element = gravityElements[i];
            Vector3 currentLocalAngle = element.transform.localEulerAngles;
            if (currentLocalAngle.z < -180f) currentLocalAngle.z += 360;
            if (currentLocalAngle.z > 180f) currentLocalAngle.z -= 360;
            if (currentLocalAngle.z < element.minLocalAngle)
                element.transform.localEulerAngles = Vector3.forward * element.minLocalAngle;
            if (currentLocalAngle.z > element.maxLocalAngle)
                element.transform.localEulerAngles = Vector3.forward * element.maxLocalAngle;


            Vector3 targetLocalAngle = -element.transform.parent.eulerAngles;
            if (targetLocalAngle.z < -180f) targetLocalAngle.z += 360;
            if (targetLocalAngle.z > 180f) targetLocalAngle.z -= 360;
            float clampedZ = Mathf.Clamp(targetLocalAngle.z, element.minLocalAngle, element.maxLocalAngle);
            //if (Mathf.Abs(element.transform.localEulerAngles.z - clampedZ) < 0.1f) continue;
            if (Mathf.Abs(element.currentTargetAngle - clampedZ) < 0.1f)
                continue;

            element.currentTargetAngle = clampedZ;
            targetLocalAngle = Vector3.forward * clampedZ;

            element.tween?.Kill();
            element.tween = element.transform.DOLocalRotate(targetLocalAngle, 0.36f).SetEase(Ease.OutBack);
        }
    }
}
