using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Player Skill Data", menuName = "Scriptable Objects/Data/Player Skill Data", order = 3)]
public class PlayerSkillDataSO : ScriptableObject
{
    public DamageDataSO[] damageDatas; // use if there is mutiple hits in skill
    public List<Vector2[]> graffitiCodes;
    public bool autoFollow;
    public float defaultFollowRange;
}
