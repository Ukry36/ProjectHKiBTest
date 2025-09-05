using UnityEngine;
[CreateAssetMenu(fileName = "Gear Type", menuName = "Type/Gear Type")]
public class GearTypeSO : ScriptableObject
{
    public Sprite typeIcon;
    public string descirption;
    public bool isAttackGear;
    public bool cannotAttack;
}