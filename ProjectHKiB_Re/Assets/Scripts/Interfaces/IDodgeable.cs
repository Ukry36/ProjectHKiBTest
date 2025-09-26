using UnityEngine;

public interface IDodgeable : IDodgeableBase, IInitializable
{
    public bool CanDodge { get; set; }
    public int DodgeCount { get; set; }
    public bool CanCounterAttack { get; set; }
    public void StartDodge();
    public void StartKeepDodge();
    public bool CheckKeepDodgeEnded();
    public void EndDodge();
}