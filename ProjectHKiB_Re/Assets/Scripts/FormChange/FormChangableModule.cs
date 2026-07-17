
using System.Collections.Generic;

public interface IFormChangable : ISkinableBase, IInitializable
{

}

public class FormChangableModule : SkinableModule, IFormChangable
{
    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<ISkinable>(this);
    }

    public override void Initialize()
    {

    }

    public override void ApplySkin(SimpleAnimationDataSO animationData)
    {
        base.ApplySkin(animationData);
    }
}
