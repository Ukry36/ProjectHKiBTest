using System.Collections.Generic;
using UnityEngine;
public class GearManager : MonoBehaviour
{
    [SerializeField] private GearMergeManagerSO gearMergeManager;
    [SerializeField] private List<GearDataSO> equippedGears;
    [SerializeField] private bool merge;
    [SerializeField] private Player player;

    public void Awake()
    {
        gearMergeManager.OnRealGearMade += SetGear;
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

    public void SetGear(MergedPlayerBaseData realGear)
    {
        player.playerData.PlayerBaseData = realGear;
        player.UpdateAnimationController();
        player.UpdateStateController();
        player.UpdateFootStepController();
        player.UpdateSkin();
    }

    public void OnDestroy()
    {
        gearMergeManager.OnRealGearMade -= SetGear;
    }
}