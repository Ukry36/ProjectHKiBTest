using UnityEngine;
[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/Data/AttackData", order = 3)]
public class AttackDataSO : ScriptableObject
{
    public DamageDataSO[] damageDatas;
    public int attackMoveMinRange;
    public int attackMoveMaxRange;
    public bool isAutoTarget;
    public AudioDataSO initialSound;

}
