using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Gear Merge Manager", menuName = "Scriptable Objects/Manager/Gear Merge Manager", order = 2)]
public class GearMergeManagerSO : ScriptableObject
{
    private const int MAINGEAR = 0;
    private const int MAXGEARCOUNT = 4;
    public GearDataSO defaultGearData;
    [SerializeField] private DatabaseManagerSO databaseManager;

    // Every gears that can be merged from other gears
    [SerializeField] private GearDataSO[] allMergedGearDatas;
    public delegate void OnRealGearMadeEventHandler(MergedPlayerBaseData realGear);
    public event OnRealGearMadeEventHandler OnRealGearMade;


    private void Awake()
    {
        allMergedGearDatas = allMergedGearDatas.OrderBy(a => a.mergePriority).ToArray();
        if (defaultGearData == null)
            Debug.LogError("ERROR : default gear data not attached!!!");
    }

    public void MergeGears(List<GearDataSO> equippedGears)
    {
        int i, j, k;
        // final merged gear data
        MergedPlayerBaseData mergedGearData = new();
        // If gear in slot is null or already merged, its value is false
        List<bool> isGearAvailableSlot = new(MAXGEARCOUNT);

        // temporary List for merged gears
        List<GearDataSO> mergedEquippedGears = equippedGears.ToList();

        // Stores nullcheck in isGearAvailableSlot
        // if null, put defaultGear in
        for (i = 0; i < equippedGears.Count; i++)
        {
            isGearAvailableSlot.Add(equippedGears[i] != null);
            if (equippedGears[i] == null)
                mergedEquippedGears[i] = defaultGearData;
        }

        // For all merged gears check if their mergeset gears are in equipped gears 
        // If they exists, check them as already merged
        for (i = 0; i < allMergedGearDatas.Length; i++)
        {
            List<int> inMergeSetGearSlots = new(equippedGears.Count);
            for (j = 0; j < allMergedGearDatas[i].mergeSet.Length; j++)
            {
                for (k = 0; k < equippedGears.Count; k++)
                {
                    if (isGearAvailableSlot[k] && allMergedGearDatas[i].mergeSet[j].Equals(equippedGears[k]))
                        inMergeSetGearSlots.Add(k);
                }
            }

            if (inMergeSetGearSlots.Count.Equals(allMergedGearDatas[i].mergeSet.Length))
            {
                for (j = 0; j < inMergeSetGearSlots.Count; j++)
                {
                    isGearAvailableSlot[inMergeSetGearSlots[j]] = false;
                    mergedEquippedGears[inMergeSetGearSlots[j]] = allMergedGearDatas[i];
                }
            }
        }

        // Set skin skin and animation or base stats of main gear as default
        // and apply main effect, set geartype
        SetDatas(mergedGearData, mergedEquippedGears[MAINGEAR].playerBaseData);
        mergedEquippedGears[MAINGEAR].ApplyMainGearEffect(mergedGearData);
        mergedGearData.gearType = mergedEquippedGears[MAINGEAR].gearType;

        // If there is attack gear in subgears, override attack animation and geartype
        // also if maingear cannot attack it doesn't happen
        // and apply sub effects
        for (i = mergedEquippedGears.Count - 1; i > MAINGEAR; i--)
        {
            if (mergedEquippedGears[i].gearType.isAttackGear
            && !mergedEquippedGears[MAINGEAR].gearType.isAttackGear
            && !mergedEquippedGears[MAINGEAR].gearType.cannotAttack)
            {
#if UNITY_EDITOR
                //Debug.Log("AttackGear Overrided: " + mergedEquippedGears[i]);
                //Debug.Log("AttackData Length: " + mergedEquippedGears[i].playerBaseData.AttackDatas.Length);
#endif
                databaseManager.SetIStateControllable(mergedGearData, mergedEquippedGears[i].playerBaseData);
                mergedGearData.gearType = mergedEquippedGears[i].gearType;
                mergedGearData.AttackDatas = mergedEquippedGears[i].playerBaseData.AttackDatas;
            }
            mergedEquippedGears[i].ApplySubGearEffect(mergedGearData);
        }
        // This triggers player to reset data
        // also triggers other effects or something
        OnRealGearMade?.Invoke(mergedGearData);
        mergedGearData = null;
    }

    public void SetDatas(MergedPlayerBaseData mergedGearData, PlayerBaseDataSO playerBaseData)
    {
        databaseManager.SetIMovable(mergedGearData, playerBaseData);
        databaseManager.SetIAttackable(mergedGearData, playerBaseData);
        databaseManager.SetIDodgeable(mergedGearData, playerBaseData);
        databaseManager.SetIDamagable(mergedGearData, playerBaseData);
        databaseManager.SetIDodgeable(mergedGearData, playerBaseData);
        databaseManager.SetGraffiriable(mergedGearData, playerBaseData);
        databaseManager.SetIStateControllable(mergedGearData, playerBaseData);
        databaseManager.SetISkinable(mergedGearData, playerBaseData);
    }
}