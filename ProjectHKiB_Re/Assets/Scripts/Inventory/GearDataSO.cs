using UnityEngine;
using UnityEngine.U2D.Animation;
[CreateAssetMenu(fileName = "Gear Data", menuName = "Scriptable Objects/Data/Gear Data", order = 2)]
public abstract class GearDataSO : ItemDataSO
{
    [SerializeField] protected GearMergeManagerSO gearManager;
    public GearTypeSO gearType;
    public PlayerDataSO gearData;
    public SpriteLibraryAsset standingCGData;
    public GameObject tutorialPrefab;
    public GearDataSO[] mergeSet;
    public int mergePriority;
    public string mainGearEffectDiscription;
    public string subGearEffectDiscription;

    public virtual void ApplyMainGearEffect(GearDataSO realGear)
    {
        realGear.gearData.MaxHP.Value += 25;
        realGear.gearData.defaultSkin = gearData.defaultSkin;
    }
    public virtual void ApplySubGearEffect(GearDataSO realGear)
    {
        realGear.gearData.MaxHP.Value += 25;
    }
}
