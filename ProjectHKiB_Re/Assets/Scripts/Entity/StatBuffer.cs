using System.Collections.Generic;

public class StatBuff<T>
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public T Value { get; set; }
}
public class StatBufferFloat
{
    public List<StatBuff<float>> StatBuffAddList { get; set; } = new(10);
    public float StatBuffAdd
    {
        get
        {
            float result = 0;
            for (int i = 0; i < StatBuffAddList.Count; i++)
            {
                result += StatBuffAddList[i].Value;
            }
            return result;
        }
    }
    public List<StatBuff<float>> StatBuffPropList { get; set; } = new(10);
    public float StatBuffProp
    {
        get
        {
            float result = 0;
            for (int i = 0; i < StatBuffPropList.Count; i++)
            {
                result += StatBuffPropList[i].Value;
            }
            return result;
        }
    }
    public float GetBuffedStat(float baseStat)
    => baseStat + StatBuffAdd + StatBuffProp * baseStat;
}

public class StatBufferBool
{
    public List<StatBuff<bool>> StatBuffList { get; set; } = new(10);
    public bool StatBuff
    {
        get
        {
            bool result = false;
            for (int i = 0; i < StatBuffList.Count; i++)
            {
                if (StatBuffList[i].Value)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
    }
    public bool GetBuffedStat()
    => StatBuff;
}