using System.Collections;
using System.Collections.Generic;   
using UnityEngine;

public interface IEventSaveProvider
{
    Dictionary<string, bool> EventFlags { get; }
    void SetEventFlag(string id, bool value);

    Dictionary<string, float> EventVars { get; }
    void SetEventVar(string id, float value);

    Dictionary<string, bool> Passages { get; }
    void SetPassage(string id, bool opened);
}

