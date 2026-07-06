using UnityEngine;

public abstract class EventControllableBase<T> : MonoBehaviour
{
    public string ID;
    public T Target;
    public abstract void Initialize();
}