using UnityEngine;
[CreateAssetMenu(fileName = "Enemy Skill Data", menuName = "Scriptable Objects/Data/Enemy Skill Data", order = 3)]
public class EnemySkillDataSO : ScriptableObject
{
    public DamageDataSO[] damageDatas; // use if there is mutiple hits in skill
    public float attackTinkerDelaySec;
    public float skillCoolDownSec;
    public bool autoFollow;
    public float defaultFollowRange;
}
