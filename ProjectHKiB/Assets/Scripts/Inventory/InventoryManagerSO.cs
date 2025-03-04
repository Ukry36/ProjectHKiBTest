using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Inventory Manager", menuName = "Scriptable Objects/Manager/Inventory Manager", order = 2)]
public class InventoryManagerSO : ScriptableObject
{
    public List<ItemDataSO> inventory;

}
