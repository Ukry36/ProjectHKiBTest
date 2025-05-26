using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Player Skill Data", menuName = "Scriptable Objects/Data/Player Skill Data", order = 3)]
public class PlayerSkillDataSO : ScriptableObject
{
    [Serializable]
    public class GraffitiCode
    {
        public List<Vector2> code = new();
    }
    public AttackDataSO attackData;
    public List<GraffitiCode> graffitiCodes;
    public List<GraffitiCode> graffitiAllCases;

    public void CalculateAllCases()
    {
        graffitiAllCases.Clear();
        foreach (GraffitiCode graffitiCode in graffitiCodes)
        {
            foreach (Vector2 center in graffitiCode.code)
            {
                GraffitiCode skillCase = new() { code = new(graffitiCode.code.Count) };
                foreach (Vector2 point in graffitiCode.code)
                {
                    skillCase.code.Add(point - center);
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
