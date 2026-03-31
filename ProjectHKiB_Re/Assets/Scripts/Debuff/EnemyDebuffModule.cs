using System.Collections.Generic;
using UnityEngine;

public class EnemyDebuffModule : InterfaceModule, IEnemyDebuff
{
    [System.Serializable]
    public class ColorBuffEntry
    {
        public EmotionColor color;
        public StatBuffSO statBuff;
        public float defaultDuration = 5f;
        public int maxStack = 200;
    }

    [Header("Color Buff Mapping")]
    [SerializeField] private List<ColorBuffEntry> colorBuffEntries = new();

    private IBuffable _buffable;
    private InterfaceRegister _interfaceRegister;
    private readonly Dictionary<EmotionColor, ColorBuffEntry> _entryMap = new();

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        if (interfaceRegistable is InterfaceRegister register)
        {
            _interfaceRegister = register;
        }

        interfaceRegistable.RegisterInterface<IEnemyDebuff>(this);
    }

    public void Initialize()
    {
        _buffable = GetComponent<IBuffable>();

        _entryMap.Clear();
        foreach (var entry in colorBuffEntries)
        {
            if (entry != null && entry.statBuff != null)
            {
                _entryMap[entry.color] = entry;
            }
        }
    }

    public void ApplyColor(EmotionColor color, int stack, float overrideDuration = -1f)
    {
        if (!_entryMap.TryGetValue(color, out var entry)) return;
        if (_buffable == null) return;
        if (stack <= 0) return;

        int current = GetStacks(color);
        int next = Mathf.Min(entry.maxStack, current + stack);
        int addAmount = next - current;

        if (addAmount <= 0) return;

        float duration = overrideDuration > 0f ? overrideDuration : entry.defaultDuration;

        _buffable.Buff(entry.statBuff, addAmount, 1, duration);

        EvaluateReactionOnApply(color);
    }

    public int GetStacks(EmotionColor color)
    {
        if (_buffable == null) return 0;
        if (!_entryMap.TryGetValue(color, out var entry)) return 0;
        if (entry.statBuff == null) return 0;

        BuffInfo buffInfo = _buffable.FindBuff(entry.statBuff);
        return buffInfo != null ? buffInfo.BuffStack : 0;
    }

    public bool HasColor(EmotionColor color)
    {
        return GetStacks(color) > 0;
    }

    public void RemoveColor(EmotionColor color, int stack = 1)
    {
        if (_buffable == null) return;
        if (!_entryMap.TryGetValue(color, out var entry)) return;
        if (entry.statBuff == null) return;
        if (stack <= 0) return;

        _buffable.UnBuff(entry.statBuff, stack);
    }

    private void EvaluateReactionOnApply(EmotionColor appliedColor)
    {
        int sadness = GetStacks(EmotionColor.Sadness);
        int excitement = GetStacks(EmotionColor.Excitement);
        int happiness = GetStacks(EmotionColor.Happiness);
        int anger = GetStacks(EmotionColor.Anger);
        int fear = GetStacks(EmotionColor.Fear);
        int voidStack = GetStacks(EmotionColor.Void);

        // 공포 + 행복 = 행복 제거, 공포가 덮어씀
        if (fear > 0 && happiness > 0)
        {
            RemoveColor(EmotionColor.Happiness, happiness);
            return;
        }

        // 공허 + 흥분 = 상쇄
        if (voidStack > 0 && excitement > 0)
        {
            RemoveColor(EmotionColor.Void, voidStack);
            RemoveColor(EmotionColor.Excitement, excitement);
            return;
        }

        // 분노 + 행복 = 상쇄
        if (anger > 0 && happiness > 0)
        {
            RemoveColor(EmotionColor.Anger, anger);
            RemoveColor(EmotionColor.Happiness, happiness);
            return;
        }

        // 붕괴 = 슬픔 + (흥분 or 공포)
        if (sadness > 0 && (excitement > 0 || fear > 0))
        {
            int other = Mathf.Max(excitement, fear);
            int reactionDamage = sadness * other;

            if (_interfaceRegister != null &&
                _interfaceRegister.TryGetInterface(out IDamagable damagable))
            {
                damagable.HP -= Mathf.Max(1, reactionDamage);
                damagable.OnDamaged?.Invoke();

                if (damagable.HP <= 0)
                {
                    damagable.Die();
                }
            }
        }
    }

    public string GetApproxRomanStack(EmotionColor color)
    {
        int stack = GetStacks(color);
        if (stack <= 0) return string.Empty;

        int rounded = (stack / 5) * 5;
        if (rounded < 5) rounded = 5;

        return ToRoman(rounded);
    }

    private string ToRoman(int number)
    {
        var map = new (int value, string symbol)[]
        {
            (1000, "M"),
            (900, "CM"),
            (500, "D"),
            (400, "CD"),
            (100, "C"),
            (90, "XC"),
            (50, "L"),
            (40, "XL"),
            (10, "X"),
            (9, "IX"),
            (5, "V"),
            (4, "IV"),
            (1, "I"),
        };

        System.Text.StringBuilder sb = new();
        int n = number;

        for (int i = 0; i < map.Length; i++)
        {
            while (n >= map[i].value)
            {
                sb.Append(map[i].symbol);
                n -= map[i].value;
            }
        }

        return sb.ToString();
    }
}