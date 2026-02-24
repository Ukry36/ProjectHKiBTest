using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Item Data", menuName = "Scriptable Objects/Data/Item Data")]
public class ItemDataSO : ScriptableObject
{
    // 고유 식별을 위한GUID
    [SerializeField, HideInInspector]
    private string guid;
    public string GUID => guid;

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(guid))
            {
                guid = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
    #endif

    public Sprite itemImage36x36;
    public Sprite itemIcon;
    public Color color;
    public new string name;
    public string description;
    public bool canStack;
    public FilterPropertySO[] parentProperties;
    public StateMachineSO itemUseEvent;
}
