using UnityEngine;
using UnityEngine.Events;
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
