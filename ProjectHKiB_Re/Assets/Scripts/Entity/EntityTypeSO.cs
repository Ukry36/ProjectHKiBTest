using UnityEngine;
[CreateAssetMenu(fileName = "Entity Type", menuName = "Scriptable Objects/Enums/Entity Type", order = 1)]
public class EntityTypeSO : ScriptableObject
{
    public Sprite typeIcon;
    public EntityTypeSO attackTarget;
    public string descirption;
}
