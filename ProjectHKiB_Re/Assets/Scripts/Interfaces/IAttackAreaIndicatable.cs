using UnityEngine;

public interface IAttackIndicatable : IInitializable
{
    public int LastAttackIndicatorID { get; set; }
    public void StartIndicating(Vector2 size, Vector3 offset, Vector3 pivot);
    public void EndIndicating();
}