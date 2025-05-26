using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graffiti : MonoBehaviour
{
    private List<Vector2> graffitiVectorSet = new List<Vector2>();
    private List<PlayerSkillDataSO> skillList = new List<SkillDataS0>();
    
    int completedSkillNum = -1;

    //��ų �߰��ڵ�

    public bool ContainsVector(Vector2 target)
    {

        

        // �ƹ������� ������ false
        return false;
    }

    //�̰� ���� ������ ��ũ��Ʈ ������ �ҵ�. 

    //���� ��ǥ currentVector


    void CheckGraffitiProgress(Vector2)//�̰� �� �ǵ�� ��¼�� �̰� �� ���
    {  
        bool check = ContainsVector(currentVector)
        // ������ ������ ȣ��

        // ValidateVectorSet�� 
        //false�� �������� �ǵ��� �Բ�
        //vectorset �ʱ�ȭ�ϰ� ����Ʈ �׸� �͵� ����.
        //���ÿ�completedSkillNum >= 0�̸� CheckGraffitiComplete ȣ�� ,

        //true�� �̵��� �ڸ��� ��¦��
        // CheckCompleted >= 0 �� ���� �ǵ��, completedSkillNum�� ��

        //���⿡ ���� ������ ����Ʈ ���°�
        if (!check) {
            //���� ������ ����Ʈ
            graffitiVectorSet.Clear();
        }
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
