using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Player Skill Data", menuName = "Scriptable Objects/Data/Player Skill Data", order = 3)]
public class PlayerSkillDataSO : ScriptableObject
{
    public AttackDataSO attackData;
    public List<List<Vector2>> graffitiCodes;
}
