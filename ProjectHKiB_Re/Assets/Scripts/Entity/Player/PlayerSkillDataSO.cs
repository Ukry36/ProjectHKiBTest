using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Player Skill Data", menuName = "Scriptable Objects/Data/Player Skill Data", order = 3)]
public class PlayerSkillDataSO : ScriptableObject
{
    public AttackDataSO attackData;
    public List<List<Vector2>> graffitiCodes;
    public List<List<Vector2>> graffitiAllCases;

    public void OnEnable()
    {
        CalculateAllCases();
    }

    public void CalculateAllCases()
    {
        graffitiAllCases.Clear();
        foreach (List<Vector2> graffitiCode in graffitiCodes)
        {
            foreach (Vector2 center in graffitiCode)
            {
                List<Vector2> skillCase = new(graffitiCode.Count);
                foreach (Vector2 point in graffitiCode)
                {
                    skillCase.Add(point - center);
                }
                graffitiAllCases.Add(skillCase);
            }
        }
    }

    /*
        public List<List<Vector2>> Cases = new();

        public void VectorSet(List<List<Vector2>> graffitiCodes)
        {
            List<Vector2> skillVector = graffitiCodes[0];
            for (int i = 0; i < skillVector.Count; i++)
            {
                //vector:기존 백터에 특정좌표를 더해서 Cases에 넣을 좌표.
                Vector2 point = skillVector[i];//skillVector의 좌표 중 하나
                for (int j = 0; j < skillVector.Count; j++)
                {
                    skillVector[j] += point;
                }
                Cases.Add(skillVector);
            }
        }
    */

}
