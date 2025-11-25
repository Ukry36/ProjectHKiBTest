using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DialogueBaseStateSO : ScriptableObject, IState
{
    protected DialogueModule dialogueModule;
    
    //This Mathod Can Input DialogueModule in State SO. 
    public void Initialize(DialogueModule module)
    {
        dialogueModule = module;
    }

    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
}
