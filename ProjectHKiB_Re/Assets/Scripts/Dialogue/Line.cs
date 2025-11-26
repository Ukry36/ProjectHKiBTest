using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Line
{
    public int index;
    public bool isEndLine;
    public string characterName;

    [TextArea]
    public string[] lines;

    public float duration;
    public Sprite BG;

    public StateSO lineType;
    public ChoiceData[] choices;
    public ActionOptions actionOptions;

    public UnityEvent actionEvent;
    public Line()
    {
        if (lines != null && lines.Length > 0 && (duration <= 0))
        {
            duration = lines[0].Length * 0.1f;
        }
    }
}

[Serializable]
public class ActionOptions
{
    [Tooltip("ActionState에서 특정 입력을 기다리는지 여부")]
    public bool waitSpecificInput = false;

    [Tooltip("waitSpecificInput이 true일 때, 이 키를 눌러야 다음 줄로 진행")]
    public EnumManager.InputType inputKey;

    [Tooltip("ActionState에서 액션 후에 플레이어 입력을 기다릴지 여부")]
    public bool waitInputAfterAction = false;

    [Tooltip("ActionState에서 자동으로 넘길 경우, 몇 초 기다릴지 (0 이하면 바로 다음 줄)")]
    public float autoNextDelay = 0f;
}



[Serializable]
public struct ChoiceData
{
    public string choiceText;
    [Tooltip("선택 시 다음으로 이동할 DialogueDataSO.lines의 인덱스 번호")]
    public int nextLineIndex;
}

