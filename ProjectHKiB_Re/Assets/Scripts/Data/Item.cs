
public class Item
{
    public readonly ItemDataSO data;
    public int ID { get => data.GetInstanceID(); }
    public StateMachineSO ItemEvent { get => data.itemUseEvent; }
    public int Count { get; private set; }

    public Item(ItemDataSO data, int count = 1)
    {
        if (count <= 0 || !data.canStack) count = 1;
        this.data = data;
        Count = count;
    }

    public bool ItemCountCheck(int count) => Count - count >= 0;
    public void UnstackItem(int count)
    {
        Count -= count;
        if (Count < 0) Count = 0;
    }

    public void StackItem(int count)
    {
        if (data.canStack)
            Count += count;
        else
            Count = 1;
    }
}