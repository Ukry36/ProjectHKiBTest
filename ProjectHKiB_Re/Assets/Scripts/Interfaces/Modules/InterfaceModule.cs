using UnityEngine;

[RequireComponent(typeof(IInterfaceRegistable))]
public abstract class InterfaceModule : MonoBehaviour
{
    // you can register required interface from here
    public abstract void Register(IInterfaceRegistable interfaceRegistable);
}