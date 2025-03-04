using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Item Data", menuName = "Scriptable Objects/Data/Item Data", order = 2)]
public class ItemDataSO : ScriptableObject
{
    public Sprite itemIcon;
    public int ID;
    public new string name;
    public string description;
    public ItemTypeSO type;
    public int count;
}
