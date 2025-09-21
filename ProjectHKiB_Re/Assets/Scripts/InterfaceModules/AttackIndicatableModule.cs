using UnityEngine;

public class AttackIndicatableModule : InterfaceModule, IAttackIndicatable
{
    public SpriteRenderer indicatorFrame;
    public SpriteRenderer indicatorInner;

    public int LastAttackIndicatorID { get; set; }
    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IAttackIndicatable>(this);
    }
    public void Initialize()
    {
        this.gameObject.SetActive(true);
        indicatorFrame.enabled = false;
        indicatorInner.enabled = false;
    }

    public void StartIndicating(Vector2 size, Vector3 offset, Vector3 pivot)
    {
        indicatorFrame.enabled = true;
        indicatorInner.enabled = true;
        indicatorFrame.transform.localPosition = offset;
        indicatorInner.transform.localPosition = pivot;
        indicatorFrame.size = size;
        indicatorInner.size = Vector2.zero;
    }

    public void EndIndicating()
    {
        indicatorFrame.enabled = false;
        indicatorInner.enabled = false;
    }


}