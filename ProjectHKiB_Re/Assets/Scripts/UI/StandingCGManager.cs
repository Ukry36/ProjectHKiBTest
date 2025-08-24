using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class StandingCGManager : MonoBehaviour
{
    [Serializable]
    public class GravityElement
    {
        public Transform transform;
        public float minLocalAngle;
        public float maxLocalAngle;
        public bool isLeft;
        public bool isRight;
        public Tween tween;
        [HideInInspector] public float currentTargetAngle;
    }

    public Animator animator;
    public Color originalColor;
    public Color disabledColor;
    public Color damagedColor;
    public Vector3 hideLocalPosition;
    public Vector3 originalLocallPosition;
    [HideInInspector] public bool talkEnabled;
    [HideInInspector] public bool isHidden;

    public float defaultImpactStrength = 5;
    public float wind = 0;
    [NaughtyAttributes.Button()]
    public void ImpactRight() => ImpactRight(defaultImpactStrength);
    [NaughtyAttributes.Button()]
    public void ImpactLeft() => ImpactLeft(defaultImpactStrength);
    [NaughtyAttributes.Button()]
    public void ImpactUp() => ImpactUp(defaultImpactStrength);
    [NaughtyAttributes.Button()]
    public void ImpactDown() => ImpactDown(defaultImpactStrength);

    public List<Canvas> canvases;
    public List<GravityElement> gravityElements;

    public void PlayAnimation(string name)
    {
        if (animator) animator.Play(name);
    }

    private const string UPLAYER = "UpStandingCG";
    private const string DOWNLAYER = "DownStandingCG";
    public void SetSortingLayerUp()
    {
        for (int i = 0; i < canvases.Count; i++)
        {
            canvases[i].sortingLayerName = UPLAYER;
        }
    }

    public void SetSortingLayerDown()
    {
        for (int i = 0; i < canvases.Count; i++)
        {
            canvases[i].sortingLayerName = DOWNLAYER;
        }
    }

    [NaughtyAttributes.Button()]
    public void Hide() => Move(hideLocalPosition);

    [NaughtyAttributes.Button()]
    public void Move() => Move(Vector3.zero);
    public void Move(Vector3 localPosition) => transform.DOLocalMove(localPosition, 0.36f);
    public void MoveToOriginalPosition() => Move(originalLocallPosition);

    [NaughtyAttributes.Button()]
    public void SetTalkDisabled()
    {
        SetSortingLayerDown();
        Image[] images = GetComponentsInChildren<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            images[i].DOColor(disabledColor, 0.36f);
        }
        talkEnabled = false;
    }
    [NaughtyAttributes.Button()]
    public void SetTalkEnabled()
    {
        SetSortingLayerUp();
        Image[] images = GetComponentsInChildren<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            images[i].DOColor(originalColor, 0.36f);
        }
        talkEnabled = true;
    }
    [NaughtyAttributes.Button()]
    public void Damage()
    {
        SetSortingLayerDown();
        Image[] images = GetComponentsInChildren<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            images[i].color = damagedColor;
        }
        if (talkEnabled) SetTalkEnabled();
        else SetTalkDisabled();
    }

    private void Update()
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


            Vector3 targetLocalAngle = -element.transform.parent.eulerAngles + Vector3.forward * wind;
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

    public void ImpactRight(float strength)
    {
        if (strength < 0) return;
        for (int i = 0; i < gravityElements.Count; i++)
        {
            GravityElement element = gravityElements[i];

            Vector3 targetLocalAngle = element.transform.localEulerAngles + Vector3.forward * strength;
            if (targetLocalAngle.z < -180f) targetLocalAngle.z += 360;
            if (targetLocalAngle.z > 180f) targetLocalAngle.z -= 360;
            float clampedZ = Mathf.Clamp(targetLocalAngle.z, element.minLocalAngle, element.maxLocalAngle);

            element.currentTargetAngle = clampedZ;
            targetLocalAngle = Vector3.forward * clampedZ;
            element.transform.localEulerAngles = targetLocalAngle;
        }
    }

    public void ImpactLeft(float strength)
    {
        if (strength < 0) return;
        for (int i = 0; i < gravityElements.Count; i++)
        {
            GravityElement element = gravityElements[i];

            Vector3 targetLocalAngle = element.transform.localEulerAngles - Vector3.forward * strength;
            if (targetLocalAngle.z < -180f) targetLocalAngle.z += 360;
            if (targetLocalAngle.z > 180f) targetLocalAngle.z -= 360;
            float clampedZ = Mathf.Clamp(targetLocalAngle.z, element.minLocalAngle, element.maxLocalAngle);

            element.currentTargetAngle = clampedZ;
            targetLocalAngle = Vector3.forward * clampedZ;
            element.transform.localEulerAngles = targetLocalAngle;
        }
    }

    public void ImpactUp(float strength)
    {
        if (strength < 0) return;
        for (int i = 0; i < gravityElements.Count; i++)
        {
            GravityElement element = gravityElements[i];

            Vector3 targetLocalAngle = element.transform.localEulerAngles + (element.isLeft ? -1 : element.isRight ? 1 : 0) * strength * Vector3.forward;
            if (targetLocalAngle.z < -180f) targetLocalAngle.z += 360;
            if (targetLocalAngle.z > 180f) targetLocalAngle.z -= 360;
            float clampedZ = Mathf.Clamp(targetLocalAngle.z, element.minLocalAngle, element.maxLocalAngle);

            element.currentTargetAngle = clampedZ;
            targetLocalAngle = Vector3.forward * clampedZ;
            element.transform.localEulerAngles = targetLocalAngle;
        }
    }

    public void ImpactDown(float strength)
    {
        if (strength < 0) return;
        for (int i = 0; i < gravityElements.Count; i++)
        {
            GravityElement element = gravityElements[i];

            Vector3 targetLocalAngle = element.transform.localEulerAngles - (element.isLeft ? -1 : element.isRight ? 1 : 0) * strength * Vector3.forward;
            if (targetLocalAngle.z < -180f) targetLocalAngle.z += 360;
            if (targetLocalAngle.z > 180f) targetLocalAngle.z -= 360;
            float clampedZ = Mathf.Clamp(targetLocalAngle.z, element.minLocalAngle, element.maxLocalAngle);

            element.currentTargetAngle = clampedZ;
            targetLocalAngle = Vector3.forward * clampedZ;
            element.transform.localEulerAngles = targetLocalAngle;
        }
    }
}
