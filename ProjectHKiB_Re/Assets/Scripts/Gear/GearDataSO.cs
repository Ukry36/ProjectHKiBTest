using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
[CreateAssetMenu(fileName = "Gear Data", menuName = "Scriptable Objects/Data/Gear Data", order = 2)]
public class GearDataSO : ItemDataSO
{
    public GearTypeSO gearType;
    public PlayerBaseDataSO playerBaseData;
    public SpriteLibraryAsset standingCGData;
    public GameObject tutorialPrefab;
    public GearDataSO[] mergeSet;
    public int mergePriority;
    public string mainGearEffectDiscription;
    public string subGearEffectDiscription;

    public GearEffectSO[] mainGearEffects;
    public GearEffectSO[] subGearEffects;

    public List<GraffitiCode> graffitiCodes;

    public List<GraffitiCode> graffitiAllCases;

    public void CalculateAllCases()
    {
        graffitiAllCases.Clear();
        foreach (GraffitiCode graffitiCode in graffitiCodes)
        {
            foreach (Vector2 center in graffitiCode.code)
            {
                GraffitiCode skillCase = new() { code = new(graffitiCode.code.Count) };
                foreach (Vector2 point in graffitiCode.code)
                {
                    skillCase.code.Add(point - center);
                }
                graffitiAllCases.Add(skillCase);
            }
        }
    }

    public void ApplyMainGearEffect(MergedPlayerBaseData realGear)
    {
        for (int i = 0; i < mainGearEffects.Length; i++)
            mainGearEffects[i].ApplyEffect(realGear);
    }

    public void ApplySubGearEffect(MergedPlayerBaseData realGear)
    {
        for (int i = 0; i < subGearEffects.Length; i++)
            subGearEffects[i].ApplyEffect(realGear);
    }
}
