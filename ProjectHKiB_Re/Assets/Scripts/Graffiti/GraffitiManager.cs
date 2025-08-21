using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraffitiManager : MonoBehaviour
{
    private List<Vector2> graffitiProgress = new();
    private List<PlayerSkillDataSO> skillList = new();
    
    public PlayerSkillDataSO[] SkillListTemp;
    private int completedSkillNum = -1;
    private bool check = true;
    //��ų �߰��ڵ�

    // public bool ContainsVector(Vector2 target)
    // {

    //     return false;
    // }

    //�̰� ���� ������ ��ũ��Ʈ ������ �ҵ�. 

    //���� ��ǥ currentVector

    /// <summary>
    /// called everytime when player moves in graffiti
    /// </summary>
    /// <param name="currentPlayerPos"> current player pos</param>
    public void CheckGraffitiProgress(Vector2 currentPlayerPos) //�̰� �� �ǵ�� ��¼�� �̰� �� ����
    {  
        
        // ������ ������ ȣ��

        // ValidateVectorSet�� 
        //false�� �������� �ǵ��� �Բ�
        //vectorset �ʱ�ȭ�ϰ� ����Ʈ �׸� �͵� ����.
        //���ÿ�completedSkillNum >= 0�̸� CheckGraffitiComplete ȣ�� ,

        //true�� �̵��� �ڸ��� ��¦��
        // CheckCompleted >= 0 �� ���� �ǵ��, completedSkillNum�� ��
        //get player's vector
        
        graffitiProgress.Add(currentPlayerPos);
        
        //print Vectorset();
        int completedSkillNum = CheckCompleted();

        if(completedSkillNum >= 0)
        {
            //success feedback-> green effect
            EndGraffiti();
            return;
        }


        check = ValidateProgress(graffitiProgress);
        //fail
        if (!check)
        {
            //fail feedback-> red effect
            graffitiProgress.Clear();
        }
    }

    private void EndGraffiti()
    {
        TriggerSkill(completedSkillNum);
        graffitiProgress.Clear();
    }

    private bool ValidateProgress(List<Vector2> graffitiProgress)
    {
        for(int skill = 0; skill < SkillListTemp.Length; skill++)
        {
            for (int i = 0; i < SkillListTemp[skill].graffitiAllCases.Count; i++)
            {
                List<Vector2> graffitiCode = SkillListTemp[skill].graffitiAllCases[i].code;
                bool isSkillValid = true;
                for (int j = 0; j < graffitiProgress.Count; j++)
                {
                    if (!graffitiCode.Contains(graffitiProgress[j]))
                    {
                        isSkillValid = false;
                        break;
                    }
                }
                if (isSkillValid) return true;
            }
        }
        return false;
    }

    public void TriggerSkill(int num)
    {
        // trigger skill
        // trigger func from player script
    }
    // �׶���Ƽ ���� ���� ��ǲ�� ������ ȣ��
    // �÷��̾ ��ų �ߵ��ϵ��� ��

    //List<Vector2> VectorSet()
    //{
    //    List<Vector2> patterns=null;
//
    //    foreach (var graffitiCode in SkillListTemp.graffitiAllCases){
    //        if (graffitiProgress.All(vector => graffitiCode.code.Contains(vector)))
    //            {
    //                patterns = graffitiCode.code;  
    //                break;
    //            }
    //    }
    //    return patterns;
    //}

    

    // �̵��� ������ vectorset�� ��ų �߿� ���ԵǴ��� Ȯ��
    int CheckCompleted()
    {
        for(int skill = 0; skill < SkillListTemp.Length; skill++)
        {
            for (int i = 0; i < SkillListTemp[skill].graffitiAllCases.Count; i++)
            {
                List<Vector2> graffitiCode = SkillListTemp[skill].graffitiAllCases[i].code;
                if (graffitiCode == graffitiProgress)
                    return skill;
            }
        }
        return -1;
    }
    // ���� vectorset�� ��ų �� �ϳ��� ��ġ�ϴ��� Ȯ��

}
