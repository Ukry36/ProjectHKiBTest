using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

[System.Serializable]
public class CustomVariableSets
{
    public SerializedDictionary<string, CustomVariable<bool>> boolVariables;
    public SerializedDictionary<string, CustomVariable<int>> intVariables;
    public SerializedDictionary<string, CustomVariable<float>> floatVariables;
}
