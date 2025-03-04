using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "Gear Merge Manager", menuName = "Scriptable Objects/Manager/Gear Merge Manager", order = 2)]
public class GearMergeManagerSO : ScriptableObject
{
    public const int MAINGEAR = 0;
    public GearDataSO realGearDummy;
    public GearDataSO[] mergedGearDatas;
    public UnityEvent<List<GearDataSO>> onGearMerged;
    public UnityEvent<GearDataSO> onRealGearMade;

    private void Awake()
    {
        mergedGearDatas = mergedGearDatas.OrderBy(a => a.mergePriority).ToArray();
    }

    public void MergeGears(List<GearDataSO> equippedGears)
    {
        List<GearDataSO> mergedEquippedGears = equippedGears;

        List<bool> unavailableFlag = new();

        for (int i = 0; i < equippedGears.Count; i++)
            unavailableFlag.Add(!equippedGears[i]);

        for (int i = 0; i < mergedGearDatas.Length; i++)
        {
            List<int> canmergeGears = new();
            for (int j = 0; j < mergedGearDatas[i].mergeSet.Length; j++)
            {
                for (int k = 0; k < equippedGears.Count; k++)
                {
                    if (!unavailableFlag[k] && mergedGearDatas[i].mergeSet[j].Equals(equippedGears[k]))
                        canmergeGears.Add(k);
                }
            }

            if (canmergeGears.Count.Equals(mergedGearDatas[i].mergeSet.Length))
            {
                for (int j = 0; j < canmergeGears.Count; j++)
                {
                    unavailableFlag[canmergeGears[j]] = true;
                    mergedEquippedGears[canmergeGears[j]] = mergedGearDatas[i];
                }
            }
        }
        onGearMerged?.Invoke(mergedEquippedGears);

        MakeRealGear(mergedEquippedGears);
        onRealGearMade?.Invoke(realGearDummy);
    }

    public void MakeRealGear(List<GearDataSO> gears)
    {
        if (gears[MAINGEAR]) gears[MAINGEAR].ApplyMainGearEffect(realGearDummy);

        for (int i = 1; i < gears.Count; i++)
            if (gears[i]) gears[i].ApplySubGearEffect(realGearDummy);
    }

}