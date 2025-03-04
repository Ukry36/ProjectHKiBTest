using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CustomVariableSets
{
    [HideInInspector] public Dictionary<string, CustomVariable<bool>> boolVariables;
    [HideInInspector] public Dictionary<string, CustomVariable<int>> intVariables;
    [HideInInspector] public Dictionary<string, CustomVariable<float>> floatVariables;

    [SerializeField] private CustomVariableSetter<bool>[] boolVariableSetter;
    [SerializeField] private CustomVariableSetter<int>[] intVariableSetter;
    [SerializeField] private CustomVariableSetter<float>[] floatVariableSetter;

    public CustomVariableSets()
    {
        boolVariables = new();
        intVariables = new();
        floatVariables = new();
        int i;
        if (boolVariableSetter != null)
            for (i = 0; i < boolVariableSetter.Length; i++)
            {
                boolVariables.Add(boolVariableSetter[i].name, boolVariableSetter[i].variable);
            }
        if (intVariableSetter != null)
            for (i = 0; i < intVariableSetter.Length; i++)
            {
                intVariables.Add(intVariableSetter[i].name, intVariableSetter[i].variable);
            }
        if (floatVariableSetter != null)
            for (i = 0; i < floatVariableSetter.Length; i++)
            {
                floatVariables.Add(floatVariableSetter[i].name, floatVariableSetter[i].variable);
            }
    }
}

[System.Serializable]
public class CustomVariableSetter<T>
{
    public string name;
    public CustomVariable<T> variable;
}