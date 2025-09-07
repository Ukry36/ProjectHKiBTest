using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Item Data", menuName = "Scriptable Objects/Data/Item Data")]
public class ItemDataSO : ScriptableObject
{
    public Sprite itemImage36x36;
    public Sprite itemIcon9x9;
    public Color color;
    public new string name;
    public string description;
    public bool canStack;
    public FilterPropertySO[] parentProperties;
    public StateMachineSO itemUseEvent;
}
