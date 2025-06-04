using System.Collections.Generic;

public class FloatBuffCalculator
{
    public Dictionary<int, float> StatBuffAddList { get; set; } = new(10);
    public float StatBuffAdd
    {
        get
        {
            float result = 0;
            foreach (var item in StatBuffAddList)
            {
                result += item.Value;
            }
            return result;
        }
    }
    public Dictionary<int, float> StatBuffPropList { get; set; } = new(10);
    public float StatBuffProp
    {
        get
        {
            float result = 0;
            foreach (var item in StatBuffPropList)
            {
                result += item.Value;
            }
            return result;
        }
    }
    public float GetBuffedStat(float baseStat)
    => baseStat + StatBuffAdd + StatBuffProp * baseStat;
}

public class BoolBuffCalculator
{
    public Dictionary<int, bool> StatBuffList { get; set; } = new(10);
    public bool StatBuff
    {
        get
        {
            foreach (var item in StatBuffList)
            {
                if (item.Value)
                {
                    return true;
                }
            }
            return false;
        }
    }
    public bool GetBuffedStat()
    => StatBuff;
}