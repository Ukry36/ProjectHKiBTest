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
    // null ���
    if (patterns == null)
        return false;

    // �� �迭�� ��ȸ�ϸ鼭
    foreach (var arr in patterns)
    {
        if (arr == null)
            continue;    // null �迭�� ��ŵ

        // �迭�� target�� ������ ��� true ��ȯ
        foreach (var v in arr)
        {
            if (v == target)
                return true;
        }
    }

    // �ƹ������� ������ false
    return false;
}

//�̰� ���� ������ ��ũ��Ʈ ������ �ҵ�. 

//���� ��ǥ currentVector


void CheckGraffitiProgress(Vector2)
    {
        // ������ ������ ȣ��

        // ValidateVectorSet�� 
        //false�� �������� �ǵ��� �Բ�
        //vectorset �ʱ�ȭ�ϰ� ����Ʈ �׸� �͵� ����.
        //���ÿ�completedSkillNum >= 0�̸� CheckGraffitiComplete ȣ�� ,

        //true�� �̵��� �ڸ��� ��¦��
        // CheckCompleted >= 0 �� ���� �ǵ��, completedSkillNum�� ��

        //���⿡ ���� ������ ����Ʈ ���°�
        graffitiVectorSet.Clear();
    }

    void CheckGraffitiComplete()
    {
        completedSkillNum = CheckCompleted();
        if (completedSkillNum >= 0)
        {
            //���� ȣ���ϴ� �� �ְ�

            completedSkillNum = -1;
        }
    }
    // �׶���Ƽ ���� ���� ��ǲ�� ������ ȣ��
    // �÷��̾ ��ų �ߵ��ϵ��� ��


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

    // �̵��� ������ vectorset�� ��ų �߿� ���ԵǴ��� Ȯ��
    


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
    // ���� vectorset�� ��ų �� �ϳ��� ��ġ�ϴ��� Ȯ��
        
    }

