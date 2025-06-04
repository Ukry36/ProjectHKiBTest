using UnityEngine;

[CreateAssetMenu(fileName = "StatBuff", menuName = "Scriptable Objects/StatBuff")]
public class StatBuffSO : ScriptableObject
{
    public int ID { get => this.GetInstanceID(); }
    [field: NaughtyAttributes.ResizableTextArea()][field: SerializeField] public string Description { get; set; }
    [field: SerializeField] public float BuffTime { get; set; }
    [field: SerializeField] public bool UseToken { get; set; }
    [field: SerializeField] public bool TimeStackable { get; set; }
    [field: SerializeField] public bool Multiplyable { get; set; }
    [field: SerializeField] public bool UpdateBuffTime { get; set; }
    [field: SerializeField] public float Value { get; set; }
    [field: SerializeField] public StatBuffTypeSO BuffType { get; private set; }
    public void ApplyBuff(StatBuffController buffController, int multiplyer = 1)
    {
        BuffType.ApplyBuff(buffController, this, multiplyer);
    }
    public void RemoveBuff(StatBuffController buffController)
    {
        BuffType.RemoveBuff(buffController, this);
    }
}