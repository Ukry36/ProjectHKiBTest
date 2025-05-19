using System.Collections.Generic;
using UnityEngine;
public class GearManager : MonoBehaviour
{
    [SerializeField] private GearMergeManagerSO gearMergeManager;
    [SerializeField] private List<GearDataSO> equippedGears;
    [SerializeField] private bool merge;
    [SerializeField] private Player player;

    public void Start()
    {
        gearMergeManager.OnRealGearMade += player.SetGear;
        gearMergeManager.MergeGears(equippedGears);
    }

    public void Update()
    {
        if (merge)
        {
            gearMergeManager.MergeGears(equippedGears);
            merge = false;
        }
    }

    public void OnDestroy()
    {
        gearMergeManager.OnRealGearMade -= player.SetGear;
    }
}