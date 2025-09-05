using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Item Type", menuName = "Scriptable Objects/Enum/Item Type")]
public class ItemTypeSO : ScriptableObject
{
    public bool canStack;
    public Sprite typeIcon5x5;
    public string descirption;
}