using System.Collections.Generic;
using UnityEngine;

public class EmotionModule : InterfaceModule, IEmotionModule
{
    [Header("Manual Emotion Buffs")]
    [SerializeField] private List<StatBuffSO> emotionBuffs = new();

    [Header("Cancel")]
    [SerializeField] private bool applyCancelColor = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private IBuffable _buffable;
    private BuffableModule _buffableModule;

    private readonly Dictionary<EmotionColor, StatBuffSO> _buffMap = new();
    private bool _isEvaluatingReaction;

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IEmotionModule>(this);
    }

    public void Initialize()
    {
        _buffableModule = GetComponent<BuffableModule>();
        _buffable = _buffableModule;

        if (_buffable == null)
            _buffable = GetComponent<IBuffable>();

        _buffMap.Clear();

        for (int i = 0; i < emotionBuffs.Count; i++)
        {
            RegisterEmotionBuff(emotionBuffs[i]);
        }

        if (showDebugLog)
            Debug.Log($"[EmotionModule] Emotion Buff Loaded: {_buffMap.Count}");
    }

    private void RegisterEmotionBuff(StatBuffSO buff)
    {
        if (buff == null) return;

        if (!buff.IsEmotionBuff)
        {
            if (showDebugLog)
                Debug.LogWarning($"[EmotionModule] IsEmotionBuff가 꺼져 있음: {buff.name}");
            return;
        }

        if (_buffMap.ContainsKey(buff.EmotionColor))
        {
            if (showDebugLog)
                Debug.LogWarning($"[EmotionModule] EmotionColor 중복 등록됨: {buff.EmotionColor}");
        }

        _buffMap[buff.EmotionColor] = buff;
    }

    public void ApplyColor(EmotionColor color, int stack, float overrideDuration = -1f)
    {
        ApplyColor(color, GetCurrentSourceGear(), stack, overrideDuration);
    }

    public void ApplyColor(EmotionColor color, Gear sourceGear, int stack, float overrideDuration = -1f)
    {
        ApplyColorInternal(color, sourceGear, stack, overrideDuration, true);
    }

    private void ApplyColorInternal(
        EmotionColor color,
        Gear sourceGear,
        int stack,
        float overrideDuration,
        bool evaluateReaction
    )
    {
        if (_buffable == null) return;
        if (stack <= 0) return;

        if (!_buffMap.TryGetValue(color, out StatBuffSO buff))
        {
            if (showDebugLog)
                Debug.LogWarning($"[EmotionModule] Emotion Buff not found: {color}");
            return;
        }

        int previousStack = GetStacks(color, sourceGear);
        int nextStack = Mathf.Min(buff.MaxStack, previousStack + stack);
        int addStack = nextStack - previousStack;

        if (addStack <= 0) return;

        _buffable.Buff(buff, sourceGear, addStack, 1, overrideDuration);

        if (showDebugLog)
            Debug.Log($"[EmotionModule] Apply {color} +{addStack}, Total {nextStack}");

        if (evaluateReaction && IsBaseColor(color))
            EvaluateReaction(color, sourceGear, previousStack);
    }

    public int GetStacks(EmotionColor color)
    {
        return GetStacks(color, GetCurrentSourceGear());
    }

    public int GetStacks(EmotionColor color, Gear sourceGear)
    {
        if (_buffable == null) return 0;
        if (!_buffMap.TryGetValue(color, out StatBuffSO buff)) return 0;

        BuffInfo info = _buffable.FindBuff(buff, sourceGear);
        return info != null ? info.BuffStack : 0;
    }

    public bool HasColor(EmotionColor color)
    {
        return GetStacks(color) > 0;
    }

    public bool HasColor(EmotionColor color, Gear sourceGear)
    {
        return GetStacks(color, sourceGear) > 0;
    }

    public void RemoveColor(EmotionColor color, int stack = 1)
    {
        RemoveColor(color, GetCurrentSourceGear(), stack);
    }

    public void RemoveColor(EmotionColor color, Gear sourceGear, int stack = 1)
    {
        if (_buffable == null) return;
        if (stack <= 0) return;
        if (!_buffMap.TryGetValue(color, out StatBuffSO buff)) return;

        _buffable.UnBuff(buff, sourceGear, stack);

        if (showDebugLog)
            Debug.Log($"[EmotionModule] Remove {color} -{stack}");
    }

    private void RemoveAll(EmotionColor color, Gear sourceGear)
    {
        int stack = GetStacks(color, sourceGear);
        if (stack > 0)
            RemoveColor(color, sourceGear, stack);
    }

    private void EvaluateReaction(EmotionColor appliedColor, Gear sourceGear, int previousStack)
    {
        if (_isEvaluatingReaction) return;

        _isEvaluatingReaction = true;

        try
        {
            if (TryCancelOrOverwrite(sourceGear)) return;
            if (TrySameColorReaction(appliedColor, sourceGear, previousStack)) return;
            if (TryVoidReaction(appliedColor, sourceGear)) return;
            TryNormalReaction(appliedColor, sourceGear);
        }
        finally
        {
            _isEvaluatingReaction = false;
        }
    }

    private bool TryCancelOrOverwrite(Gear sourceGear)
    {
        // 공포 + 행복 = 행복 제거, 공포 유지
        if (GetStacks(EmotionColor.Fear, sourceGear) > 0 &&
            GetStacks(EmotionColor.Happiness, sourceGear) > 0)
        {
            RemoveAll(EmotionColor.Happiness, sourceGear);

            if (showDebugLog)
                Debug.Log("[EmotionModule] Fear overwrites Happiness");

            return true;
        }

        // 공허 + 흥분 = 상쇄
        if (TryCancel(EmotionColor.Void, EmotionColor.Excitement, sourceGear))
            return true;

        // 분노 + 행복 = 상쇄
        if (TryCancel(EmotionColor.Anger, EmotionColor.Happiness, sourceGear))
            return true;

        return false;
    }

    private bool TryCancel(EmotionColor a, EmotionColor b, Gear sourceGear)
    {
        int stackA = GetStacks(a, sourceGear);
        int stackB = GetStacks(b, sourceGear);

        if (stackA <= 0 || stackB <= 0) return false;

        int resultStack = stackA + stackB;

        RemoveColor(a, sourceGear, stackA);
        RemoveColor(b, sourceGear, stackB);

        if (applyCancelColor)
            ApplyReactionColor(EmotionColor.Cancel, sourceGear, resultStack);

        if (showDebugLog)
            Debug.Log($"[EmotionModule] {a} + {b} = Cancel");

        return true;
    }

    private bool TrySameColorReaction(EmotionColor appliedColor, Gear sourceGear, int previousStack)
    {
        if (previousStack <= 0) return false;

        if (appliedColor == EmotionColor.Sadness)
            return ReactSame(EmotionColor.Sadness, EmotionColor.Sorrow, sourceGear);

        if (appliedColor == EmotionColor.Excitement)
            return ReactSame(EmotionColor.Excitement, EmotionColor.Madness, sourceGear);

        if (appliedColor == EmotionColor.Anger)
            return ReactSame(EmotionColor.Anger, EmotionColor.Fury, sourceGear);

        return false;
    }

    private bool ReactSame(EmotionColor source, EmotionColor result, Gear sourceGear)
    {
        int stack = GetStacks(source, sourceGear);
        if (stack <= 0) return false;

        if (!CanApplyColor(result))
        {
            if (showDebugLog)
                Debug.LogWarning($"[EmotionModule] Reaction Buff not found: {result}");
            return false;
        }

        RemoveColor(source, sourceGear, stack);
        ApplyReactionColor(result, sourceGear, stack);

        if (showDebugLog)
            Debug.Log($"[EmotionModule] {source} + {source} = {result}, Stack {stack}");

        return true;
    }

    private bool TryVoidReaction(EmotionColor appliedColor, Gear sourceGear)
    {
        if (appliedColor == EmotionColor.Excitement) return false;
        if (GetStacks(EmotionColor.Void, sourceGear) <= 0) return false;

        if (appliedColor == EmotionColor.Void)
        {
            return TryPair(EmotionColor.Void, EmotionColor.Sadness, EmotionColor.VoidReaction, sourceGear)
                || TryPair(EmotionColor.Void, EmotionColor.Happiness, EmotionColor.VoidReaction, sourceGear)
                || TryPair(EmotionColor.Void, EmotionColor.Anger, EmotionColor.VoidReaction, sourceGear)
                || TryPair(EmotionColor.Void, EmotionColor.Fear, EmotionColor.VoidReaction, sourceGear);
        }

        if (!IsBaseColor(appliedColor)) return false;
        if (appliedColor == EmotionColor.Void) return false;

        return TryPair(appliedColor, EmotionColor.Void, EmotionColor.VoidReaction, sourceGear);
    }

    private bool TryNormalReaction(EmotionColor appliedColor, Gear sourceGear)
    {
        return TryPair(appliedColor, EmotionColor.Sadness, EmotionColor.Excitement, EmotionColor.Collapse, sourceGear)
            || TryPair(appliedColor, EmotionColor.Sadness, EmotionColor.Fear, EmotionColor.Collapse, sourceGear)
            || TryPair(appliedColor, EmotionColor.Happiness, EmotionColor.Excitement, EmotionColor.Madness, sourceGear)
            || TryPair(appliedColor, EmotionColor.Anger, EmotionColor.Excitement, EmotionColor.Madness, sourceGear)
            || TryPair(appliedColor, EmotionColor.Sadness, EmotionColor.Happiness, EmotionColor.Longing, sourceGear)
            || TryPair(appliedColor, EmotionColor.Sadness, EmotionColor.Anger, EmotionColor.Resentment, sourceGear)
            || TryPair(appliedColor, EmotionColor.Fear, EmotionColor.Excitement, EmotionColor.Panic, sourceGear)
            || TryPair(appliedColor, EmotionColor.Fear, EmotionColor.Anger, EmotionColor.Bluff, sourceGear);
    }

    private bool TryPair(
        EmotionColor appliedColor,
        EmotionColor a,
        EmotionColor b,
        EmotionColor result,
        Gear sourceGear
    )
    {
        if (appliedColor != a && appliedColor != b)
            return false;

        return TryPair(a, b, result, sourceGear);
    }

    private bool TryPair(EmotionColor a, EmotionColor b, EmotionColor result, Gear sourceGear)
    {
        int stackA = GetStacks(a, sourceGear);
        int stackB = GetStacks(b, sourceGear);

        if (stackA <= 0 || stackB <= 0)
            return false;

        if (!CanApplyColor(result))
        {
            if (showDebugLog)
                Debug.LogWarning($"[EmotionModule] Reaction Buff not found: {result}");
            return false;
        }
    
        int resultStack = stackA + stackB;

        RemoveColor(a, sourceGear, stackA);
        RemoveColor(b, sourceGear, stackB);
        ApplyReactionColor(result, sourceGear, resultStack);

        if (showDebugLog)
            Debug.Log($"[EmotionModule] {a} + {b} = {result}, Stack {resultStack}");

        return true;
    }

    private void ApplyReactionColor(EmotionColor color, Gear sourceGear, int stack)
    {
        // 반응색은 다시 반응하지 않게 false
        ApplyColorInternal(color, sourceGear, stack, -1f, false);
    }

    private bool CanApplyColor(EmotionColor color)
    {
        return _buffMap.ContainsKey(color);
    }

    private bool IsBaseColor(EmotionColor color)
    {
        return color == EmotionColor.Sadness
            || color == EmotionColor.Excitement
            || color == EmotionColor.Happiness
            || color == EmotionColor.Anger
            || color == EmotionColor.Void
            || color == EmotionColor.Fear;
    }

    private Gear GetCurrentSourceGear()
    {
        if (_buffableModule == null)
            _buffableModule = GetComponent<BuffableModule>();

        return _buffableModule != null ? _buffableModule.GetCurrentSourceGear() : null;
    }
    //Stack을 로마자로 표기하는 함수
    public string GetApproxRomanStack(EmotionColor color)
    {
        return GetApproxRomanStack(color, GetCurrentSourceGear());
    }

    public string GetApproxRomanStack(EmotionColor color, Gear sourceGear)
    {
        int stack = GetStacks(color, sourceGear);
        if (stack <= 0) return string.Empty;

        int rounded = Mathf.Max(5, (stack / 5) * 5);
        return ToRoman(rounded);
    }

    private string ToRoman(int number)
    {
        var map = new (int value, string symbol)[]
        {
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
            (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
            (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
        };

        System.Text.StringBuilder sb = new();

        for (int i = 0; i < map.Length; i++)
        {
            while (number >= map[i].value)
            {
                sb.Append(map[i].symbol);
                number -= map[i].value;
            }
        }

        return sb.ToString();
    }
}