using System.Linq;
using DG.Tweening;
using UnityEngine;

public class GravityPart : MonoBehaviour
{
    public float minLocalAngle;
    public float maxLocalAngle;
    public bool isLeft;
    public bool isRight;
    public bool isHairPart;
    public Tween tween;
    [HideInInspector] public float currentTargetAngle;


    [NaughtyAttributes.Button("Min -1")]
    public void MinM5() => minLocalAngle -= 1;

    [NaughtyAttributes.Button("Min -5")]
    public void MinP5() => minLocalAngle -= 5;

    [NaughtyAttributes.Button("Max +1")]
    public void MaxM5() => maxLocalAngle += 1;

    [NaughtyAttributes.Button("Max +5")]
    public void MaxP5() => maxLocalAngle += 5;

    [NaughtyAttributes.Button()]
    public void MakeFree() { maxLocalAngle = 180; minLocalAngle = -180; }

    [NaughtyAttributes.Button()]
    public void Reset() { maxLocalAngle = 0; minLocalAngle = 0; isLeft = false; isRight = false; isHairPart = false; }

    [NaughtyAttributes.Button()]
    public void ApplySettingToDuplicants()
    {
        var objects = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == this.gameObject.name);
        foreach (var obj in objects)
        {
            if (!obj.TryGetComponent(out GravityPart component))
            {
                obj.AddComponent<GravityPart>();
                component = obj.GetComponent<GravityPart>();
            }
            component.maxLocalAngle = maxLocalAngle;
            component.minLocalAngle = minLocalAngle;
            component.isLeft = isLeft;
            component.isRight = isRight;
            component.isHairPart = isHairPart;
        }
    }
}