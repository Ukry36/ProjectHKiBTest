using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "End State", menuName = "Scriptable Objects/Dialogue States/End State", order = 3)]
public class EndStateSO : DialogueBaseStateSO
{
    public override void OnEnter()
    {
        Debug.Log("대화 종료!");
        dialogueModule.dialogueUI.SetActive(false);

        GameManager.instance.inputManager.PLAYMode();
        
        dialogueModule.currentDialogue = null;
        dialogueModule.currentLineNum = -1;
    }
}
