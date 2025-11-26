using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "End State", menuName = "Scriptable Objects/Dialogue States/End State", order = 3)]
public class EndStateSO : DialogueBaseStateSO
{
    public override void OnEnter(DialogueModule module)
    {
        Debug.Log("대화 종료!");
        module.dialogueUI.SetActive(false);

        GameManager.instance.inputManager.PLAYMode();
        /*
                module.CurrentDialogue = null;
                module.CurrentLineNum = -1;*/
    }

    public override void OnExit(DialogueModule module) { }

    public override void OnUpdate(DialogueModule module) { }
}
