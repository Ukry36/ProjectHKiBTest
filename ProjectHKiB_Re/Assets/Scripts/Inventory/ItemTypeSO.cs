using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Item Type", menuName = "Scriptable Objects/Enum/Item Type", order = 2)]
public class ItemTypeSO : ScriptableObject
{
    public bool canStack;
    public Sprite typeIcon;
    public string descirption;
}