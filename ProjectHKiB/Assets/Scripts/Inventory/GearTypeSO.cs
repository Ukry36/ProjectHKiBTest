using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Gear Type", menuName = "Scriptable Objects/Enum/Gear Type", order = 2)]
public class GearTypeSO : ScriptableObject
{
    public Sprite typeIcon;
    public string descirption;
    public bool isAttackGear;
    public bool cannotAttack;
}