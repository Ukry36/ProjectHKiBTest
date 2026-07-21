using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.U2D.Animation;
[CreateAssetMenu(fileName = "Gear Data", menuName = "Scriptable Objects/Data/Gear Data", order = 2)]
public class GearDataSO : ItemDataSO
{
    [Header("Data")]
    public GearTypeSO gearType;
    public PlayerBaseDataSO playerBaseData;
    public GameObject tutorialPrefab;
    public float transformTime;

    [Header("Base Stat Buff")]
    public StatBuffSO buff;

    [Header("Base Animation")]
    public SimpleAnimationDataSO mainAnimationData;
    public SpriteLibraryAsset mainSpriteLibrary;
    public SimpleAnimationDataSO effectAnimationData;
    public SpriteLibraryAsset effectSpriteLibrary;

    [Header("Skin Data")]
    public SkinDataSO skinData;
    public SpriteLibraryAsset standingCGData;

    [Header("Merge Setting")]
    public GearDataSO[] mergeSet;
    public int mergePriority;
    public SerializedDictionary<GearDataSO, SkinDataSO> SkinMixList;

    [Header("Gear Effect")]
    public string mainGearEffectDiscription;
    public GearEffectSO[] mainGearEffects;

    [Header("Graffiti")]
    public List<GraffitiCode> graffitiCodes;
    public List<GraffitiCode> graffitiAllCases;

    public void CalculateAllCases()
    {
        graffitiAllCases.Clear();
        foreach (GraffitiCode graffitiCode in graffitiCodes)
        {
            foreach (Vector2Int center in graffitiCode.code)
            {
                GraffitiCode skillCase = new() { code = new(graffitiCode.code.Count) };
                foreach (Vector2Int point in graffitiCode.code)
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
}
