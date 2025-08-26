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
        public bool isHairPart;
        public Tween tween;
        [HideInInspector] public float currentTargetAngle;
    }

    public Animator bodyAnimator;
    public FaceController faceController;
    public Color originalColor;
    public Color disabledColor;
    public Color damagedColor;
    public Vector3 hideLocalPosition;
    public Vector3 originalLocallPosition;
    [HideInInspector] public bool talkEnabled;
    [HideInInspector] public bool isHidden;

    public float defaultImpactStrength = 5;
    public float wind = 0;
    [NaughtyAttributes.Button()] public void ImpactRight() => ImpactRight(defaultImpactStrength, false);
    [NaughtyAttributes.Button()] public void ImpactLeft() => ImpactLeft(defaultImpactStrength, false);
    [NaughtyAttributes.Button()] public void ImpactUp() => ImpactUp(defaultImpactStrength, false);
    [NaughtyAttributes.Button()] public void ImpactDown() => ImpactDown(defaultImpactStrength, false);

    public List<GravityPart> gravityParts;

    public void Awake()
    {
        InitGravityElements();
    }
    public void InitGravityElements()
    {
        var parts = GetComponentsInChildren<GravityPart>(true);
        foreach (var part in parts)
        {
            gravityParts.Add(part);
        }
    }

    public void PlayBodyAnimation(string name)
    {
        if (bodyAnimator) bodyAnimator.Play(name);
    }

    public void PlayFaceAnimation(string name)
    {
        if (faceController) faceController.PlayAnimation(name);
    }

    private const string UPLAYER = "UpStandingCG";
    private const string DOWNLAYER = "DownStandingCG";
    public void SetSortingLayerUp()
    {
        var canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].sortingLayerName = UPLAYER;
        }
    }

    public void SetSortingLayerDown()
    {
        var canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
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
        for (int i = 0; i < gravityParts.Count; i++)
        {
            GravityPart part = gravityParts[i];
            Vector3 currentLocalAngle = part.transform.localEulerAngles;
            if (currentLocalAngle.z < -180f) currentLocalAngle.z += 360;
            if (currentLocalAngle.z > 180f) currentLocalAngle.z -= 360;
            if (currentLocalAngle.z < part.minLocalAngle)
                part.transform.localEulerAngles = Vector3.forward * part.minLocalAngle;
            if (currentLocalAngle.z > part.maxLocalAngle)
                part.transform.localEulerAngles = Vector3.forward * part.maxLocalAngle;


            Vector3 targetLocalAngle = -part.transform.parent.eulerAngles + Vector3.forward * wind;
            if (targetLocalAngle.z < -180f) targetLocalAngle.z += 360;
            if (targetLocalAngle.z > 180f) targetLocalAngle.z -= 360;
            float clampedZ = Mathf.Clamp(targetLocalAngle.z, part.minLocalAngle, part.maxLocalAngle);
            //if (Mathf.Abs(element.transform.localEulerAngles.z - clampedZ) < 0.1f) continue;
            if (Mathf.Abs(part.currentTargetAngle - clampedZ) < 0.1f)
                continue;

            part.currentTargetAngle = clampedZ;
            targetLocalAngle = Vector3.forward * clampedZ;

            part.tween?.Kill();
            part.tween = part.transform.DOLocalRotate(targetLocalAngle, 0.36f).SetEase(Ease.OutBack);
        }
    }

    public void ImpactHairRight(float strength) => ImpactRight(strength, true);
    public void ImpactRight(float strength, bool onlyHairPart)
    {
        if (strength < 0) return;
        for (int i = 0; i < gravityParts.Count; i++)
        {
            GravityPart part = gravityParts[i];
            if (onlyHairPart && !part.isHairPart) continue;

            Vector3 targetLocalAngle = part.transform.localEulerAngles + Vector3.forward * strength;
            if (targetLocalAngle.z < -180f) targetLocalAngle.z += 360;
            if (targetLocalAngle.z > 180f) targetLocalAngle.z -= 360;
            float clampedZ = Mathf.Clamp(targetLocalAngle.z, part.minLocalAngle, part.maxLocalAngle);

            part.currentTargetAngle = clampedZ;
            targetLocalAngle = Vector3.forward * clampedZ;
            part.transform.localEulerAngles = targetLocalAngle;
        }
    }

    public void ImpactHairLeft(float strength) => ImpactLeft(strength, true);
    public void ImpactLeft(float strength, bool onlyHairPart)
    {
        if (strength < 0) return;
        for (int i = 0; i < gravityParts.Count; i++)
        {
            GravityPart part = gravityParts[i];
            if (onlyHairPart && !part.isHairPart) continue;

            Vector3 targetLocalAngle = part.transform.localEulerAngles - Vector3.forward * strength;
            if (targetLocalAngle.z < -180f) targetLocalAngle.z += 360;
            if (targetLocalAngle.z > 180f) targetLocalAngle.z -= 360;
            float clampedZ = Mathf.Clamp(targetLocalAngle.z, part.minLocalAngle, part.maxLocalAngle);

            part.currentTargetAngle = clampedZ;
            targetLocalAngle = Vector3.forward * clampedZ;
            part.transform.localEulerAngles = targetLocalAngle;
        }
    }

    public void ImpactHairUp(float strength) => ImpactUp(strength, true);
    public void ImpactUp(float strength, bool onlyHairPart)
    {
        if (strength < 0) return;
        for (int i = 0; i < gravityParts.Count; i++)
        {
            GravityPart part = gravityParts[i];
            if (onlyHairPart && !part.isHairPart) continue;

            Vector3 targetLocalAngle = part.transform.localEulerAngles + (part.isLeft ? -1 : part.isRight ? 1 : 0) * strength * Vector3.forward;
            if (targetLocalAngle.z < -180f) targetLocalAngle.z += 360;
            if (targetLocalAngle.z > 180f) targetLocalAngle.z -= 360;
            float clampedZ = Mathf.Clamp(targetLocalAngle.z, part.minLocalAngle, part.maxLocalAngle);

            part.currentTargetAngle = clampedZ;
            targetLocalAngle = Vector3.forward * clampedZ;
            part.transform.localEulerAngles = targetLocalAngle;
        }
    }

    public void ImpactHairDown(float strength) => ImpactDown(strength, true);
    public void ImpactDown(float strength, bool onlyHairPart)
    {
        if (strength < 0) return;
        for (int i = 0; i < gravityParts.Count; i++)
        {
            GravityPart part = gravityParts[i];
            if (onlyHairPart && !part.isHairPart) continue;

            Vector3 targetLocalAngle = part.transform.localEulerAngles - (part.isLeft ? -1 : part.isRight ? 1 : 0) * strength * Vector3.forward;
            if (targetLocalAngle.z < -180f) targetLocalAngle.z += 360;
            if (targetLocalAngle.z > 180f) targetLocalAngle.z -= 360;
            float clampedZ = Mathf.Clamp(targetLocalAngle.z, part.minLocalAngle, part.maxLocalAngle);

            part.currentTargetAngle = clampedZ;
            targetLocalAngle = Vector3.forward * clampedZ;
            part.transform.localEulerAngles = targetLocalAngle;
        }
    }
}
