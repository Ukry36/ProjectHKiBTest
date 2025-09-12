using System.Collections.Generic;
using UnityEngine;
public class GearManager : MonoBehaviour
{
    public GearMergeManagerSO gearMergeManager;
    [SerializeField] private CardData card;
    [SerializeField] private Player player;

    public void Start()
    {
        gearMergeManager.OnRealGearMade += player.SetGear;
    }

    [NaughtyAttributes.Button]
    public void Equip()
    {
        card.MergeGear();
        gearMergeManager.EquipMergedCard(card);
    }

    public void OnDestroy()
    {
        gearMergeManager.OnRealGearMade -= player.SetGear;
    }
}