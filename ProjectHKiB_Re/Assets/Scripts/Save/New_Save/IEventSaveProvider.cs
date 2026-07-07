using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEventSaveProvider
{
    Dictionary<string, int> EventFlags { get; }
    void SetEventFlag(string id, int value);

    Dictionary<string, bool> Passages { get; }
    void SetPassage(string id, bool opened);
}

