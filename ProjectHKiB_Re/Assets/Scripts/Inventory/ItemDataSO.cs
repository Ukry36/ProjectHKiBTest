using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Item Data", menuName = "Scriptable Objects/Data/Item Data")]
public class ItemDataSO : ScriptableObject
{
    public Sprite itemIcon9x9;
    public new string name;
    public string description;
    public ItemTypeSO type;
    public StateMachineSO itemUseEvent;
}
