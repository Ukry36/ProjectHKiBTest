using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class NewBehaviourScript : MonoBehaviour
{
    PlayerSkillDataSO whirlwind = ScriptableObject.CreateInstance<PlayerSkillDataSO>();
    whirlwind.graffitiCodes.Add(new Vector2[]{
            new Vector2( 0,  0),
            new Vector2( 0,  1),
            new Vector2( 1,  0),
            new Vector2(-1,  0),
            new Vector2( 0, -1),
        });
    whirlwind.graffitiCodes.Add(new Vector2[]{
            new Vector2( 1,  0),
            new Vector2( 1,  1),
            new Vector2( 2,  0),
            new Vector2( 0,  0),
            new Vector2( 1, -1),
        });
    whirlwind.graffitiCodes.Add(new Vector2[]{
            new Vector2( -1,  0),
            new Vector2( -1,  1),
            new Vector2( 0,  0),
            new Vector2(-1,  0),
            new Vector2( -1, -1),
        });
    whirlwind.graffitiCodes.Add(new Vector2[]{
            new Vector2( 0,  1),
            new Vector2( 0,  2),
            new Vector2( 1,  1),
            new Vector2(-1,  1),
            new Vector2( 0, 0 ),
        });
    whirlwind.graffitiCodes.Add(new Vector2[]{
            new Vector2( 0,  -1),
            new Vector2( 0,  0),
            new Vector2( 1,  -1),
            new Vector2(-1,  -1),
            new Vector2( 0, -2),
        });
    private List<Vector2> graffitiVectorSet = new List<Vector2>();
    private List<PlayerSkillDataSO> skillList = new List<SkillDataS0>();
    skillList.Add(whirlwind);
    
    //(0,0)(1,0)(0,1)
    int completedSkillNum = -1;

public bool ContainsVector(
        List<Vector2[]> patterns,
        Vector2 target)
{
    // null 방어
    if (patterns == null)
        return false;

    // 각 배열을 순회하면서
    foreach (var arr in patterns)
    {
        if (arr == null)
            continue;    // null 배열은 스킵

        // 배열에 target이 있으면 즉시 true 반환
        foreach (var v in arr)
        {
            if (v == target)
                return true;
        }
    }

    // 아무곳에도 없으면 false
    return false;
}

//이건 따로 모으는 스크립트 만들어야 할듯. 

//현재 좌표 currentVector


void CheckGraffitiProgress(Vector2)
    {
        // 움직일 때마다 호출

        // ValidateVectorSet이 
        //false면 부정적인 피드백과 함께
        //vectorset 초기화하고 이펙트 그린 것들 지움.
        //동시에completedSkillNum >= 0이면 CheckGraffitiComplete 호출 ,

        //true면 이동한 자리에 빤짝이
        // CheckCompleted >= 0 면 성공 피드백, completedSkillNum을 셋

        //여기에 이제 빨간색 이펙트 들어가는거
        graffitiVectorSet.Clear();
    }

    void CheckGraffitiComplete()
    {
        completedSkillNum = CheckCompleted();
        if (completedSkillNum >= 0)
        {
            //여기 호출하는 거 넣고

            completedSkillNum = -1;
        }
    }
    // 그라피티 종료 관련 인풋이 들어오면 호출
    // 플레이어가 스킬 발동하도록 함


    bool ValidateVectorSet(Vector2[] vector)
    {
    List<Vector2[]> patterns;

    for (int i = 0; i < skillList.Count; i++)
    {
        if (ContainsVactor(skillList[i], vector))
            graffitiVectorSet.Add(vector);
    }
    return false;

}

    // 이동할 때마다 vectorset이 스킬 중에 포함되는지 확인
    


    int CheckCompleted()
    {
        for (int i = 0; i < skillList.Count; i++)
        {
            if (graffitiVectorSet.value == skillList[i].value)
            {
                return i;
            }
    }
}
    // 현재 vectorset이 스킬 중 하나와 일치하는지 확인
        
    }

